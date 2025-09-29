using System.Collections;
using UnityEngine;

public class TraderNPCController : MonoBehaviour
{
    [Header("Данные NPC")]
    public NPCTradePool pool;

    [Header("Префабы панелей (World-Space Canvas)")]
    public TradePanelController panelPrefab;   // оффер-панель
    public GameObject noItemsPrefab;           // «нет предметов»
    public GameObject thinkingPrefab;          // «думаю…»

    [Header("Якорь на NPC")]
    public Transform panelAnchor;
    public Vector3 panelOffset   = new Vector3(0f, 1.6f, 0f);
    public Vector3 emotionOffset = new Vector3(0f, 1.8f, 0f);

    [Header("Тайминги")]
    public float thinkingDelay = 3.5f;   // задержка «думаю…»
    public float emotionLifetime = 3.0f; // сколько держать эмоцию до исчезновения NPC

    // "YES" / "NO" / "ADD_yes" / "ADD_no"
    public System.Action<TraderNPCController, string> OnFinished;

    private bool _opened;
    private TradePanelController _activePanel;
    private GameObject _noItemsGO;
    private GameObject _thinkingGO;
    private Coroutine _pendingOfferCoro;

    [Header("Эмоции (префабы)")]
    public GameObject emoYesPrefab;
    public GameObject emoNoPrefab;
    public GameObject emoAddNoPrefab;

    // --- Параметры поведения ADD ---
    [Header("ADD-поведение")]
    [Range(0f,1f)] public float addAgreeChance = 0.20f; // 20% согласия
    public Vector2 addSmallBoostRange = new Vector2(0.03f, 0.08f); // +3..8% к текущей выгоде
    public const float FAIR_MIN = -0.20f;  // крайние для всего трейдинга
    public const float FAIR_MAX =  0.40f;

    public void OpenOffer()
    {
        if (_opened) return;
        _opened = true;
        if (!panelAnchor) panelAnchor = transform;

        bool hasAny = EggInventory.Instance != null &&
                      EggInventory.Instance.GetItemsSnapshot().Exists(s => s.IsBrainrot);

        if (!hasAny)
        {
            ShowNoItemsPanel();
            EggInventory.OnFirstBrainrotGained -= HandleFirstBrainrot;
            EggInventory.OnFirstBrainrotGained += HandleFirstBrainrot;
            return;
        }

        ShowOfferPanel();
    }

    void ShowNoItemsPanel()
    {
        if (!noItemsPrefab) { Debug.LogWarning("[TraderNPCController] noItemsPrefab не назначен"); return; }
        _noItemsGO = Instantiate(noItemsPrefab, panelAnchor, false);
        _noItemsGO.transform.localPosition = panelOffset;
        _noItemsGO.SetActive(true);
    }

    void ShowOfferPanel()
    {
        if (!panelPrefab) { Debug.LogWarning("[TraderNPCController] panelPrefab не назначен"); return; }

        if (_noItemsGO) { Destroy(_noItemsGO); _noItemsGO = null; }
        if (_thinkingGO){ Destroy(_thinkingGO); _thinkingGO = null; }

        var tradePanel = Instantiate(panelPrefab, panelAnchor, false);
        tradePanel.transform.localPosition = panelOffset; // позиционирование — только здесь
        tradePanel.gameObject.SetActive(true);
        tradePanel.ShowWorld(pool, panelAnchor, OnPanelClose);

        _activePanel = tradePanel;
        Debug.Log("[TraderNPCController] Открыл окно трейда");
    }

    void HandleFirstBrainrot()
    {
        EggInventory.OnFirstBrainrotGained -= HandleFirstBrainrot;

        if (_activePanel) return;

        if (_noItemsGO && thinkingPrefab)
        {
            _thinkingGO = Instantiate(thinkingPrefab, panelAnchor, false);
            _thinkingGO.transform.localPosition = emotionOffset;
            _thinkingGO.SetActive(true);

            if (_noItemsGO) { Destroy(_noItemsGO); _noItemsGO = null; }

            if (_pendingOfferCoro != null) StopCoroutine(_pendingOfferCoro);
            _pendingOfferCoro = StartCoroutine(SwapThinkingToOffer());
        }
        else
        {
            ShowOfferPanel();
        }
    }

