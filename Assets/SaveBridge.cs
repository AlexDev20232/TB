using System.Collections.Generic;
using UnityEngine;
using YG;

/// <summary>
/// Единая точка сохранений (локально + облако) и работа со слотами.
/// Делает периодические снимки занятых слотов, хранит доходы, сейвит на паузу/выход.
/// </summary>
public static class SaveBridge
{
    private const string PP_KEY = "LocalSaveJson";

    // Данные плагина YG2 (v2)
    public static SavesYG Saves => YG2.saves;

    // защита от двойного восстановления слотов
    private static bool _slotsRestoredOnce;

    // анти‑спам сохранений
    private static float _lastSaveTime;
    private const float MIN_SAVE_INTERVAL = 5f;

    // флаг «данные менялись», чтобы не писать сейв каждую секунду
    private static bool _dirty;

    

    /// <summary>Вызывать один раз в Awake (например, в GameManager.Awake()).</summary>
    public static void Init()
    {
        // Поднимем локальный кэш в оперативные данные
        if (PlayerPrefs.HasKey(PP_KEY))
            JsonUtility.FromJsonOverwrite(PlayerPrefs.GetString(PP_KEY), Saves);

        // Когда SDK отдаст данные из облака — синхронизируем локальный кэш и восстановим слоты
        YG2.onGetSDKData += () =>
        {
            PersistLocal();
            LoadSlots(); // безопасно — повторный вызов ничего не сделает
        };

        // Поднимем авто‑сейвер, если его ещё нет
        if (Object.FindObjectOfType<SaveBridgeHook>() == null)
        {
            var go = new GameObject("[SaveBridge]");
            Object.DontDestroyOnLoad(go);
            go.AddComponent<SaveBridgeHook>();
        }
    }

    /// <summary>
    /// Восстановить Brainrot’ов из сохранения на занятые слоты.
    /// Вызывать в Start() после появления BaseController.
    /// </summary>
    public static void LoadSlots()
    {
        if (_slotsRestoredOnce) return;

        if (BaseController.Instance == null)
        {
            Debug.LogWarning("[SaveBridge] LoadSlots() вызван до инициализации BaseController.");
            return;
        }

        BaseController.Instance.RestoreFromSave(Saves.placed);
        _slotsRestoredOnce = true;
    }

    /// <summary>Пометить, что данные изменились (например, вырос доход).</summary>
    public static void MarkDirty() => _dirty = true;

    /// <summary>Сделать снимок слотов из сцены в Saves.placed.</summary>
   public static void SnapshotSlotsFromScene()
{
    if (Saves.placed == null) Saves.placed = new List<SlotSave>();
    Saves.placed.Clear();

    void Dump(List<point> list, int floor)
    {
        if (list == null) return;

        for (int i = 0; i < list.Count; i++)
        {
            var p = list[i];
            if (p == null || !p.isUsed || p.FreeSlot == null) continue;

            // 1) Брайрот на слоте
           var br = p.FreeSlot.GetComponentInChildren<BrainrotController>(true);
if (br != null)
{
    // пропуск превью:
    if (!br.enabled) continue;
    var so = br.GetStats();
    if (so == null) continue;

    var id = p.incomePointTrans ? p.incomePointTrans.GetComponentInChildren<IncomeDisplay>(true) : null;

    // ⬇️ попробуем взять вес из BrainrotEconomy (если есть)
    var econ = p.FreeSlot.GetComponentInChildren<BrainrotEconomy>(true);
    float w = econ ? econ.kg : 0f;

    Saves.placed.Add(new SlotSave {
        floor        = floor,
        slotIndex    = i,
        brainrotKey  = BrainrotUnlocks.InternalKey(so),
        storedIncome = id ? id.CurrentIncome : 0,
        isEgg        = false,
        eggKey       = null,
        eggType      = 0,
        weightKg     = w                         // ⬅️ сохраняем kg
    });
    continue;
}

            // 2) Яйцо на слоте
            var eg = p.FreeSlot.GetComponentInChildren<EggController>(true);
            if (eg != null)
            {
                Saves.placed.Add(new SlotSave {
                    floor        = floor,
                    slotIndex    = i,
                    isEgg        = true,
                    eggKey       = eg.egg ? eg.egg.name : "",
                    eggType      = (int)eg.eggType,
                    brainrotKey  = null,
                    storedIncome = 0
                });
            }
        }
    }

    Dump(BaseController.Instance.availableSlots,  0);
    Dump(BaseController.Instance.availableSlots2, 1);
    Dump(BaseController.Instance.availableSlots3, 2);

    Save(); // локально + облако
}

    /// <summary>Сделать снимок слотов и сохранить (локально + облако).</summary>
    public static void SnapshotAndSave(bool force = false)
    {
       SnapshotSlotsFromScene();
    Save();
    }

    /// <summary>Сохранить без пересъёма слотов.</summary>
    public static void Save()
    {
        PersistLocal();
        if (YG2.isSDKEnabled)
            YG2.SaveProgress();
    }

    /// <summary>Установить монеты и сразу сохранить.</summary>
    public static void SetCoins(int value, bool saveNow = true)
    {
        Saves.coins = value;
        if (saveNow) Save();
    }


      public static void ResetAllDataToDefaults()
    {
        // Переписываем весь SavesYG в дефолт (сохраняем idSave внутри YG2 сам решит)
        YG2.SetDefaultSaves();

        // Если хочешь гарантированно «ноль» — явно задаём
        YG2.saves.coins = 0;
        YG2.saves.currentRebirthStage = 0;
        YG2.saves.rebirthIncomeMultiplier = 0f;

        YG2.saves.unlocked.Clear();
        YG2.saves.placed.Clear();
YG2.saves.thirdFloorOpened = false;
YG2.saves.thirdFloorSlotsUnlocked = 0;

        YG2.saves.secondFloorOpened = false;
        YG2.saves.secondFloorSlotsUnlocked = 0;

        // Другие твои флаги магазинов/рекламы/паков, если надо:
        // YG2.saves.vipOwned = false; и т.д.

        YG2.SaveProgress();
    }

    private static void PersistLocal()
    {
        string json = JsonUtility.ToJson(Saves);
        PlayerPrefs.SetString(PP_KEY, json);
        PlayerPrefs.Save();
    }

    // ─────────────────────────── вспомогательный компонент ───────────────────────────

    /// <summary>
    /// Авто‑сейвер: раз в N секунд сохраняет изменения (если были),
    /// и делает форс‑сейв при паузе/выходе приложения.
    /// </summary>
    public class SaveBridgeHook : MonoBehaviour
    {
        [SerializeField] private float autosaveInterval = 5f;
        private float _nextTick;

        private void Awake() => DontDestroyOnLoad(gameObject);

        private void Update()
        {
            if (Time.unscaledTime >= _nextTick)
            {
                _nextTick = Time.unscaledTime + autosaveInterval;
                if (_dirty) SnapshotAndSave(); // снимок и сейв только если были изменения
            }
        }

        private void OnApplicationPause(bool pause)
        {
            if (pause) SnapshotAndSave(force: true);
        }

        private void OnApplicationQuit()
        {
            SnapshotAndSave(force: true);
        }
    }
}
