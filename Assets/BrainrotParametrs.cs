// Assets/Scripts/UI/BrainrotParametrs.cs
using TMPro;
using UnityEngine;

/// <summary>
/// Отображает параметры Brainrot в карточке:
/// • Форматирует число_price / income (K, M)           <br>
/// • Красит надписи в зависимости от редкости          <br>
/// • Переводит названия редкостей и типов на русский   <br>
/// </summary>
public class BrainrotParametrs : MonoBehaviour
{
    // ---------------- ссылки на текстовые поля ------------------------------------
    [Header("Ссылки на TextMeshPro")]   
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI rarityText;
    [SerializeField] private TextMeshProUGUI incomeText;
    [SerializeField] private TextMeshProUGUI priceText;
    [SerializeField] private TextMeshProUGUI typeText;

    float _kgCached = -1f;

    // ---------------- цвета и русские названия для Gold / Diamond / Candy ---------
    [Header("Цвета типов (заполните в инспекторе)")]
    public TypeColor[] typeColors =
    {
        new TypeColor { group = StandardType.Gold,    color = new Color32(255,230, 95,255), ruName="Золотой"   },
        new TypeColor { group = StandardType.Diamond, color = new Color32(140,220,255,255), ruName="Алмазный" },
        new TypeColor { group = StandardType.Candy,   color = new Color32(255,120,200,255), ruName="Конфетный"}
    };

    [System.Serializable]
    public struct TypeColor
    {
        public StandardType group;
        public Color  color;
        public string ruName;   // подпись в UI
    }

    // ==============================================================================
    //                                   PUBLIC API
    // ==============================================================================
    /// <summary>
    /// Полная инициализация карточки.
    /// </summary>
    public void Init(string name,
                     CharacterRarity rarity,
                     float incomePerSec,
                     int   price,
                     StandardType group = StandardType.Standard)
    {
        // Имя персонажа
        nameText.text = name;

        // --- Редкость (на русском) -------------------------------------------------
        string ru = RarityToRussian(rarity);
        rarityText.text = rarity == CharacterRarity.God
                          ? $"<rainb>{ru}</rainb>"   // радужный GOD
                          : ru;

        // --- Доход и цена (формат K / M) ------------------------------------------
        incomeText.text = "+" + FormatNumber(incomePerSec) + "/с";
        priceText.text  = FormatNumber(price, addDollar: true);

        // --- Тип персонажа (Gold / Diamond / Candy / Standard) --------------------
        ApplyType(group);
    }

    /// <summary>Из GameManager для покраски текста редкости.</summary>
    public void SetRarityColor(Color c) => rarityText.color = c;

    // ==============================================================================
    //                                PRIVATE HELPERS
    // ==============================================================================
    /// <summary>Возвращает русскую строку для редкости.</summary>
    private static string RarityToRussian(CharacterRarity r) => r switch
    {
        CharacterRarity.Common    => "Обычный",
        CharacterRarity.Rare      => "Редкий",
        CharacterRarity.Epic      => "Эпический",
        CharacterRarity.Legendary => "Легендарный",
        CharacterRarity.Mythic    => "Мифический",
        CharacterRarity.God       => "Бог Брайнротов",
        CharacterRarity.Secret    => "Секретный",
        _                         => r.ToString()   // fallback
    };

    /// <summary>Показывает/скрывает и окрашивает поле «Тип».</summary>
    private void ApplyType(StandardType group)
    {
        if (group == StandardType.Standard)
        {
            typeText.gameObject.SetActive(false);  // стандартный тип скрываем
            return;
        }

        foreach (var tc in typeColors)
            if (tc.group == group)
            {
                typeText.gameObject.SetActive(true);
                typeText.text  = tc.ruName;
                typeText.color = tc.color;
                return;
            }

        // если цвет не задан – показываем имя enum'а белым
        typeText.gameObject.SetActive(true);
        typeText.text  = group.ToString();
        typeText.color = Color.white;
    }
   public void SetIncomePerSec(int perSec)
    {
        if (incomeText) incomeText.text = $"${perSec}/s";
    }
      public void SetWeight(float kg)
    {
        _kgCached = kg;
        if (nameText)
        {
            // если имя уже содержит скобки — можно заменить только часть в скобках
            // простой вариант: добавить в конец
            // здесь лучше использовать исходное имя из Init(...)
            string plain = nameText.text;
            int br = plain.IndexOf('[');
            if (br > 0) plain = plain.Substring(0, br).Trim(); // отрезать старую вставку в скобках
            nameText.text = $"{plain} [{kg:0.##}kg]";
        }
    }
    /// <summary>Форматирование чисел: 1 000 → 1 K, 1 000 000 → 1 M.</summary>
    private static string FormatNumber(float v, bool addDollar = false)
    {
        string suffix = addDollar ? "$" : "";
        if (v >= 1_000_000f) return (v / 1_000_000f).ToString("0.#") + " M" + suffix;
        if (v >= 1_000f) return (v / 1_000f).ToString("0.#") + " K" + suffix;
        return Mathf.RoundToInt(v) + suffix;
    }
}
