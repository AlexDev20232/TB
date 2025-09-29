using UnityEngine;
using TMPro;
using static ButtonChose;

public class TradePanelController : MonoBehaviour
{
    [Header("Гриды с предметами")]
    public Transform playerGrid;
    public Transform npcGrid;
    public TradeSlotUI slotPrefab;

    [Header("Fairness marker (4 якоря)")]
    public RectTransform markerRect;
    public TextMeshProUGUI markerLabel;
    public RectTransform startRed;
    public RectTransform startYellow;
    public RectTransform startGreen;
    public RectTransform endGreen;

    [Header("Плавность маркера")]
    [SerializeField] private float markerSmooth = 12f;

    [Header("Пороги зон (в долях)")]
    [SerializeField] private float F_MIN      = -0.25f;
    [SerializeField] private float F_BAD_END  = -0.01f; // конец Bad (−1%)
    [SerializeField] private float F_FAIR_END =  0.03f; // конец Fair (+3%)
    [SerializeField] private float F_MAX      =  0.40f;
    [SerializeField] private float EPS        =  0.002f; // «мёртвая зона» ±0.2%

    public TradeOffer Offer { get; set; }
    private System.Action<string> _onClose;
    private float _markerTargetX;

    void Awake() => gameObject.SetActive(false);

    private void OnEnable()  => ButtonChose.OnChoice += HandleChoice;
    private void OnDisable() => ButtonChose.OnChoice -= HandleChoice;

    void Update()
    {
        if (!markerRect) return;
        var p = markerRect.anchoredPosition;
        p.x = Mathf.Lerp(p.x, _markerTargetX, Time.deltaTime * markerSmooth);
        markerRect.anchoredPosition = p;
    }

    void HandleChoice(Type choice)
    {
        switch (choice)
        {
            case Type.Yes: CloseWith("YES"); break;
            case Type.Add: CloseWith("ADD"); break;
            case Type.No:  CloseWith("NO");  break;
        }
    }

    // Позиционированием занимается TraderNPCController — тут только поворот на камеру
    public void ShowWorld(NPCTradePool pool, Transform anchor, System.Action<string> onClose)
    {
        _onClose = onClose;
        Offer = TradeGenerator.CreateOffer(EggInventory.Instance, pool, maxPerSide: 3, npcMarginIgnored: 0f);
        Redraw();

        if (Camera.main)
            transform.rotation = Quaternion.LookRotation(Camera.main.transform.forward, Vector3.up);

        gameObject.SetActive(true);
    }

    public void Redraw()
    {
        Clear(playerGrid); Clear(npcGrid);
        if (Offer == null) return;

        foreach (var it in Offer.playerGive) Instantiate(slotPrefab, playerGrid).Bind(it);
        foreach (var it in Offer.npcGive)    Instantiate(slotPrefab, npcGrid).Bind(it);

        // ✔ устойчивый расчёт: округлённая относительная разница
        UpdateFairnessMarker(Offer.FairnessRounded);

        Debug.Log($"FAIR chk: npc={Offer.npcValue:0.000}  player={Offer.playerValue:0.000}  rel={(Offer.FairnessRel*100f):0.00}%");
    }

    void UpdateFairnessMarker(float fairnessRel)
    {
        if (!markerRect || !startRed || !startYellow || !startGreen || !endGreen) return;

        float f = Mathf.Clamp(fairnessRel, F_MIN, F_MAX);
        if (Mathf.Abs(f) < EPS) f = 0f; // подавляем дрожание ±0.2%

        float xR = startRed.anchoredPosition.x;
        float xY = startYellow.anchoredPosition.x;
        float xG = startGreen.anchoredPosition.x;
        float xE = endGreen.anchoredPosition.x;

        float leftX  = Mathf.Min(xR, xY, xG, xE);
        float rightX = Mathf.Max(xR, xY, xG, xE);
        xR = leftX;  xE = rightX;
        xY = Mathf.Clamp(xY, xR, xE);
        xG = Mathf.Clamp(xG, xY, xE);

        float x;

        if (f < F_BAD_END) // 🔴 Bad
        {
            float t = Mathf.InverseLerp(F_MIN, F_BAD_END, f);
            x = Mathf.Lerp(xR, xY, t);
            SetLabel("Bad Trade", new Color(1f, 0.25f, 0.25f));
        }
        else if (f <= F_FAIR_END) // 🟡 Fair (−1%..+3%), нессиметрично
        {
            float t = (f <= 0f)
                ? Mathf.InverseLerp(F_BAD_END, 0f, f) * 0.5f
                : 0.5f + Mathf.InverseLerp(0f, F_FAIR_END, f) * 0.5f;
            x = Mathf.Lerp(xY, xG, t);
            SetLabel("Fair Trade", new Color(1f, 0.85f, 0.25f));
        }
        else // 🟢 Good
        {
            float t = Mathf.InverseLerp(F_FAIR_END, F_MAX, f);
            x = Mathf.Lerp(xG, xE, t);
            SetLabel("Good Trade", new Color(0.35f, 1f, 0.35f));
        }

        _markerTargetX = x;
        // markerLabel.text = $"{markerLabel.text} ({f*100f:+0.0;-0.0;0}%)"; // если нужен точный %
    }

    void SetLabel(string text, Color color)
    {
        if (!markerLabel) return;
        markerLabel.text = text;
        markerLabel.color = color;
    }

    void Clear(Transform rt)
    {
        if (!rt) return;
        for (int i = rt.childCount - 1; i >= 0; i--) Destroy(rt.GetChild(i).gameObject);
    }

    void CloseWith(string result)
    {
        Debug.Log($"[TradePanel] Button: {result}  (npcValue={Offer?.npcValue:0.0}, playerValue={Offer?.playerValue:0.0}, fairnessRel={Offer?.FairnessRel:0.000})");
        _onClose?.Invoke(result);
    }
}