    IEnumerator SwapThinkingToOffer()
    {
        yield return new WaitForSeconds(Mathf.Clamp(thinkingDelay, 0.5f, 10f));
        if (_thinkingGO) { Destroy(_thinkingGO); _thinkingGO = null; }
        ShowOfferPanel();
    }

    // ======= КНОПКИ ПАНЕЛИ =======
    void OnPanelClose(string result)
    {
        if (result == "ADD")
        {
            // 20% — соглашается «чуть докинуть», БЕЗ эмоции и БЕЗ закрытия окна
            if (Random.value < addAgreeChance && _activePanel && _activePanel.Offer != null)
            {
                float cur = _activePanel.Offer.FairnessRel;                // текущая выгода для игрока
                float inc = Random.Range(addSmallBoostRange.x, addSmallBoostRange.y); // небольшой буст
                float target = Mathf.Clamp(cur + inc, FAIR_MIN, FAIR_MAX); // не выходим за края

                // Узкий коридор вокруг цели — чтобы «подкрутить» текущий оффер, а не пересобирать радикально
                float bandHalf = 0.01f; // ±1% вокруг цели
                var band = new Vector2(Mathf.Clamp(target - bandHalf, FAIR_MIN, FAIR_MAX),
                                       Mathf.Clamp(target + bandHalf, FAIR_MIN, FAIR_MAX));

                var newOffer = TradeGenerator.CreateOfferInBand(EggInventory.Instance, pool, maxPerSide: 3, relBand: band);
                _activePanel.Offer = newOffer;
                _activePanel.Redraw();
                // НИКАКИХ эмоций и закрытий — игрок видит немного улучшившийся оффер
            }
            else
            {
                // Не согласен — злая эмоция и уходим (панель прячем)
                SpawnEmotion(emoAddNoPrefab);
                HideTradePanel();
                StartCoroutine(CloseAfterDelay("ADD_no"));
            }
            return;
        }

        if (result == "YES")
        {
            if (_activePanel && _activePanel.Offer != null)
            {
                bool applied = ApplyTrade(_activePanel.Offer);
                Debug.Log($"[TraderNPCController] ApplyTrade = {applied}");
            }
            SpawnEmotion(emoYesPrefab);
            HideTradePanel();
            StartCoroutine(CloseAfterDelay("YES"));
        }
        else // NO
        {
            SpawnEmotion(emoNoPrefab);
            HideTradePanel();
            StartCoroutine(CloseAfterDelay("NO"));
        }
    }

    void HideTradePanel()
    {
        if (_activePanel && _activePanel.gameObject.activeSelf)
            _activePanel.gameObject.SetActive(false);
    }

    IEnumerator CloseAfterDelay(string final)
    {
        yield return new WaitForSeconds(Mathf.Max(0.1f, emotionLifetime));
        CloseWith(final);
    }

    bool ApplyTrade(TradeOffer offer)
    {
        if (EggInventory.Instance == null || offer == null) return false;

        foreach (var it in offer.playerGive)
        {
            bool ok = EggInventory.Instance.RemoveBrainrot(it.so, it.type, it.weightKg);
            if (!ok) Debug.LogWarning($"[TraderNPCController] Не нашли у игрока {it.so?.name} [{it.weightKg:0.##}kg] для удаления");
        }
        foreach (var it in offer.npcGive)
        {
            EggInventory.Instance.AddBrainrot(it.so, it.type, it.weightKg);
        }

        SaveBridge.SnapshotAndSave(force: true);
        return true;
    }

    void SpawnEmotion(GameObject prefab)
    {
        if (!prefab || !panelAnchor) return;
        var emo = Instantiate(prefab, panelAnchor, false);
        emo.transform.localPosition = emotionOffset;
        emo.SetActive(true);
        Destroy(emo, Mathf.Max(0.1f, emotionLifetime));
    }

    void CloseWith(string final)
    {
        Debug.Log($"[TraderNPCController] Result: {final}");

        EggInventory.OnFirstBrainrotGained -= HandleFirstBrainrot;
        if (_pendingOfferCoro != null) { StopCoroutine(_pendingOfferCoro); _pendingOfferCoro = null; }
        if (_activePanel) { Destroy(_activePanel.gameObject); _activePanel = null; }
        if (_noItemsGO)   { Destroy(_noItemsGO); _noItemsGO = null; }
        if (_thinkingGO)  { Destroy(_thinkingGO); _thinkingGO = null; }

        _opened = false;
        OnFinished?.Invoke(this, final);
    }
}
