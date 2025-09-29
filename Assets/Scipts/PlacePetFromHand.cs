// Assets/Scripts/Inventory/PlacePetFromHand.cs
using UnityEngine;

/// <summary>
/// Если у игрока «в руках»/на поводке выбран брайрот, при подходе к свободному слоту
/// показывает подсказку "E — Place". По нажатию E:
///  - удаляет этого брайрота из инвентаря,
///  - инстансит на слот с тем же типом и весом,
///  - масштабирует по kg и пересчитывает доход,
///  - помечает слот занятым и сохраняет.
/// </summary>
public class PlacePetFromHand : MonoBehaviour
{
    [Header("Ссылки")]
    public HotbarBrainrotAPI hotbar;          // простой API к хотбару
    public GameObject placePromptPrefab;      // подсказка “E — Place” (World Space)

    [Header("Поиск слота")]
    public float searchRadius = 3.0f;
    public Vector3 promptOffset = new Vector3(0f, 1.2f, 0f);

    GameObject _promptGO;
    point _nearestSlot;
    BaseController.SlotFloor _nearestFloor;
    int _nearestIndex;

    void Reset()
    {
        if (!hotbar) hotbar = FindObjectOfType<HotbarBrainrotAPI>();
    }

    void Update()
    {
        // нет выбранного пета → скрыть подсказку
        if (hotbar == null || !hotbar.HasEquippedBrainrot())
        {
            HidePrompt();
            return;
        }

        // найти ближайший свободный слот
        if (!TryFindNearestFreeSlot(out _nearestFloor, out _nearestIndex, out _nearestSlot))
        {
            HidePrompt();
            return;
        }

        // показать подсказку
        if (_nearestSlot != null && _nearestSlot.FreeSlot != null)
            ShowPromptAt(_nearestSlot.FreeSlot.position + promptOffset);

        // поставить по E
        if (Input.GetKeyDown(KeyCode.E))
            TryPlaceEquipped();
    }

    // ───────────────────────────── поиск свободного ─────────────────────────────

    bool TryFindNearestFreeSlot(out BaseController.SlotFloor floor, out int index, out point slot)
    {
        floor = 0; index = -1; slot = null;
        float best = float.MaxValue;

        var bc = BaseController.Instance;
        if (bc == null) return false;

        // 1F
        for (int i = 0; i < bc.availableSlots.Count; i++)
        {
            var p = bc.availableSlots[i];
            if (IsSlotCandidate(p, ref best))
            { floor = BaseController.SlotFloor.Floor1; index = i; slot = p; }
        }
        // 2F
        for (int i = 0; i < bc.availableSlots2.Count; i++)
        {
            var p = bc.availableSlots2[i];
            if (IsSlotCandidate(p, ref best))
            { floor = BaseController.SlotFloor.Floor2; index = i; slot = p; }
        }
        // 3F
        for (int i = 0; i < bc.availableSlots3.Count; i++)
        {
            var p = bc.availableSlots3[i];
            if (IsSlotCandidate(p, ref best))
            { floor = BaseController.SlotFloor.Floor3; index = i; slot = p; }
        }

        return slot != null;

        // локальный помощник — валидный кандидат?
        bool IsSlotCandidate(point p, ref float bestDist)
        {
            if (p == null || p.FreeSlot == null) return false;
            if (p.isUsed || p.isReserved) return false;

            float d = Vector3.Distance(transform.position, p.FreeSlot.position);
            if (d > searchRadius) return false;
            if (d >= bestDist) return false;

            bestDist = d;
            return true;
        }
    }

    // ───────────────────────────── подсказка ─────────────────────────────

    void ShowPromptAt(Vector3 pos)
    {
        if (!placePromptPrefab) return;
        if (!_promptGO) _promptGO = Instantiate(placePromptPrefab);
        _promptGO.transform.position = pos;
        if (Camera.main)
            _promptGO.transform.rotation = Quaternion.LookRotation(Camera.main.transform.forward);
    }

