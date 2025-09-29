// Assets/Scripts/Hotbar/HotbarEggEquip.cs
using UnityEngine;

/// <summary>
/// Хотбар 1..5: яйца → показываем в руке; брайроты → питомец рядом c игроком и поводок.
/// </summary>
public class HotbarEggEquip : MonoBehaviour
{
    [Header("Где появится яйцо (если это яйцо)")]
    public Transform holdPoint;
    public bool makeChild = true;

    [Header("Каталог яиц (чтобы получить префаб по типу)")]
    public TypeOfEgg typeCatalog;

    [Header("Поза в руке")]
    public Vector3 localPosition = Vector3.zero;
    public Vector3 localEuler    = Vector3.zero;
    public Vector3 localScale    = Vector3.one;

    // состояние яйца-визуала
    private GameObject[] _shown = new GameObject[5];
    public int CurrentSlot { get; private set; } = -1;
    public EggScriptableObject EquippedEggSO { get; private set; }
    public StandardType       EquippedEggType { get; private set; }
    public bool HasEquipped => CurrentSlot >= 0 && _shown[CurrentSlot] != null;

    // состояние питомца
    private int _currentBrainrotIndex = -1;

    private void Update()
    {
        int slot = -1;
        if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1)) slot = 0;
        else if (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2)) slot = 1;
        else if (Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.Keypad3)) slot = 2;
        else if (Input.GetKeyDown(KeyCode.Alpha4) || Input.GetKeyDown(KeyCode.Keypad4)) slot = 3;
        else if (Input.GetKeyDown(KeyCode.Alpha5) || Input.GetKeyDown(KeyCode.Keypad5)) slot = 4;

        if (slot >= 0) ToggleSlot(slot);

        // горячая клавиша «очистить всё»
        if (Input.GetKeyDown(KeyCode.Backspace))
        {
            if (_currentBrainrotIndex >= 0) { PetFollowerManager.Instance?.Despawn(); _currentBrainrotIndex = -1; }
            if (HasEquipped) HideEquipped();
        }
    }

    public void ToggleSlot(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= 5) return;

        if (EggInventory.Instance == null)
        {
            Debug.LogWarning("[Hotbar] Нет EggInventory.Instance");
            return;
        }
        var items = EggInventory.Instance.GetItemsSnapshot();
        if (slotIndex >= items.Count)
        {
            GameManager.Instance?.ErrorMessage("В этом слоте ничего нет");
            return;
        }

        var s = items[slotIndex];

        // ── БРАЙРОТ: питомец ─────────────────────────────────────────────
        if (s.IsBrainrot)
        {
            // если уже активен этот же — убрать питомца
            if (_currentBrainrotIndex == slotIndex)
            {
                PetFollowerManager.Instance?.Despawn();
                _currentBrainrotIndex = -1;
                return;
            }

            // переключение с яйца на питомца — прячем яйцо-визуал
            if (HasEquipped) HideEquipped();

            // если был другой питомец — убрать
            if (_currentBrainrotIndex >= 0)
                PetFollowerManager.Instance?.Despawn();

            // спавн питомца
            PetFollowerManager.Instance?.SpawnFromStack(s);
            _currentBrainrotIndex = slotIndex;
            return;
        }

        // ── ЯЙЦО: визуал в руке ─────────────────────────────────────────
        // если где-то активен питомец — убрать
        if (_currentBrainrotIndex >= 0)
        {
            PetFollowerManager.Instance?.Despawn();
            _currentBrainrotIndex = -1;
        }

        // если этот слот уже показан — спрятать
        if (_shown[slotIndex] != null)
        {
            HideSlot(slotIndex);
            CurrentSlot = -1;
            EquippedEggSO = null;
            EquippedEggType = StandardType.Standard;
            return;
        }

        // спавним визуал яйца
        if (!holdPoint) { Debug.LogWarning("[Hotbar] holdPoint не задан"); return; }
        if (!s.IsEgg) { Debug.LogWarning("[Hotbar] слот не яйцо"); return; }

        var tp = TypeOfEgg.GetParamsForType(s.egg, s.type);
        if (tp == null || !tp.characterPrefab) { Debug.LogWarning("[Hotbar] нет префаба для яйца"); return; }

        // спрячем другой активный слот
        if (CurrentSlot >= 0 && _shown[CurrentSlot] != null)
            HideSlot(CurrentSlot);

        GameObject go;
        if (makeChild)
        {
            go = Instantiate(tp.characterPrefab, holdPoint);
            go.transform.localPosition    = localPosition;
            go.transform.localEulerAngles = localEuler;
            go.transform.localScale       = localScale;
        }
        else
        {
            go = Instantiate(tp.characterPrefab);
            go.transform.position   = holdPoint.position;
            go.transform.rotation   = holdPoint.rotation;
            go.transform.localScale = localScale;
        }

        StripToViewOnly(go);

        _shown[slotIndex] = go;
        CurrentSlot       = slotIndex;
        EquippedEggSO     = s.egg;
        EquippedEggType   = s.type;
    }

    public void HideEquipped()
    {
        if (CurrentSlot >= 0) HideSlot(CurrentSlot);
        CurrentSlot = -1;
        EquippedEggSO = null;
        EquippedEggType = StandardType.Standard;
    }

    public void HideSlot(int slotIndex)
    {
        if (_shown[slotIndex] != null)
        {
            Destroy(_shown[slotIndex]);
            _shown[slotIndex] = null;
        }
    }

    // «чистая» модель яйца для руки (без логики/физики)
    private void StripToViewOnly(GameObject go)
    {
        foreach (var c in go.GetComponentsInChildren<EggController>(true))     Destroy(c);
        foreach (var c in go.GetComponentsInChildren<BrainrotMover>(true))     Destroy(c);
        foreach (var c in go.GetComponentsInChildren<Rotater>(true))           Destroy(c);
        foreach (var rb in go.GetComponentsInChildren<Rigidbody>(true))         Destroy(rb);
        foreach (var col in go.GetComponentsInChildren<Collider>(true))         Destroy(col);

        foreach (var anim in go.GetComponentsInChildren<Animator>(true)) anim.enabled = false;

        go.tag = "Untagged";
        go.layer = LayerMask.NameToLayer("Default");
    }
}
