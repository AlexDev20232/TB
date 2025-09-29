// Assets/Scripts/BaseController.cs
using System.Collections.Generic;
using UnityEngine;
using YG;

[System.Serializable]
public class point
{
    public int index;
    public Transform FreeSlot;
    public bool isUsed = false;
    public bool isReserved = false;       // бронь, пока бот бежит
    public Transform incomePointTrans;
}

public class BaseController : MonoBehaviour
{
    public static BaseController Instance { get; private set; }

    [Header("Точки 1-го этажа")]
    [SerializeField] private Transform baseEntrance;
    [SerializeField] public List<point> availableSlots = new List<point>();

    [Header("2-й этаж")]
    [SerializeField] private GameObject secondFloorRoot;
    [SerializeField] public List<point> availableSlots2 = new List<point>();

    [Header("3-й этаж")]
    [SerializeField] private GameObject thirdFloorRoot;
    [SerializeField] public List<point> availableSlots3 = new List<point>();

    [SerializeField] private Brainrot[] extraBrainrots;
    [SerializeField] private Brainrot[] extraPackBrainrots;

    private void Awake() => Instance = this;

    private void Start()
    {
        // 1) Восстанавливаем размещённых ботов из сейва
        SaveBridge.LoadSlots();

        // 2) Переключаем визуальное состояние этажей (ON/OFF и сколько площадок открыто)
        RestoreFloorStateFromSaves(2);
        RestoreFloorStateFromSaves(3);

        // 3) Подстраховка: если что-то разблокировано, но не стоит на базе — доспаунить
        EnsureAllUnlockedArePlaced();
    }


    /// <summary>
/// Универсальный поиск слота по ЛЮБОМУ объекту, стоящему на FreeSlot (яйцо/брайрот/любой child).
/// </summary>
public bool TryFindSlotOfAny(Transform childOnSlot, out SlotFloor floor, out int index, out point slot)
{
    // поднимаемся по родителям до компонента point
    point found = childOnSlot ? childOnSlot.GetComponentInParent<point>(true) : null;
    if (found == null) { floor = 0; index = -1; slot = null; return false; }
    bool ok = TryGetFloorAndIndex(found, out floor, out index);
    slot = ok ? found : null;
    return ok;
}


    private void EnsureAllUnlockedArePlaced()
    {
        if (SaveBridge.Saves?.unlocked == null || SaveBridge.Saves.unlocked.Count == 0)
            return;

        // Быстрая проверка: стоит ли юнит с таким key на каком-то этаже
        bool IsKeyPlaced(string key)
        {
            bool CheckList(List<point> list)
            {
                foreach (var p in list)
                {
                    if (p?.FreeSlot == null) continue;
                    var ctrl = p.FreeSlot.GetComponentInChildren<BrainrotController>();
                    if (!ctrl) continue;

                    var so = ctrl.GetStats();
                    if (so && BrainrotUnlocks.InternalKey(so) == key) return true;
                }
                return false;
            }
            return CheckList(availableSlots) || CheckList(availableSlots2) || CheckList(availableSlots3);
        }

        // ВАЖНО: перед попыткой доспауна убедимся, что визуал этажей соответствует сейву
        RefreshFloorsVisual();

        int placedNow = 0;

        foreach (var key in SaveBridge.Saves.unlocked)
        {
            if (string.IsNullOrEmpty(key)) continue;
            if (IsKeyPlaced(key)) continue; // уже стоит — ок

            var so = FindBrainrotByKey(key);
            if (!so) continue;              // не нашли SO — пропускаем (лог напишет сам FindBrainrotByKey)

            // Аккуратная посадка через штатный метод (ищет активные свободные слоты)
            if (TrySpawnOwnedBrainrot(so))
                placedNow++;
        }

        if (placedNow > 0)
        {
            Debug.Log($"[Base] EnsureAllUnlockedArePlaced: доспавнено {placedNow} юнит(ов).");
            SaveBridge.SnapshotAndSave(force: true);
        }
    }
    // ── ПУБЛИЧНОЕ API ────────────────────────────────────────────────────
    public Vector3 GetBaseEntrancePosition() => baseEntrance.position;

    public point GetEmptySlot()
    {
        var slot = FindFirstUsableSlot();
        if (slot == null)
            Debug.LogWarning("[BaseController] Нет свободных слотов на активных этажах!");
        return slot;
    }

