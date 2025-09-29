// Assets/Scripts/Rebirth/RebirthManager.cs
using UnityEngine;

public class RebirthManager : MonoBehaviour
{
    [Header("All stages, in order")]
    public RebirthStage[] stages;

    [Header("Runtime hooks")]
    public RebirthPanel ui;

    public static RebirthManager Instance { get; private set; }

    [Header("DEV")]
    [Tooltip("Если включено — можно делать ребёрт без выполнения условий.")]
    public bool devBypassRequirements = false;

    void Awake()
    {
        if (Instance && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    public RebirthStage CurrentStage =>
        stages != null && stages.Length > SaveBridge.Saves.currentRebirthStage
            ? stages[SaveBridge.Saves.currentRebirthStage]
            : null;

    public bool AllStagesComplete => CurrentStage == null;

    public bool RequirementsMet()
    {
        var st = CurrentStage;
        if (st == null) return false;

        if (GameManager.Instance.Money < st.requiredCoins) return false;

       foreach (var br in st.requiredBrainrots)
{
    if (br == null) continue;

    string targetName = br.characterName;

    // проверяем, есть ли хотя бы один brainrot на базе с таким же именем
    bool has = BaseController.Instance.HasBrainrotByName(targetName);

    if (!has) return false;
}

        return true;
    }

  public void DoRebirth()
{
    var st = CurrentStage;

    if (!devBypassRequirements)
    {
        if (!RequirementsMet()) return;
        if (st == null) return;
    }
    else
    {
        if (st == null) { Debug.LogWarning("DEV: Нет текущего этапа для ребёрта."); return; }
    }

    // 1) множитель
    SaveBridge.Saves.rebirthIncomeMultiplier += st.incomeMultiplierPlus;
    MoneyBoostManager.Instance?.RefreshPermanent();

    // 2) сброс базы
    BaseController.Instance.ResetBase();

    // 3) деньги
    GameManager.Instance.SetMoney(0);
    GameManager.Instance.AddMoney(st.grantCoins);
    

    // 4) слоты этажей

        // ── 2-й этаж: если впервые — гарантируем минимум 1 слот
        int add2 = st.addSlotsFloor2;
    if (!SaveBridge.Saves.secondFloorOpened && SaveBridge.Saves.secondFloorSlotsUnlocked == 0)
        add2 = Mathf.Max(1, add2);
    if (add2 > 0)
        BaseController.Instance.UnlockFloorSlots(2, add2);

    // ── 3-й этаж: открываем только если этап действительно даёт слоты на 3F
    if (st.addSlotsFloor3 > 0)
        BaseController.Instance.UnlockFloorSlots(3, st.addSlotsFloor3);

    // 5) следующий этап + сейв
    SaveBridge.Saves.currentRebirthStage++;
    SaveBridge.SnapshotAndSave(force: true);

    // 6) UI
    ui?.Refresh();
}
}
