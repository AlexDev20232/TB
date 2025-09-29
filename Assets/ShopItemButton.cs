// Assets/Scripts/Shop/ShopItemButton.cs
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using YG;

[RequireComponent(typeof(Button))]
[RequireComponent(typeof(PurchaseYG))]
public class ShopItemButton : MonoBehaviour
{
    [Header("ID из консоли Яндекса")]
    public string purchaseId;

    [Header("Награда за покупку")]
    public RewardType rewardType;
    public int    intAmount;
    public float  multiplierValue;
    public string debugName;

    [Header("Starter‑pack content")]
    public Brainrot bundleBrainrot;
    public int      bundleCoins;
    public bool     removeAds;

    // ────────────────────────────────── private
    Button      _btn;
    PurchaseYG  _ui;
    string      _cachedPrice;

    // ────────────────────────────────── life‑cycle
    void Awake()
    {
        _btn = GetComponent<Button>();
        _ui = GetComponent<PurchaseYG>();

        _ui.id = purchaseId;                     // sync ID
        _btn.onClick.AddListener(OnPress);

        Invoke(nameof(CachePrice), .1f);         // wait until PurchaseYG fills UI
        Invoke(nameof(RefreshState), .15f);      // first visual state
         ShopManager.Instance?.RegisterButton(this);
    }

    void CachePrice()
    {
        if (_ui.textMP.priceValue)
            _cachedPrice = _ui.textMP.priceValue.text;
        else if (_ui.textLegasy.priceValue)
            _cachedPrice = _ui.textLegasy.priceValue.text;
    }

    void OnPress()
    {
        if (ShopManager.Instance.TryBuy(this))
            _ui.BuyPurchase();
    }

    // ────────────────────────────────── public helpers
    public void RefreshState()
    {
        bool owned = rewardType switch
        {
            RewardType.StarterPack     => SaveBridge.Saves.starterPackOwned,
            RewardType.LimitedOffer    => SaveBridge.Saves.limitedOfferOwned,
            RewardType.VipFlag         => SaveBridge.Saves.vipOwned,
            RewardType.MoneyMultiplier => SaveBridge.Saves.moneyX2Owned,
            RewardType.LuckMultiplier  => SaveBridge.Saves.luckX3Owned,
            _                          => false
        };

        _btn.interactable = !owned || !IsOneShot;

        // show “Куплено!” only for one‑shot items that are already owned
        if (owned && IsOneShot && _ui.textMP.priceValue)
            _ui.textMP.priceValue.text = "Куплено!";
        else if (!owned && _ui.textMP.priceValue && !string.IsNullOrEmpty(_cachedPrice))
            _ui.textMP.priceValue.text = _cachedPrice;
    }

    public bool IsOneShot =>
        rewardType == RewardType.StarterPack  ||
        rewardType == RewardType.LimitedOffer ||
        rewardType == RewardType.MoneyMultiplier ||
        rewardType == RewardType.LuckMultiplier ||
        rewardType == RewardType.VipFlag;

    public string UiPrice => _cachedPrice;

    // ────────────────────────────────── enum
    public enum RewardType
    {
        CoinsPack,
        MoneyMultiplier,
        LuckMultiplier,
        VipFlag,
        StarterPack,
        LimitedOffer
    }
}