    public bool TrySpawnOwnedBrainrot(Brainrot so, int storedIncome = 0)
    {
        // Проверяем входной SO
        if (so == null) return false;

        // Пытаемся забронировать слот
        if (!TryReserveSlot(out var floor, out var idx, out var slot))
            return false;

        GameObject inst = null;
        try
        {
            // Спавним персонажа на забронированной площадке
            inst = Instantiate(so.characterPrefab, slot.FreeSlot.position, slot.FreeSlot.rotation, slot.FreeSlot);

            // Настройка поворота, если у префаба есть оффсет у ходьбы/позиционирования
            var mover = inst.GetComponent<BrainrotMover>();
            if (mover)
            {
                inst.transform.rotation = slot.FreeSlot.rotation * mover.GetOffsetQuat();
                mover.currentState = BrainrotMover.MoveState.Positioning;
            }

            // Инициализация контроллера
            var ctrl = inst.GetComponent<BrainrotController>();
            if (ctrl)
            {
                ctrl.Init(so);
                ctrl.MarkBought();
                // Сигнализируем контроллеру о "прибытии"
                ctrl.OnReachedPosition(slot);
            }

            // Устанавливаем накопленный доход (если пришёл из сейва)
            if (storedIncome > 0)
                inst.GetComponentInChildren<IncomeDisplay>()?.SetIncome(storedIncome);

            // Спавним UI над персонажем
            SpawnBrainrotUI(so, inst.transform);

            // КРИТИЧЕСКОЕ: немедленно подтверждаем занятие слота.
            // Это переведёт слот из "reserved" в "used" и сохранит состояние.
            ConfirmSlotOccupied(floor, idx);

            // Форс-сейв состояния базы (на всякий случай)
            SaveBridge.SnapshotAndSave(force: true);
            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[Base] TrySpawnOwnedBrainrot exception: {e.Message}");
            // Если что-то пошло не так — снимаем бронь, чтобы слот не завис в reserved
            ReleaseSlot(floor, idx);
            return false;
        }
    }
    public bool HasBrainrotByName(string name)
    {
        bool Check(List<point> list)
        {
            foreach (var p in list)
            {
                if (!p.isUsed || p.FreeSlot == null) continue;

                var ctrl = p.FreeSlot.GetComponentInChildren<BrainrotController>();
                if (ctrl == null) continue;

                var so = ctrl.GetStats();
                if (so != null && so.characterName == name)
                    return true;
            }
            return false;
        }

        return Check(availableSlots) || Check(availableSlots2) || Check(availableSlots3);
    }
    public bool IsBrainrotPlaced(Brainrot so)
    {
        bool Check(List<point> list)
        {
            foreach (var p in list)
                if (p.isUsed)
                {
                    var c = p.FreeSlot ? p.FreeSlot.GetComponentInChildren<BrainrotController>() : null;
                    if (c && c.MatchesTarget(so)) return true;
                }
            return false;
        }
        return Check(availableSlots) || Check(availableSlots2) || Check(availableSlots3);
    }

    // Ребёрт: очищает только ботов и доход, не ломая платформы
    public void ResetBase()
    {
        void Clear(List<point> list)
        {
            foreach (var p in list)
            {
                if (p == null) continue;

                if (p.FreeSlot)
                {
                    var ctrl = p.FreeSlot.GetComponentInChildren<BrainrotController>(true);
                    if (ctrl) Destroy(ctrl.gameObject);
                }

                if (p.incomePointTrans)
                {
                    var income = p.incomePointTrans.GetComponentInChildren<IncomeDisplay>(true);
                    if (income) Destroy(income.gameObject);
                }

                p.isUsed = false;
                p.isReserved = false;
            }
        }

        Clear(availableSlots);
        Clear(availableSlots2);
        Clear(availableSlots3);

        IncomeDisplay.ResetAllInScene();

        SaveBridge.Saves.placed.Clear();
        SaveBridge.SnapshotAndSave(force: true);
    }

