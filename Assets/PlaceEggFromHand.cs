// Assets/Scripts/Inventory/PlaceEggFromHand.cs
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Если в руке есть яйцо, и рядом есть СВОБОДНЫЙ слот базы — показывает подсказку на центре слота.
/// По E ставит яйцо на этот слот.
/// </summary>
public class PlaceEggFromHand : MonoBehaviour
{
    [Header("Ссылки")]
    public HotbarEggEquip hotbar;                 // скрипт хотбара на игроке
    public GameObject     placePromptPrefab;      // UI-подсказка (без цены)
    public Collider       baseAreaTrigger;        // (опц.) общий триггер зоны базы

    [Header("Поиск слота")]
    public float   searchRadius     = 3.0f;       // радиус поиска свободного слота (если не задан trigger)
    public Vector3 slotPromptOffset = new Vector3(0, 0.25f, 0); // смещение подсказки над центром FreeSlot

    private GameObject _prompt;
    private Transform  _promptTargetSlot;         // к какому слоту привязана подсказка сейчас

    private void Reset()
    {
        if (!hotbar) hotbar = GetComponent<HotbarEggEquip>();
    }

    private void Update()
    {
        // ничего не экипировано — скрыть подсказку
        if (hotbar == null || !hotbar.HasEquipped)
        {
            HidePrompt();
            return;
        }

        // ищем ближайший свободный слот
        if (!TryFindNearestFreeSlot(out var floor, out var idx, out var slot))
        {
            HidePrompt();
            return;
        }

        // показать подсказку на центре FreeSlot
        ShowPromptAtSlot(slot.FreeSlot);

        // поставить по E
        if (Input.GetKeyDown(KeyCode.E))
        {
            PlaceEquippedEggToSlot(floor, idx, slot);
        }
    }

    // ──────────────────────────────── ПОДСКАЗКА ────────────────────────────────
    private void ShowPromptAtSlot(Transform freeSlot)
    {
        if (!placePromptPrefab || !freeSlot) return;

        if (_prompt == null)
            _prompt = Instantiate(placePromptPrefab);

        _promptTargetSlot = freeSlot;

        // позиция = центр слота + оффсет
        _prompt.transform.position = freeSlot.position + slotPromptOffset;

        // повернуть лицом к камере
        if (Camera.main)
            _prompt.transform.rotation = Quaternion.LookRotation(Camera.main.transform.forward);

        // если используешь общий скрипт PurchasePrompt — прячем цену и меняем текст
        var pp = _prompt.GetComponentInChildren<PurchasePrompt>();
        if (pp)
        {
            pp.SetName("Place Egg");
            pp.SetWalkingMode(0); // 0 — без цены; или сделай свой SetPlaceMode()
        }
    }

    private void HidePrompt()
    {
        if (_prompt) { Destroy(_prompt); _prompt = null; }
        _promptTargetSlot = null;
    }

    // ──────────────────────────────── ПОСТАНОВКА ────────────────────────────────
    private void PlaceEquippedEggToSlot(BaseController.SlotFloor floor, int idx, point slot)
    {
        var eggSO   = hotbar.EquippedEggSO;
        var eggType = hotbar.EquippedEggType;
        if (!eggSO) { GameManager.Instance?.ErrorMessage("Нечего ставить"); return; }

        var tp = TypeOfEgg.GetParamsForType(eggSO, eggType);
        if (tp == null || !tp.characterPrefab)
        {
            Debug.LogWarning($"[PlaceEgg] Нет префаба для {eggSO.name} ({eggType})");
            return;
        }

        // инстансим на слоте
        var go = Instantiate(tp.characterPrefab, slot.FreeSlot.position, slot.FreeSlot.rotation, slot.FreeSlot);
        var ec = go.GetComponent<EggController>() ?? go.AddComponent<EggController>();
        
        ec.Init(eggSO, eggType);
        ec.OnReachedPosition(slot);

        // помечаем слот занятым
        BaseController.Instance.ConfirmSlotOccupied(floor, idx);

        // убираем предмет из руки и подсказку
        hotbar.HideEquipped();
        HidePrompt();

        // списать 1 яйцо из рюкзака
        if (EggInventory.Instance) EggInventory.Instance.TryConsume(eggSO, eggType);

        // сохранение
        SaveBridge.SnapshotAndSave(force: true);
    }

    // ──────────────────────────────── ПОИСК СВОБОДНОГО СЛОТА ────────────────────────────────
    private bool TryFindNearestFreeSlot(out BaseController.SlotFloor floor, out int index, out point nearest)
    {
        // если задан общий триггер зоны базы — требуем, чтобы игрок был внутри
        if (baseAreaTrigger && !baseAreaTrigger.bounds.Contains(transform.position))
        {
            floor = 0; index = -1; nearest = null;
            return false;
        }

        float bestDist = float.MaxValue;
        BaseController.SlotFloor bestFloor = 0;
        int bestIndex = -1;
        point bestPoint = null;

        Vector3 pos = transform.position;
        var bc = BaseController.Instance;

        ScanListForFreeSlot(bc.availableSlots,  BaseController.SlotFloor.Floor1, pos, ref bestDist, ref bestFloor, ref bestIndex, ref bestPoint);
        ScanListForFreeSlot(bc.availableSlots2, BaseController.SlotFloor.Floor2, pos, ref bestDist, ref bestFloor, ref bestIndex, ref bestPoint);
        ScanListForFreeSlot(bc.availableSlots3, BaseController.SlotFloor.Floor3, pos, ref bestDist, ref bestFloor, ref bestIndex, ref bestPoint);

        floor = bestFloor; index = bestIndex; nearest = bestPoint;
        return nearest != null;
    }

    private void ScanListForFreeSlot(
        List<point> list, BaseController.SlotFloor f, Vector3 pos,
        ref float bestDist, ref BaseController.SlotFloor bestFloor, ref int bestIndex, ref point bestPoint)
    {
        if (list == null) return;

        for (int i = 0; i < list.Count; i++)
        {
            var p = list[i];
            if (p == null || p.FreeSlot == null) continue;
            if (p.isUsed || p.isReserved) continue;

            float d = Vector3.Distance(pos, p.FreeSlot.position);
            if (d <= searchRadius && d < bestDist)
            {
                bestDist = d;
                bestFloor = f;
                bestIndex = i;
                bestPoint = p;
            }
        }
    }
}