    void HidePrompt()
    {
        if (_promptGO) { Destroy(_promptGO); _promptGO = null; }
    }

    // ───────────────────────────── постановка ─────────────────────────────

    void TryPlaceEquipped()
    {
        if (_nearestSlot == null || hotbar == null) return;

        // данные из хотбара
        Brainrot so            = hotbar.EquippedBrainrotSO();
        StandardType type      = hotbar.EquippedBrainrotType();
        float kg               = hotbar.EquippedBrainrotKg();
        GameObject petInstance = hotbar.CurrentPetInstance();

        if (!so)
        {
            Debug.LogWarning("[PlacePetFromHand] Нет SO у выбранного брайрота.");
            return;
        }

        // 1) удалить из инвентаря
        bool removed = EggInventory.Instance != null &&
                       EggInventory.Instance.RemoveBrainrot(so, type, kg);
        if (!removed)
        {
            Debug.LogWarning("[PlacePetFromHand] Не удалось удалить брайрота из инвентаря (не найден).");
            return;
        }

        // 2) инстанс на слот
        var parent = _nearestSlot.FreeSlot;
        GameObject go = Instantiate(so.characterPrefab, parent.position, parent.rotation, parent);

        // корректный разворот с учётом yawOffset
        var mv = go.GetComponent<BrainrotMover>();
        if (mv) go.transform.rotation = parent.rotation * mv.GetOffsetQuat();

        // 2.1 визуал из руки/поводка — удалить и очистить хотбар
        if (petInstance) Destroy(petInstance);
        hotbar.HideEquipped();

        // 3) масштаб по весу
        float scale = Mathf.Max(0.01f, so.baseScale * (1f + kg * so.scalePerKg));
        go.transform.localScale = Vector3.one * scale;

        // 4) базовый UI через контроллер (имя/редкость и т.п.)
        var ctrl = go.GetComponent<BrainrotController>();
        if (!ctrl) ctrl = go.AddComponent<BrainrotController>();
        ctrl.Init(so);
        ctrl.MarkBought();
        ctrl.OnReachedPosition(_nearestSlot); // это создаст IncomeDisplay c базовым incomePerSecond

        // 5) пересчитать доход по твоей формуле и обновить IncomeDisplay
        float income = EvaluateIncome(so, type, kg);
        var id = _nearestSlot.incomePointTrans
            ? _nearestSlot.incomePointTrans.GetComponentInChildren<IncomeDisplay>(true)
            : null;
        if (id) id.Init(Mathf.RoundToInt(income));

        // + если ты показываешь вес в карточке — обновим
        var ui = go.GetComponentInChildren<BrainrotParametrs>(true);
        if (ui) ui.SetWeight(kg);

        // 6) пометить слот занятым и сохранить
        BaseController.Instance.ConfirmSlotOccupied(_nearestFloor, _nearestIndex);
        SaveBridge.SnapshotAndSave(force: true);

        // 7) убрать подсказку
        HidePrompt();

        Debug.Log($"[PlacePetFromHand] Установлен {so.name} [{kg:0.##}kg], type={type}, income=${income:0.0}/s");
    }

    // ───────────────────────────── формула дохода ─────────────────────────────

    float EvaluateIncome(Brainrot so, StandardType type, float kg)
    {
        if (!so) return 0f;
        float baseAdd = 1.0f;
        float k = Mathf.Max(so.kPerKgBasic, 0.0001f);
        float typeMult = type switch
        {
            StandardType.Gold    => 1.25f,
            StandardType.Diamond => 1.5f,
            StandardType.Candy   => 4f,
            _                    => 1f,
        };
        float rarMult = so.rarity switch
        {
            CharacterRarity.Rare      => 2f,
            CharacterRarity.Epic      => 32f,
            CharacterRarity.Legendary => 64f,
            CharacterRarity.Mythic    => 220f,
            CharacterRarity.God       => 350f,
            CharacterRarity.Secret    => 500f,
            _                         => 1f,
        };
        return Mathf.Max(0.1f, (baseAdd + k * kg) * rarMult * typeMult);
    }
}