    // Полная визуальная очистка (для хард-ресета)
    public void HardVisualWipe()
    {
        void Clear(List<point> list, bool disablePoints)
        {
            foreach (var p in list)
            {
                if (p?.FreeSlot)
                {
                    var ctrl = p.FreeSlot.GetComponentInChildren<BrainrotController>(true);
                    if (ctrl) Destroy(ctrl.gameObject);

                    if (disablePoints)
                        p.FreeSlot.gameObject.SetActive(false);
                }
                if (p?.incomePointTrans)
                {
                    var inc = p.incomePointTrans.GetComponentInChildren<IncomeDisplay>(true);
                    if (inc) Destroy(inc.gameObject);
                }
                if (p != null) { p.isUsed = false; p.isReserved = false; }
            }
        }

        Clear(availableSlots, false);
        Clear(availableSlots2, true);
        Clear(availableSlots3, true);

        if (secondFloorRoot) secondFloorRoot.SetActive(false);
        if (thirdFloorRoot) thirdFloorRoot.SetActive(false);

        IncomeDisplay.ResetAllInScene();
    }

    // ── БРОНИРОВАНИЕ / ПОСАДКА ──────────────────────────────────────────
    public enum SlotFloor { Floor1 = 0, Floor2 = 1, Floor3 = 2 }

    private static bool IsSlotUsable(point p)
    {
        return p != null
            && !p.isUsed
          && !p.isReserved
            && p.FreeSlot != null
            && p.FreeSlot.gameObject.activeInHierarchy;
    }




    private static bool IsSlotFree(point p) =>
    p != null && !p.isUsed && p.FreeSlot && p.FreeSlot.gameObject.activeInHierarchy;

    // Найти первый свободный слот, игнорируя бронь
    public bool TryFindFreeSlotIgnoringReservation(out SlotFloor floor, out int index, out point slot)
    {
        for (int i = 0; i < availableSlots.Count; i++)
            if (IsSlotFree(availableSlots[i])) { floor = SlotFloor.Floor1; index = i; slot = availableSlots[i]; return true; }

        for (int i = 0; i < availableSlots2.Count; i++)
            if (IsSlotFree(availableSlots2[i])) { floor = SlotFloor.Floor2; index = i; slot = availableSlots2[i]; return true; }

        for (int i = 0; i < availableSlots3.Count; i++)
            if (IsSlotFree(availableSlots3[i])) { floor = SlotFloor.Floor3; index = i; slot = availableSlots3[i]; return true; }

        floor = 0; index = -1; slot = null; return false;
    }

    // По ссылке на point — получить его этаж и индекс
    public bool TryGetFloorAndIndex(point s, out SlotFloor floor, out int index)
    {
        for (int i = 0; i < availableSlots.Count; i++)
            if (availableSlots[i] == s) { floor = SlotFloor.Floor1; index = i; return true; }
        for (int i = 0; i < availableSlots2.Count; i++)
            if (availableSlots2[i] == s) { floor = SlotFloor.Floor2; index = i; return true; }
        for (int i = 0; i < availableSlots3.Count; i++)
            if (availableSlots3[i] == s) { floor = SlotFloor.Floor3; index = i; return true; }

        floor = 0; index = -1; return false;
    }

    public bool TryFindFreeSlot(out SlotFloor floor, out int index, out point slot)
    {
        // 1F
        for (int i = 0; i < availableSlots.Count; i++)
            if (IsSlotUsable(availableSlots[i]))
            { floor = SlotFloor.Floor1; index = i; slot = availableSlots[i]; return true; }

        // 2F
        for (int i = 0; i < availableSlots2.Count; i++)
            if (IsSlotUsable(availableSlots2[i]))
            { floor = SlotFloor.Floor2; index = i; slot = availableSlots2[i]; return true; }

        // 3F
        for (int i = 0; i < availableSlots3.Count; i++)
            if (IsSlotUsable(availableSlots3[i]))
            { floor = SlotFloor.Floor3; index = i; slot = availableSlots3[i]; return true; }

        floor = 0; index = -1; slot = null; return false;
    }

    public bool TryReserveSlot(out SlotFloor floor, out int index, out point slot)
    {
        if (!TryFindFreeSlot(out floor, out index, out slot)) return false;
        slot.isReserved = true;
        return true;
    }



    public void ConfirmSlotOccupied(SlotFloor floor, int index)
    {
        var list = GetListForFloor(floor);
        if (index < 0 || index >= list.Count || list[index] == null) return;

        list[index].isReserved = false;
        list[index].isUsed = true;
        SaveBridge.SnapshotAndSave();
    }

    public void ReleaseSlot(SlotFloor floor, int index)
    {
        var list = GetListForFloor(floor);
        if (index < 0 || index >= list.Count || list[index] == null) return;

        list[index].isReserved = false;
    }

