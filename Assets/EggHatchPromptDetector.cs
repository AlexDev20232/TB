// Assets/Scripts/Eggs/UI/EggHatchPromptDetector.cs
using UnityEngine;

public class EggHatchPromptDetector : MonoBehaviour
{
    [Header("Параметры")]
    [Tooltip("Радиус поиска готового яйца вокруг игрока")]
    public float radius = 2.5f;

    [Tooltip("Префаб подсказки 'E — Hatch Now' (Canvas в World Space)")]
    public GameObject hatchPromptPrefab;

    private GameObject    _prompt;     // инстанс подсказки
    private EggController _targetEgg;  // текущее ближайшее готовое яйцо
    
    public GameObject pickPromptPrefab;

    private void OnEnable() => EggController.OnHatchStarted += OnHatchStarted;
private void OnDisable() => EggController.OnHatchStarted -= OnHatchStarted;

private void OnHatchStarted(EggController ec)
{
    if (_targetEgg == ec) Hide();
}


   void Update()
{
    // если текущая цель началась крутиться — убираем
    if (_targetEgg && _targetEgg.IsHatching) { Hide(); }

    // 1) Готовое яйцо рядом (и не крутится) → показать "Hatch Now"
    var egg = FindNearestReadyEgg();      // уже фильтрует IsHatching
    if (egg != null)
    {
        Show(egg);

        if (Input.GetKeyDown(KeyCode.E))
        {
            _targetEgg?.HatchNow();
            
            Hide();
        }
        return;                            // ⬅️ важен ранний выход!
    }

    // 2) Если яйца нет → дать подсказку для стоящего брайрота
   var br = FindNearestBrainrotOnSlot();
    if (br != null)
    {
        ShowPick(br); // показываем подсказку "Взять брайрота"

        if (Input.GetKeyDown(KeyCode.E))
        {
            CollectBrainrot(br);
            Hide(); // убираем подсказку
        }
        return;
    }

    // 3) Ничего подходящего — убираем
    Hide();
}
private void CollectBrainrot(BrainrotController br)
{
    if (!br) return;
    var so = br.GetStats();
    if (!so) return;

    // тип можно брать из so.type
    var type = so.type;

    // вес — из BrainrotEconomy (если нет — 0)
    float kg = 0f;
    var econ = br.GetComponent<BrainrotEconomy>();
    if (econ) kg = econ.kg;

    // 1) снять со слота
    if (BaseController.Instance.TryFindSlotOf(br, out var floor, out var idx, out var slot))
    {
        var id = slot.incomePointTrans ? slot.incomePointTrans.GetComponentInChildren<IncomeDisplay>(true) : null;
        if (id) Destroy(id.gameObject);

        var topUi = br.GetComponentInChildren<BrainrotParametrs>(true);
        if (topUi) Destroy(topUi.gameObject);

        Destroy(br.gameObject);
        BaseController.Instance.FreeSlot(floor, idx, removeIncome: false);
    }
    else
    {
        Destroy(br.gameObject);
    }

    // 2) Положить в ЯИЧНЫЙ ИНВЕНТАРЬ как ОТДЕЛЬНЫЙ слот (без стека)
    if (EggInventory.Instance != null)
    {
        bool added = EggInventory.Instance.AddBrainrot(so, type, kg);
        if (!added)
            GameManager.Instance?.ErrorMessage("Инвентарь заполнен");
    }

    SaveBridge.SnapshotAndSave(force: true);
}


private BrainrotController FindNearestBrainrotOnSlot()
{
    var bots = FindObjectsOfType<BrainrotController>();
    BrainrotController best = null;
    float bestD = float.MaxValue;
    var pos = transform.position;

    foreach (var b in bots)
    {
        if (!b) continue;
        var mv = b.GetComponent<BrainrotMover>();
        if (mv && mv.currentState != BrainrotMover.MoveState.Positioning) continue; // нас интересуют только «стоящие»
        float d = Vector3.Distance(pos, b.transform.position);
        if (d <= radius && d < bestD) { best = b; bestD = d; }
    }
    return best;
}

private void ShowPick(BrainrotController br)
{
    if (!_prompt && pickPromptPrefab) _prompt = Instantiate(pickPromptPrefab);
    if (!_prompt) return;

    _prompt.transform.position = br.transform.position + Vector3.up * 1.2f;
    if (Camera.main) _prompt.transform.rotation = Quaternion.LookRotation(Camera.main.transform.forward);

    var pp = _prompt.GetComponentInChildren<PurchasePrompt>();
    if (pp) { pp.SetName("Взять брайрота"); pp.SetWalkingMode(0); }
}


  private EggController FindNearestReadyEgg()
{
    var eggs = FindObjectsOfType<EggController>();
    EggController best = null;
    float bestD = float.MaxValue;
    Vector3 pos = transform.position;

    foreach (var e in eggs)
    {
        if (!e || !e.IsPurchased || !e.IsReady || e.IsHatching) continue; // ⬅️ это уже есть у тебя
        float d = Vector3.Distance(pos, e.transform.position);
        if (d <= radius && d < bestD) { best = e; bestD = d; }
    }
    return best;
}



    private void Show(EggController egg)
    {
        if (egg == null) { Hide(); return; }
        _targetEgg = egg;

        if (!_prompt && hatchPromptPrefab)
            _prompt = Instantiate(hatchPromptPrefab);

        if (_prompt)
        {
            // позиция: над якорем infoAnchor (если есть) — иначе над центром яйца
            Transform anchor = egg.infoAnchor ? egg.infoAnchor : egg.transform;
            _prompt.transform.position = anchor.position + Vector3.up * 0.2f;

            if (Camera.main)
                _prompt.transform.rotation = Quaternion.LookRotation(Camera.main.transform.forward);

            // текст
            var pp = _prompt.GetComponentInChildren<PurchasePrompt>();
            if (pp)
            {
                pp.SetName("Hatch Now");
                pp.SetWalkingMode(0); // 0 — без цены (или сделай отдельный SetPlaceMode/SetHatchMode)
            }
        }
    }

    private void Hide()
    {
        _targetEgg = null;
        if (_prompt) { Destroy(_prompt); _prompt = null; }
    }
}
