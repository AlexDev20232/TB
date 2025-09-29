// Assets/Scripts/Shop/ShopManager.cs
using UnityEngine;
using YG;
using System.Collections.Generic;

/// <summary>
/// Централизованный менеджер покупок: проверяет, платит, выдаёт награды.
/// </summary>
public class ShopManager : MonoBehaviour
{
    public static ShopManager Instance { get; private set; }

    bool               _purchaseInProgress;
    ShopItemButton     _currentItem;
    readonly HashSet<string> _pendingOneShot = new();

    void Awake()
    {
        if (Instance && Instance != this) { Destroy(gameObject); return; }
        Instance = this;    

        YG2.onPurchaseSuccess += OnPurchaseSuccess;
        YG2.onPurchaseFailed += OnPurchaseFailed;

        AdsController.AddCheck();
        
    }
    
    readonly Dictionary<string, ShopItemButton> _buttonsById = new();   // карта ID→кнопка
    readonly HashSet<string> _pendingGrants = new();              // ID, которые ждут кнопку



    void BuildButtonsCache()                                           // <─ NEW
{
    _buttonsById.Clear();
    foreach (ShopItemButton b in Resources.FindObjectsOfTypeAll<ShopItemButton>())
        _buttonsById[b.purchaseId] = b;            // сюда попадают даже Inactive
}

    // ───────────────────────────── кнопка → сюда
    /// <returns>true если можно открывать окно оплаты</returns>
    public bool TryBuy(ShopItemButton item)
    {
        if (_purchaseInProgress) return false;

        bool oneShot = item.IsOneShot;

        // уже куплен?
        if (oneShot &&
            (item.rewardType == ShopItemButton.RewardType.StarterPack && SaveBridge.Saves.starterPackOwned ||
             item.rewardType == ShopItemButton.RewardType.LimitedOffer && SaveBridge.Saves.limitedOfferOwned ||
             item.rewardType == ShopItemButton.RewardType.VipFlag && SaveBridge.Saves.vipOwned ||
             item.rewardType == ShopItemButton.RewardType.MoneyMultiplier && SaveBridge.Saves.moneyX2Owned ||
             item.rewardType == ShopItemButton.RewardType.LuckMultiplier && SaveBridge.Saves.luckX3Owned))
        {
            Debug.Log("[Shop] Одноразовый товар уже активирован");
            return false;
        }

        // уже запрошен?
        if (oneShot && _pendingOneShot.Contains(item.purchaseId))
        {
            Debug.Log("[Shop] Уже ждём подтверждения от SDK");
            return false;
        }

        _currentItem = item;
        _purchaseInProgress = true;
        if (oneShot) _pendingOneShot.Add(item.purchaseId);
        // YG2.BuyPayments(item.purchaseId);
        return true;
    }


    public void RegisterButton(ShopItemButton btn)          // <─ NEW
{
    _buttonsById[btn.purchaseId] = btn;

    if (_pendingGrants.Remove(btn.purchaseId))           // была ли ожидающая награда?
        GrantReward(btn);
}

    // ───────────────────────────── callbacks SDK
    void OnPurchaseSuccess(string id)
    {
        _purchaseInProgress = false;
        _pendingOneShot.Remove(id);

        if (_buttonsById.Count == 0) BuildButtonsCache();   // обновляем кэш один раз

   if (_buttonsById.TryGetValue(id, out var btn))      // кнопка уже есть (может быть Inactive)
       GrantReward(btn);
   else                                                // кнопка ещё не создана
       _pendingGrants.Add(id);

    _currentItem = null;
    }

    void OnPurchaseFailed(string id)
    {
        Debug.LogWarning($"[Shop] Покупка {id} отменена/ошибка");
        _purchaseInProgress = false;
        _pendingOneShot.Remove(id);
        _currentItem = null;
        RefreshAllButtons();
    }

    // ───────────────────────────── выдача наград
    void GrantReward(ShopItemButton item)
    {
        switch (item.rewardType)
        {
            case ShopItemButton.RewardType.CoinsPack:
                GameManager.Instance.AddMoney(item.intAmount);
                // Coins‑pack многоразовый → сразу консумируем
                YG2.ConsumePurchaseByID(item.purchaseId);
                break;

            case ShopItemButton.RewardType.MoneyMultiplier:
                SaveBridge.Saves.moneyX2Owned = true;
                MoneyBoostManager.Instance?.RefreshPermanent();
                break;

            case ShopItemButton.RewardType.LuckMultiplier:     // одноразовый
                SaveBridge.Saves.luckX3Owned = true;
                break;

            case ShopItemButton.RewardType.VipFlag:
                SaveBridge.Saves.vipOwned = true;
                MoneyBoostManager.Instance?.RefreshPermanent();
                break;

           case ShopItemButton.RewardType.StarterPack:
{
    if (SaveBridge.Saves.starterPackOwned) break;

    GameManager.Instance.AddMoney(item.bundleCoins);
    AdsController.EnableBannedAd(false);
    if (item.removeAds) { SaveBridge.Saves.adsDisabled = true; AdsController.DisableInterAdv(); }

    // если в паке есть брайрот — открыть в Индексе и попытаться поставить на базу
   if (item.bundleBrainrot)
{
    BrainrotUnlocks.ForceUnlock(item.bundleBrainrot); // ← индекс открыт
    if (!BaseController.Instance.IsBrainrotPlaced(item.bundleBrainrot))
        BaseController.Instance.TrySpawnOwnedBrainrot(item.bundleBrainrot);
}


    SaveBridge.Saves.starterPackOwned = true;
    SaveBridge.Save();
    RefreshAllButtons();
    Debug.Log($"[Shop] Reward granted for {item.debugName}");
    break;
}



          case ShopItemButton.RewardType.LimitedOffer:
    if (SaveBridge.Saves.limitedOfferOwned) break;

    GameManager.Instance.AddMoney(item.bundleCoins);
    if (item.removeAds) { SaveBridge.Saves.adsDisabled = true; AdsController.DisableInterAdv(); }

    if (item.bundleBrainrot &&
        !BaseController.Instance.IsBrainrotPlaced(item.bundleBrainrot))
    {
        var key = BrainrotUnlocks.InternalKey(item.bundleBrainrot);
        Debug.Log($"[Shop] LimitedOffer unlock: {item.bundleBrainrot.name} (key={key})");

           BrainrotUnlocks.ForceUnlock(item.bundleBrainrot);

        bool ok = BaseController.Instance.TrySpawnOwnedBrainrot(item.bundleBrainrot);
        Debug.Log(ok ? "[Shop] LimitedOffer spawn OK"
                     : "[Shop] LimitedOffer spawn FAILED (нет свободного активного слота?)");
    }

    SaveBridge.Saves.limitedOfferOwned = true;
    break;

        }

        SaveBridge.Save();          // локально + облако
        RefreshAllButtons();
        Debug.Log($"[Shop] Reward granted for {item.debugName}");
    }
/*
       void Update()
    {
        if (Input.GetKeyDown(KeyCode.O))
        {
           AdsController.EnableBannedAd(true);
        }
        if (Input.GetKeyDown(KeyCode.C))
        {
           AdsController.EnableBannedAd(false);
        }
    }
*/
    // ───────────────────────────── utils
    void RefreshAllButtons()
    {
        foreach (ShopItemButton btn in FindObjectsOfType<ShopItemButton>())
            btn.RefreshState();               // ← compile‑time safe call
    }
}