    // Для продажи/удаления бота
    public bool TryFindSlotOf(BrainrotController ctrl, out SlotFloor floor, out int index, out point slot)
    {
        if (FindInList(availableSlots, SlotFloor.Floor1, ctrl, out floor, out index, out slot)) return true;
        if (FindInList(availableSlots2, SlotFloor.Floor2, ctrl, out floor, out index, out slot)) return true;
        if (FindInList(availableSlots3, SlotFloor.Floor3, ctrl, out floor, out index, out slot)) return true;

        floor = 0; index = -1; slot = null;
        return false;
    }

    public void FreeSlot(SlotFloor floor, int index, bool removeIncome = true)
    {
        var list = GetListForFloor(floor);
        if (index < 0 || index >= list.Count) return;
        var p = list[index];
        if (p == null) return;

        if (removeIncome && p.incomePointTrans)
        {
            var income = p.incomePointTrans.GetComponentInChildren<IncomeDisplay>(true);
            if (income) Destroy(income.gameObject);
        }

        p.isReserved = false;
        p.isUsed = false;

        SaveBridge.SnapshotAndSave(force: true);
    }

    // ── ВОССТАНОВЛЕНИЕ ИЗ СЕЙВА ─────────────────────────────────────────
   public void RestoreFromSave(List<SlotSave> saved)
{
    if (saved == null) return;

    foreach (var s in saved)
    {
        var list = s.floor == 1 ? availableSlots2
                 : s.floor == 2 ? availableSlots3
                 :                availableSlots;

        if (s.slotIndex < 0 || s.slotIndex >= list.Count) continue;
        var slot = list[s.slotIndex];
        if (!slot?.FreeSlot) continue;

        if (!slot.FreeSlot.gameObject.activeSelf)
            slot.FreeSlot.gameObject.SetActive(true);

        if (s.isEgg)
        {
            // восстановление ЯЙЦА
            var egg = FindEggByName(s.eggKey);
            if (!egg) continue;

            var tp = TypeOfEgg.GetParamsForType(egg, (StandardType)s.eggType);
            if (tp == null || !tp.characterPrefab) continue;

            var go = Instantiate(tp.characterPrefab, slot.FreeSlot.position, slot.FreeSlot.rotation, slot.FreeSlot);
            var ec = go.GetComponent<EggController>() ?? go.AddComponent<EggController>();
            ec.egg = egg;
            ec.eggType = (StandardType)s.eggType;
            ec.OnPlaced(slot);

            slot.isUsed = true;
            slot.isReserved = false;
        }
        else
        {
            // восстановление БРАЙРОТА
            var so = FindBrainrotByKey(s.brainrotKey);
            if (!so) continue;

            var inst = Instantiate(so.characterPrefab, slot.FreeSlot.position, slot.FreeSlot.rotation, slot.FreeSlot);

            var mover = inst.GetComponent<BrainrotMover>();
            if (mover) inst.transform.rotation = slot.FreeSlot.rotation * mover.GetOffsetQuat();

            var ctrl = inst.GetComponent<BrainrotController>();
            if (ctrl) { ctrl.Init(so); ctrl.MarkBought(); ctrl.OnReachedPosition(slot); }

            if (s.weightKg > 0.001f)
{
    var econ = inst.AddComponent<BrainrotEconomy>();
    econ.kg = s.weightKg;

    // масштаб, исходя из SO
    float scale = so.baseScale * (1f + econ.kg * so.scalePerKg);
    inst.transform.localScale = Vector3.one * Mathf.Max(0.01f, scale);

    // верхняя табличка: вписать вес
    var bp = inst.GetComponentInChildren<BrainrotParametrs>(true);
    if (bp) bp.SetWeight(econ.kg);
}

            inst.GetComponentInChildren<IncomeDisplay>()?.SetIncome(s.storedIncome);
            SpawnBrainrotUI(so, inst.transform);

            slot.isUsed = true;
            slot.isReserved = false;
        }
    }
}

// Поиск яйца по имени (если используешь сейв яиц)
private EggScriptableObject FindEggByName(string name)
{
    if (string.IsNullOrEmpty(name)) return null;

    var cat = GameManager.Instance ? GameManager.Instance.TypeSet : null;
    if (cat && cat.allEgs != null)
    {
        foreach (var e in cat.allEgs)
            if (e && e.name == name) return e;
    }

    var all = Resources.LoadAll<EggScriptableObject>("");
    foreach (var e in all)
        if (e && e.name == name) return e;

    Debug.LogWarning($"[Save] Egg with key '{name}' not found!");
    return null;
}

