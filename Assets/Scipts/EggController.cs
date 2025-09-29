using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EggController : MonoBehaviour
{
    [Header("Данные яйца")]
    public EggScriptableObject egg;
    public StandardType eggType;

    [Header("Инфо-панель (тот же Canvas, что и раньше)")]
    public GameObject infoUIPrefab;             // Canvas с EggInfoWidget
    public Transform  infoAnchor;               // якорь над яйцом
    public bool spawnInfoOnAwake = true;

    // ── состояния инкубации ─────────────────────────────────────────────
    public bool IsPurchased { get; private set; }
    public bool IsBought    { get; private set; }
    public bool IsReady     { get; private set; }
    private float _totalHatchSec;
    private float _remainSec;

    [Header("Hatch (прокрутка)")]
    [Tooltip("Материал поверх текущих, для затемнения «кандидатов».")]
    public Material previewDarkMaterial;
    [Tooltip("Длительность прокрутки (сек).")]
    public float reelDuration = 3.0f;
    [Tooltip("Базовый интервал между кандидатами (сек).")]
    public float reelStep = 0.2f;
    [Tooltip("Во сколько раз медленнее станет шаг к концу (замедление).")]
    public float reelSlowMult = 3f;
    [Tooltip("Кривая замедления (0..1) → 0=начало, 1=конец.")]
    public AnimationCurve reelEase = AnimationCurve.Linear(0, 0, 1, 1);
    [Tooltip("Масштаб кандидатов при прокрутке.")]
    public float previewScale = 1.0f;

    // runtime
    private GameObject    _infoUIInstance;
    private EggInfoWidget _widget;
    private point         _slotRef;
    private bool          _isHatching;
    private Renderer[]    _eggRenderers;        // чтобы прятать само яйцо

    public bool IsHatching => _isHatching;

    public static System.Action<EggController>       OnHatchStarted;
    public static System.Action<BrainrotController>  OnHatchFinished;

    // ── lifecycle ────────────────────────────────────────────────────────
    private void Awake()
    {
        if (spawnInfoOnAwake && egg != null) ForceSpawnInfoUI();
        CacheEggRenderers();
    }

    private void Update()
    {
        if (IsPurchased && !IsReady && _totalHatchSec > 0f)
        {
            _remainSec -= Time.deltaTime;
            if (_remainSec <= 0f) { _remainSec = 0f; IsReady = true; }
            UpdateIncubationUI();
        }
    }

    private void OnDestroy()
    {
        if (_infoUIInstance) Destroy(_infoUIInstance);
    }

    // ── публичные API ────────────────────────────────────────────────────
    public void Init(EggScriptableObject parameters, StandardType type)
    {
        egg     = parameters;
        eggType = type;
        ForceSpawnInfoUI();
        CacheEggRenderers();
    }

    public void OnPlaced(point slot) => OnReachedPosition(slot);

    public void OnReachedPosition(point slot)
    {
        IsPurchased = true;
        _slotRef    = slot;

        var mover = GetComponent<BrainrotMover>();
        if (mover) mover.currentState = BrainrotMover.MoveState.Positioning;

        GetComponentInChildren<Animator>()?.SetBool("Idle", true);

        _totalHatchSec = Mathf.Max(0f, egg ? egg.hatchSeconds : 0f);
        _remainSec     = _totalHatchSec;
        IsReady        = (_totalHatchSec <= 0.001f);

        ForceSpawnInfoUI();
        PrepareIncubationUI();
        UpdateIncubationUI();
        CacheEggRenderers();

        Debug.Log($"[EggController] {egg?.EggName} ({eggType}) на слоте {slot.index}. Инкубация: {_totalHatchSec}s");
    }

    public void MarkBought() => IsBought = true;
    public EggScriptableObject GetStats() => egg;
    public float GetRemainingIncubation() => Mathf.Max(0f, _remainSec);

    /// <summary>Вызвать, когда яйцо готово и игрок нажал «Hatch».</summary>
    public void HatchNow()
    {
        if (_isHatching) return;
        if (!IsReady) { Debug.Log("[EggController] HatchNow() — ещё не готово."); return; }

        if (_slotRef == null)
        {
            if (!BaseController.Instance.TryFindSlotOfAny(transform, out var _, out var _, out var p))
            { Debug.LogWarning("[EggController] Не найден слот для hatch."); return; }
            _slotRef = p;
        }

        StartCoroutine(HatchRoutine());
    }

    public void ForceSpawnInfoUI()
    {
        if (!egg || !infoUIPrefab) return;

        if (!infoAnchor)
        {
            var a = new GameObject("InfoAnchor").transform;
            a.SetParent(transform, false);
            a.localPosition = Vector3.up * 1.0f;
            infoAnchor = a;
        }

        if (_infoUIInstance == null)
        {
            _infoUIInstance = Instantiate(infoUIPrefab, infoAnchor);
            _infoUIInstance.transform.localPosition = Vector3.zero;
            _infoUIInstance.transform.localRotation = Quaternion.identity;
            _widget = _infoUIInstance.GetComponent<EggInfoWidget>();
        }
        if (_widget != null) _widget.Bind(egg, eggType);
    }

    // ── private ──────────────────────────────────────────────────────────
    private void CacheEggRenderers()
    {
        if (_eggRenderers == null)
            _eggRenderers = GetComponentsInChildren<Renderer>(true);
    }

    private void SetEggVisible(bool visible)
    {
        CacheEggRenderers();
        if (_eggRenderers == null) return;
        foreach (var r in _eggRenderers)
            if (r && (_infoUIInstance == null || r.gameObject != _infoUIInstance))
                r.enabled = visible;
    }

    private void PrepareIncubationUI()
    {
        if (_widget == null) return;
        _widget.SetPriceVisible(false);
        _widget.SetIncubationVisible(true);
    }

    private void UpdateIncubationUI()
    {
        if (_widget == null) return;
        if (!IsReady) _widget.UpdateIncubationUI(_totalHatchSec, _remainSec);
        else          _widget.UpdateIncubationUI(_totalHatchSec, 0f); // Ready!
    }

    // ── корутина хетча ───────────────────────────────────────────────────
    private IEnumerator HatchRoutine()
    {
        OnHatchStarted?.Invoke(this);
        _isHatching = true;

        // спрятать яйцо и панель
        SetEggVisible(false);
        if (_infoUIInstance) _infoUIInstance.SetActive(false);

        // пул кандидатов
        var tp = TypeOfEgg.GetParamsForType(egg, eggType);
        if (tp == null || tp.EggBrainrot == null || tp.EggBrainrot.Length == 0)
        { Debug.LogWarning("[EggController] Нет пулла Brainrot для hatch."); yield break; }

        var pool = new List<Brainrot>();
        foreach (var b in tp.EggBrainrot) if (b) pool.Add(b);

        // прокрутка с замедлением
        float elapsed = 0f;
        GameObject currentPreview = null;

        while (elapsed < reelDuration)
        {
            if (currentPreview) Destroy(currentPreview);

            var candidate = pool[Random.Range(0, pool.Count)];
            currentPreview = SpawnPreview(candidate, _slotRef, previewDarkMaterial, previewScale);

            // плавное появление превью (0 → target за 0.1с)
            StartCoroutine(ScaleIn(currentPreview.transform, 0.1f, currentPreview.transform.localScale));

            // шаг = Lerp(step, step*slow, ease(0..1))
            float x01  = Mathf.Clamp01(elapsed / reelDuration);
            float ease = reelEase != null ? reelEase.Evaluate(x01) : x01;
            float step = Mathf.Lerp(reelStep, reelStep * reelSlowMult, ease);

            yield return new WaitForSeconds(step);
            elapsed += step;
        }

        if (currentPreview) { Destroy(currentPreview); currentPreview = null; }

        // выбор победителя
        Brainrot winner = PickWinnerWeighted(pool);

        // финал: спавним бота корректно развёрнутым
        var finalGO = Instantiate(winner.characterPrefab,
                                  _slotRef.FreeSlot.position,
                                  _slotRef.FreeSlot.rotation,
                                  _slotRef.FreeSlot);

        // учтём yawOffset из BrainrotMover
        var mv = finalGO.GetComponent<BrainrotMover>();
        Quaternion faceRot = _slotRef.FreeSlot.rotation;
        if (mv) faceRot *= mv.GetOffsetQuat();
        finalGO.transform.SetPositionAndRotation(_slotRef.FreeSlot.position, faceRot);

        // глушим авто-вращатели
        foreach (var rot in finalGO.GetComponentsInChildren<Rotater>(true))
            rot.enabled = false;

        // (опц.) если префаб содержит узел "Model" — обнулим локальный поворот
        var model = finalGO.transform.Find("Model");
        if (model) model.localRotation = Quaternion.identity;

        // активируем контроллер бота
        var ctrl = finalGO.GetComponent<BrainrotController>();
        if (ctrl)
        {
            ctrl.Init(winner);
            ctrl.MarkBought();
            ctrl.OnReachedPosition(_slotRef);
        }


        // ── Экономика (у тебя уже есть):
var econ = finalGO.AddComponent<BrainrotEconomy>();
econ.SetupFrom(winner, winner.rarity, finalGO.transform, extraMult: 1f, baseAdd: 1.0f, applyScale: true);

// IncomeDisplay на слоте — задать $/s:
var id = _slotRef.incomePointTrans
         ? _slotRef.incomePointTrans.GetComponentInChildren<IncomeDisplay>(true)
         : null;
if (id) id.Init(Mathf.RoundToInt(econ.incomePerSec));

// ── СПАВНИТЬ ТАБЛИЧКУ ВЕРХОМ и ВПИСАТЬ kg/$s:
BaseController.Instance?.SpawnBrainrotUI(winner, finalGO.transform);
var bp = finalGO.GetComponentInChildren<BrainrotParametrs>(true);
if (bp)
{
    bp.SetIncomePerSec(Mathf.RoundToInt(econ.incomePerSec));
    bp.SetWeight(econ.kg);
}



        // сообщим — хетч завершён (детекторы подсказок могут отреагировать)
        if (ctrl) OnHatchFinished?.Invoke(ctrl);

        // поп-ап, если первый раз
       // поп-ап, если первый раз
// поп-ап, если это НОВЫЙ брайрот для коллекции
var key = BrainrotUnlocks.InternalKey(winner);
bool isNew = BrainrotUnlocks.TryUnlockNew(winner);
Debug.Log($"[NEWPET] HatchRoutine: winner={winner?.name} rarity={winner?.rarity} key={key} -> isNew={isNew}");

if (isNew)
{
    var icon = ResolvePetIcon(winner);
    Debug.Log($"[NEWPET] Popup call: service={(NewPetPopupService.I ? "OK" : "NULL")} icon={(icon ? "OK" : "NULL")}");
    NewPetPopupService.I?.ShowReplacing(icon, winner.characterName);
}




        // удалить яйцо и сохранить
        Destroy(gameObject);
        SaveBridge.SnapshotAndSave(force: true);
        _isHatching = false;
    }
private Sprite ResolvePetIcon(Brainrot b)
{
    if (!b) { Debug.Log("[NEWPET] ResolveIcon: SO null"); return null; }
    if (b.icon) { Debug.Log("[NEWPET] ResolveIcon: SO.icon OK"); return b.icon; }

    if (!b.iconPrefab) { Debug.Log("[NEWPET] ResolveIcon: iconPrefab NULL"); return null; }
    var set = b.iconPrefab.GetComponent<BrainrotIconSet>();
    if (set == null) { Debug.Log("[NEWPET] ResolveIcon: BrainrotIconSet MISSING"); return null; }
    if (set.skins == null || set.skins.Length == 0) { Debug.Log("[NEWPET] ResolveIcon: skins EMPTY"); return null; }

    Debug.Log("[NEWPET] ResolveIcon: iconPrefab/skins OK");
    return set.skins[0];
}




    // плавное появление из нуля масштаба
    private IEnumerator ScaleIn(Transform tr, float duration, Vector3 target)
    {
        if (!tr) yield break;
        Vector3 start = Vector3.zero;
        tr.localScale = start;
        duration = Mathf.Max(0.01f, duration);

        float t = 0f;
        while (t < duration && tr)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / duration);
            float e = 1f - (1f - k) * (1f - k); // easeOut
            tr.localScale = Vector3.LerpUnclamped(start, target, e);
            yield return null;
        }
        if (tr) tr.localScale = target;
    }

    private GameObject SpawnPreview(Brainrot so, point slot, Material darkMat, float scale = 1f)
    {
        var go = Instantiate(so.characterPrefab,
                             slot.FreeSlot.position,
                             slot.FreeSlot.rotation,
                             slot.FreeSlot);

        // правильный разворот «лицом вперёд»
        var mv = go.GetComponent<BrainrotMover>();
        if (mv) go.transform.rotation = slot.FreeSlot.rotation * mv.GetOffsetQuat();

        // вырубаем геймплей
        if (mv) mv.enabled = false;
        var bc = go.GetComponent<BrainrotController>(); if (bc) bc.enabled = false;
        foreach (var rb in go.GetComponentsInChildren<Rigidbody>(true)) Destroy(rb);
        foreach (var col in go.GetComponentsInChildren<Collider>(true)) Destroy(col);

        // «получёрный» – добавить материал поверх
        AddMaterialRecursive(go, darkMat);

        // выставим целевой масштаб (ScaleIn поставит 0 → target)
        if (Mathf.Abs(scale - 1f) > 0.001f)
            go.transform.localScale *= scale;

        return go;
    }

    private static void AddMaterialRecursive(GameObject root, Material mat)
    {
        if (!root || !mat) return;
        var renderers = root.GetComponentsInChildren<Renderer>(true);
        foreach (var r in renderers)
        {
            var src = r.materials;
            var dst = new Material[src.Length + 1];
            for (int i = 0; i < src.Length; i++) dst[i] = src[i];
            dst[dst.Length - 1] = mat; // слой «сверху»
            r.materials = dst;
        }
    }

    // выбор победителя по весам редкостей
    private Brainrot PickWinnerWeighted(List<Brainrot> pool)
    {
        var weightsByRarity = new Dictionary<CharacterRarity, int>();
        var rw = GameManager.Instance ? GameManager.Instance.RarityWeights : null;

        if (rw != null && rw.Length > 0)
        {
            foreach (var row in rw)
                if (!weightsByRarity.ContainsKey(row.rarity))
                    weightsByRarity[row.rarity] = Mathf.Max(0, row.weight);
        }
        else
        {
            // дефолт
            weightsByRarity[CharacterRarity.Common]    = 50;
            weightsByRarity[CharacterRarity.Rare]      = 30;
            weightsByRarity[CharacterRarity.Epic]      = 15;
            weightsByRarity[CharacterRarity.Legendary] = 5;
        }

        int total = 0;
        var candidates = new List<(Brainrot b, int w)>();
        foreach (var b in pool)
        {
            if (b == null) continue;
            int w = 1;
            weightsByRarity.TryGetValue(b.rarity, out w);
            if (w <= 0) continue;
            candidates.Add((b, w));
            total += w;
        }

        if (total <= 0) return pool[Random.Range(0, pool.Count)];

        int roll = Random.Range(0, total), acc = 0;
        foreach (var c in candidates)
        {
            acc += c.w;
            if (roll < acc) return c.b;
        }
        return candidates[candidates.Count - 1].b;
    }
}
