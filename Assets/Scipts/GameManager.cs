using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;
using YG;

[System.Serializable]
public class RarityColor
{
    public CharacterRarity rarity;
    public Color color;
}

public enum BrainrotGroup { Normal, Gold, Diamond, Candy }

[System.Serializable]
public struct GroupWeight
{
    public BrainrotGroup group;
    [Range(0, 100)] public int percent;
}

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [SerializeField] private TypeOfEgg typeSet;   // Каталог яиц (SO с allEgs)

    [SerializeField]
    private GroupWeight[] groupWeights =
    {
        new GroupWeight{ group = BrainrotGroup.Normal,  percent = 75 },
        new GroupWeight{ group = BrainrotGroup.Gold,    percent = 20 },
        new GroupWeight{ group = BrainrotGroup.Diamond, percent = 5  },
        new GroupWeight{ group = BrainrotGroup.Candy,   percent = 5  }
    };

    [SerializeField] private GameObject uiCanvasPrefab;
    [SerializeField] private float extraYOffset = 0f;
    [SerializeField] private TextMeshProUGUI moneyText;
    [SerializeField] private TextMeshProUGUI legendaryTimerText;
    [SerializeField] private TextMeshProUGUI mythicTimerText;
    [SerializeField] private EggScriptableObject firstEgg;
    public long Money { get; private set; } = 100;
    [SerializeField] private float initialDelay = 2f;
    [SerializeField] private float spawnInterval = 3f;
    [SerializeField] private TextMeshProUGUI errorText;
    public TypeOfEgg TypeSet => typeSet;

    private bool firstSpawnDone;

    [System.Serializable]
    public struct RarityWeight
    {
        public CharacterRarity rarity;
        public int weight;
    }

    

    [SerializeField]
    private RarityWeight[] rarityWeights =
    {
        new RarityWeight{rarity = CharacterRarity.Common,    weight = 50},
        new RarityWeight{rarity = CharacterRarity.Rare,      weight = 30},
        new RarityWeight{rarity = CharacterRarity.Epic,      weight = 20},
        new RarityWeight{rarity = CharacterRarity.Legendary, weight = 10},
        new RarityWeight{rarity = CharacterRarity.Mythic,    weight = 5},
        new RarityWeight{rarity = CharacterRarity.God,       weight = 3},
        new RarityWeight{rarity = CharacterRarity.Secret,    weight = 1},
    };

    public RarityWeight[] RarityWeights => rarityWeights;
    public RarityColor[] rarityColors;

    private const string AnchorName = "CanvasActor";

    private float legendaryTimer = 300f;
    private float mythicTimer = 600f;
    private bool pendingLegendary = false;
    private bool pendingMythic = false;

    public GameObject UICanvasPrefab => uiCanvasPrefab;
    public float ExtraYOffset => extraYOffset;

    [Header("Forced spawn")]
    [SerializeField] private EggScriptableObject forcedEgg;
    [SerializeField] [Min(1)] private int minForcedGap = 3;
    [SerializeField] [Min(1)] private int maxForcedGap = 7;

    private int _forcedGapCounter;


    

    private void Awake()
    {
        if (Instance && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        SaveBridge.Init();
    }
    
    public long GetMoney() => Money;

    public void ErrorMessage(string msg)
    {
        if (errorText == null) return;          // страховка

        errorText.text = msg;
        errorText.gameObject.SetActive(true);   // включает текст/панель

        // если у тебя на родителе висит анимация — перезапускаем
        var pop = errorText.GetComponentInParent<ErrorMessagePopup>();
        pop?.ShowAgain();

        SoundsManager.Instance?.PlayErrorSound();
    }

    private void Start()
    {
        // Загружаем деньги
        Money = YG2.saves.coins <= 0 ? 100L : YG2.saves.coins;
        ApplyMoneyUI();

        SaveBridge.LoadSlots();

        // Первое яйцо
        if (firstEgg != null)
        {
            SpawnEggSpecific(firstEgg, StandardType.Standard);
            firstSpawnDone = true;
        }

        float firstDelay = firstEgg ? initialDelay + spawnInterval - 1 : initialDelay;
        ResetForcedGap();

        InvokeRepeating(nameof(SpawnRandomCharacter), firstDelay, spawnInterval);
        InvokeRepeating(nameof(UpdateTimers), 0f, 1f);
        UpdateTimerTexts();
    }


private bool _wipeInProgress = false;

    private void Update()
    {
        // сочетание: K + B (порядок не важен)
        bool kbCombo =
            (Input.GetKey(KeyCode.K) && Input.GetKeyDown(KeyCode.B)) ||
            (Input.GetKey(KeyCode.B) && Input.GetKeyDown(KeyCode.K));

        if (!_wipeInProgress && kbCombo)
        {
            StartCoroutine(DoFullWipe());
        }
    }

private System.Collections.IEnumerator DoFullWipe()
{
    _wipeInProgress = true;
    Debug.Log("[Wipe] Полный сброс прогресса…");

    // 1) Остановить спавны и внутренние таймеры
    CancelInvoke();                           // останавливаем InvokeRepeating
    StopAllCoroutines();                      // останавливаем корутины GameManager'а
    // (таймеры легендарки/мифика больше не тикают — их перезапустит перезагрузка сцены)

    // 2) Визуально очистить базу (убрать всех ботов/яйца/UI), выключить этажи
    BaseController.Instance?.HardVisualWipe();

    // 3) Обнулить деньги локально (без немедленного сохранения)
    SetMoney(0, save: false);

    // 4) Очистить инвентарь яиц (если есть)
    if (EggInventory.Instance != null)
        EggInventory.Instance.ClearAll();

    // 5) Сбросить все сохранения (локально + облако через YG2)
    SaveBridge.ResetAllDataToDefaults();

    // 6) Немного подождать, чтобы всё удалилось корректно
    yield return null;

    // 7) Перезагрузить текущую сцену начисто
    var scene = SceneManager.GetActiveScene().buildIndex;
    SceneManager.LoadScene(scene);

    // (после загрузки Awake/Start снова выставят стейты и перезапустят InvokeRepeating)
}

    private void ResetForcedGap() =>
        _forcedGapCounter = Random.Range(minForcedGap, maxForcedGap + 1);

    public static string FormatMoney(long value)
    {
        const long K = 1_000, M = 1_000_000, B = 1_000_000_000, T = 1_000_000_000_000;
        if (value >= 999 * T) return (value / T).ToString("N0") + "T";
        if (value >= T) return (value / (double)T).ToString("0.00") + "T";
        if (value >= B) return (value / (double)B).ToString("0.00") + "B";
        if (value >= M) return (value / (double)M).ToString("0.00") + "M";
        if (value >= K) return (value / (double)K).ToString("0.00") + "K";
        return value.ToString("N0");
    }

    // ─────────── СПАВН ЯИЦ ───────────
    private void SpawnEggSpecific(EggScriptableObject egg, StandardType type)
    {
        if (!egg) return;
        var tp = TypeOfEgg.GetParamsForType(egg, type);
        if (tp == null || !tp.characterPrefab) return;

        Instantiate(tp.characterPrefab,
                    tp.characterPrefab.transform.position,
                    tp.characterPrefab.transform.rotation);
    }

    private void SpawnRandomCharacter()
    {
        if (pendingLegendary) { SpawnEggByRarity(CharacterRarity.Legendary); pendingLegendary = false; return; }
        if (pendingMythic) { SpawnEggByRarity(CharacterRarity.Mythic); pendingMythic = false; return; }

        _forcedGapCounter--;
        if (_forcedGapCounter <= 0 && forcedEgg != null)
        {
            SpawnEggSpecific(forcedEgg, StandardType.Standard);
            ResetForcedGap();
            return;
        }

        BrainrotGroup grp = PickGroup();
        CharacterRarity rarity = PickWeightedRarity();
        SpawnEggFromGroupAndRarity(grp, rarity);
    }

    private void SpawnEggFromGroupAndRarity(BrainrotGroup group, CharacterRarity rarity)
    {
        EggScriptableObject egg = typeSet.GetRandomEggByRarity(rarity);
        if (!egg) return;

        StandardType gType = group switch
        {
            BrainrotGroup.Normal => StandardType.Standard,
            BrainrotGroup.Gold => StandardType.Gold,
            BrainrotGroup.Diamond => StandardType.Diamond,
            BrainrotGroup.Candy => StandardType.Candy,
            _ => StandardType.Standard
        };

        SpawnEggSpecific(egg, gType);
    }

    private void SpawnEggByRarity(CharacterRarity rarity)
    {
        EggScriptableObject egg = typeSet.GetRandomEggByRarity(rarity);
        if (!egg) return;
        SpawnEggSpecific(egg, StandardType.Standard);
    }

    // ─────────── ВЕСА ───────────
    private BrainrotGroup PickGroup()
    {
        int sum = 0;
        foreach (var gw in groupWeights) sum += Mathf.Max(0, gw.percent);
        if (sum <= 0) return BrainrotGroup.Normal;

        int roll = Random.Range(0, sum), acc = 0;
        foreach (var gw in groupWeights)
        {
            acc += Mathf.Max(0, gw.percent);
            if (roll < acc) return gw.group;
        }
        return BrainrotGroup.Normal;
    }

    private CharacterRarity PickWeightedRarity()
    {
        int total = 0;
        foreach (var rw in rarityWeights) if (rw.weight > 0) total += rw.weight;
        if (total <= 0) return rarityWeights[0].rarity;

        int roll = Random.Range(0, total), acc = 0;
        foreach (var rw in rarityWeights)
        {
            if (rw.weight <= 0) continue;
            acc += rw.weight;
            if (roll < acc) return rw.rarity;
        }
        return rarityWeights[0].rarity;
    }

    // ─────────── ДЕНЬГИ ───────────
    public void SetMoney(long value, bool save = true)
    {
        Money = value;
        ApplyMoneyUI(save);
    }

    void ApplyMoneyUI(bool save = true)
    {
        if (moneyText) moneyText.text = "$" + MoneyFormatter.Short(Money);
        YG2.saves.coins = Money;
        if (save) YG2.SaveProgress();
        OnMoneyChange?.Invoke(Money);
    }

    public void AddMoney(int amount) { Money += amount; ApplyMoneyUI(); }
    public bool TrySpendMoney(long amount)
    {
        if (Money < amount) return false;
        Money -= amount; ApplyMoneyUI();
        return true;
    }

    // ─────────── ТАЙМЕРЫ ───────────
    public void UpdateTimers() { StartCoroutine(UpdateTimersCur()); }

    System.Collections.IEnumerator UpdateTimersCur()
    {
        legendaryTimer -= 1f;
        mythicTimer -= 1f;

        if (legendaryTimer <= 0f) { pendingLegendary = true; legendaryTimer = 300f; }
        if (mythicTimer <= 0f) { pendingMythic = true; mythicTimer = 600f; }

        UpdateTimerTexts();
        yield return null;
    }

    private void UpdateTimerTexts()
    {
        legendaryTimerText.text = pendingLegendary
            ? "<color=yellow>Legendary: READY</color>"
            : $"Гарантия <color=yellow>Легендарного</color> через {FormatTime(legendaryTimer)}";

        mythicTimerText.text = pendingMythic
            ? "<color=red>Mythic: READY</color>"
            : $"Гарантия <color=red>Мифического</color> через {FormatTime(mythicTimer)}";
    }

    private string FormatTime(float seconds)
    {
        int minutes = Mathf.FloorToInt(seconds / 60f);
        int secs = Mathf.FloorToInt(seconds % 60f);
        return $"{minutes:00}:{secs:00}";
    }

    private Transform FindChildRecursive(Transform parent, string targetName)
    {
        foreach (Transform child in parent)
        {
            if (child.name == targetName) return child;
            var deeper = FindChildRecursive(child, targetName);
            if (deeper != null) return deeper;
        }
        return null;
    }

    public event System.Action<long> OnMoneyChange;
}