    // ── ОТКРЫТИЕ ЭТАЖЕЙ/СЛОТОВ ──────────────────────────────────────────
    public void UnlockFloorSlots(int floorNumber, int count)
    {
        Debug.Log($"[Base] UnlockFloorSlots({floorNumber}, {count})");
        if (count <= 0) return;

        EnsureFloorOn(floorNumber);             // гарантированно включаем этаж
        for (int i = 0; i < count; i++)
            if (!UnlockNextFloorSlot(floorNumber))
                break;

        // после изменения сейва сразу обновим визуал
        RestoreFloorStateFromSaves(floorNumber);
    }

    public bool UnlockNextFloorSlot(int floorNumber)
    {
        var list = GetListForFloor((SlotFloor)(floorNumber - 1));
        if (list == null || list.Count == 0) return false;

        int already = floorNumber == 2 ? SaveBridge.Saves.secondFloorSlotsUnlocked
                   : floorNumber == 3 ? SaveBridge.Saves.thirdFloorSlotsUnlocked
                   : 0;

        if (already >= list.Count)
        {
            Debug.Log($"[Base] Все слоты на {floorNumber}-м этаже уже открыты.");
            return false;
        }

        // активируем ПЛОЩАДКУ прямо сейчас, чтобы было видно без перезахода
        var slot = list[already];
        if (slot?.FreeSlot) slot.FreeSlot.gameObject.SetActive(true);

        if (floorNumber == 2) SaveBridge.Saves.secondFloorSlotsUnlocked = already + 1;
        else if (floorNumber == 3) SaveBridge.Saves.thirdFloorSlotsUnlocked = already + 1;

        SaveBridge.SnapshotAndSave();
        Debug.Log($"[Base] Открыл слот #{already + 1} на {floorNumber}-м этаже.");
        return true;
    }

    private void EnsureFloorOn(int floorNumber)
    {
        if (floorNumber == 2)
        {
            if (!SaveBridge.Saves.secondFloorOpened)
            {
                if (secondFloorRoot) secondFloorRoot.SetActive(true);
                SaveBridge.Saves.secondFloorOpened = true;
                SaveBridge.SnapshotAndSave();
            }
        }
        else if (floorNumber == 3)
        {
            if (!SaveBridge.Saves.thirdFloorOpened)
            {
                if (thirdFloorRoot) thirdFloorRoot.SetActive(true);
                SaveBridge.Saves.thirdFloorOpened = true;
                SaveBridge.SnapshotAndSave();
            }
        }
    }
    public void RefreshFloorsVisual()
    {
        RestoreFloorStateFromSaves(2);
        RestoreFloorStateFromSaves(3);
    }
    public void RestoreFloorStateFromSaves(int floorNumber)
    {
        var (opened, unlocked) = floorNumber == 2
            ? (SaveBridge.Saves.secondFloorOpened, SaveBridge.Saves.secondFloorSlotsUnlocked)
            : (SaveBridge.Saves.thirdFloorOpened, SaveBridge.Saves.thirdFloorSlotsUnlocked);

        var root = floorNumber == 2 ? secondFloorRoot : thirdFloorRoot;
        var list = floorNumber == 2 ? availableSlots2 : availableSlots3;

        if (root) root.SetActive(opened);

        int n = Mathf.Clamp(unlocked, 0, list.Count);
        for (int i = 0; i < list.Count; i++)
        {
            var slot = list[i];
            if (slot?.FreeSlot)
            {
                slot.FreeSlot.gameObject.SetActive(i < n);
                slot.isUsed = false;
                slot.isReserved = false;
            }
        }
    }

    // ── ПОМОЩНИКИ ────────────────────────────────────────────────────────
    private List<point> GetListForFloor(SlotFloor f)
    {
        switch (f)
        {
            case SlotFloor.Floor1: return availableSlots;
            case SlotFloor.Floor2: return availableSlots2;
            case SlotFloor.Floor3: return availableSlots3;
        }
        return null;
    }

    private GameObject GetRootForFloor(SlotFloor f)
    {
        switch (f)
        {
            case SlotFloor.Floor1: return null;
            case SlotFloor.Floor2: return secondFloorRoot;
            case SlotFloor.Floor3: return thirdFloorRoot;
        }
        return null;
    }

    private bool FindInList(List<point> list, SlotFloor f, BrainrotController ctrl,
                            out SlotFloor floor, out int index, out point slot)
    {
        for (int i = 0; i < list.Count; i++)
        {
            var p = list[i];
            if (p?.FreeSlot && p.FreeSlot.GetComponentInChildren<BrainrotController>() == ctrl)
            { floor = f; index = i; slot = p; return true; }
        }
        floor = 0; index = -1; slot = null; return false;
    }

    private point FindFirstUsableSlot()
    {
        foreach (var p in availableSlots) if (IsSlotUsable(p)) return p;
        foreach (var p in availableSlots2) if (IsSlotUsable(p)) return p;
        foreach (var p in availableSlots3) if (IsSlotUsable(p)) return p;
        return null;
    }

     public void SpawnBrainrotUI(Brainrot so, Transform characterRoot)
    {
        GameObject uiPrefab = GameManager.Instance.UICanvasPrefab;
        if (!uiPrefab) return;

        float extraY = GameManager.Instance.ExtraYOffset;
        Transform anchor = characterRoot.Find("CanvasActor");
        if (!anchor) return;

        GameObject ui = Instantiate(uiPrefab, anchor.position + Vector3.up * extraY,
                                    Quaternion.identity, anchor);

        var bp = ui.GetComponent<BrainrotParametrs>();
        if (bp)
        {
            bp.Init(so.characterName, so.rarity, so.incomePerSecond, so.price, so.type);
            foreach (var rc in GameManager.Instance.rarityColors)
                if (rc.rarity == so.rarity) { bp.SetRarityColor(rc.color); break; }
        }
    }

    // ──────────────────────────────────────────────────────────────────────────
    // ИЩЕМ Brainrot по внутреннему ключу среди ВСЕХ яиц каталога + extras + Resources
    // ──────────────────────────────────────────────────────────────────────────
    private Brainrot FindBrainrotByKey(string key)
    {
        if (string.IsNullOrEmpty(key)) return null;

        // 0) Каталог яиц в GameManager
        var cat = GameManager.Instance ? GameManager.Instance.TypeSet : null;

        // 1) Перебираем все яйца и их TypeParametrs → EggBrainrot[]
        //    Это заменяет старые массивы standard/gold/diamond/Candy
        if (cat && cat.allEgs != null)
        {
            foreach (var egg in cat.allEgs)
            {
                if (!egg || egg.TypeParametrs == null) continue;

                foreach (var tp in egg.TypeParametrs)
                {
                    if (tp == null || tp.EggBrainrot == null) continue;

                    foreach (var b in tp.EggBrainrot)
                        if (b && BrainrotUnlocks.InternalKey(b) == key)
                            return b;
                }
            }
        }

        // 2) Дополнительные пуллы (extras), если ты туда кладёшь уникальные SO
        if (extraBrainrots != null)
            foreach (var extra in extraBrainrots)
                if (extra && BrainrotUnlocks.InternalKey(extra) == key)
                    return extra;

        if (extraPackBrainrots != null)
            foreach (var extra in extraPackBrainrots)
                if (extra && BrainrotUnlocks.InternalKey(extra) == key)
                    return extra;

        // 3) Fallback — пройтись по всем Brainrot в Resources (если хранишь там)
        foreach (var b in Resources.LoadAll<Brainrot>(""))
            if (b && BrainrotUnlocks.InternalKey(b) == key)
                return b;

        Debug.LogWarning($"[Save] Brainrot with key '{key}' not found in eggs/extras/resources!");
        return null;
    }


// Вернуть перечисление всех Brainrot из каталога яиц (без дублей)
private IEnumerable<Brainrot> EnumerateAllCatalogBrainrots()
{
    var cat = GameManager.Instance ? GameManager.Instance.TypeSet : null;
    if (!cat || cat.allEgs == null) yield break;

    var seen = new HashSet<Brainrot>();
    foreach (var egg in cat.allEgs)
    {
        if (!egg || egg.TypeParametrs == null) continue;
        foreach (var tp in egg.TypeParametrs)
        {
            if (tp == null || tp.EggBrainrot == null) continue;
            foreach (var b in tp.EggBrainrot)
            {
                if (b && seen.Add(b))
                    yield return b;
            }
        }
    }
}


}
