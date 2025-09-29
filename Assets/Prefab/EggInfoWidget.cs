// Assets/Scripts/Eggs/UI/EggInfoWidget.cs
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class EggInfoWidget : MonoBehaviour
{
    [Header("TMP-ссылки")]
    public TextMeshProUGUI rarityText;   // цвет по редкости
    public TextMeshProUGUI typeText;     // Basic/Gold/Diamond/Candy
    public TextMeshProUGUI nameText;     // имя яйца
    public TextMeshProUGUI priceText;    // "25$"

    [Header("Цвета текста типа (задаёшь в инспекторе)")]
    public Color typeColorStandard = Color.white;   // не используется (тип скрыт)
    public Color typeColorGold     = new Color(1f, 0.82f, 0.15f);
    public Color typeColorDiamond  = new Color(0.35f, 0.9f, 1f);
    public Color typeColorCandy    = new Color(1f, 0.5f, 0.9f);

    [Header("Инкубация (тот же Canvas)")]
    public GameObject incubationRoot;   // группа, где лежат slider + timeText
    public Slider     incubationSlider; // 0..1
    public TextMeshProUGUI timeLeftText;// "0m 17s Left" / "Ready!"

    /// <summary>Заполнить постоянные данные (редкость, имя, цена, тип).</summary>
    public void Bind(EggScriptableObject egg, StandardType eggType)
    {
        if (!egg) return;

        // Редкость (цвет из GameManager.rarityColors если задан)
        if (rarityText)
        {
            rarityText.text  = egg.rarity.ToString();
            rarityText.color = GetRarityColor(egg.rarity);
        }

        // Тип: Standard — скрыть; иначе — показать с нужным цветом
        if (typeText)
        {
            if (eggType == StandardType.Standard)
            {
                typeText.gameObject.SetActive(false);
            }
            else
            {
                typeText.gameObject.SetActive(true);
                typeText.text  = eggType.ToString();
                typeText.color = GetTypeColor(eggType);
            }
        }

        if (nameText)  nameText.text  = egg.EggName;
        if (priceText) priceText.text = $"{egg.price}$";
    }

    /// <summary>Переключить видимость цены (при инкубации цену убираем).</summary>
    public void SetPriceVisible(bool visible)
    {
        if (priceText) priceText.gameObject.SetActive(visible);
    }

    /// <summary>Включить/выключить секцию инкубации.</summary>
    public void SetIncubationVisible(bool visible)
    {
        if (incubationRoot) incubationRoot.SetActive(visible);
    }

    /// <summary>Обновить прогресс и текст времени.</summary>
    public void UpdateIncubationUI(float totalSec, float remainingSec)
    {
        if (!incubationRoot) return;

        remainingSec = Mathf.Max(0f, remainingSec);
        float t = (totalSec <= 0.0001f) ? 1f : Mathf.Clamp01(1f - (remainingSec / totalSec));

        if (incubationSlider) incubationSlider.value = t;

        if (timeLeftText)
        {
            if (remainingSec <= 0.001f)
            {
                timeLeftText.text = "Ready!";
            }
            else
            {
                int m = Mathf.FloorToInt(remainingSec / 60f);
                int s = Mathf.FloorToInt(remainingSec % 60f);
                timeLeftText.text = $"{m}m {s}s Left";
            }
        }
    }

    // ——— helpers ———
    private Color GetRarityColor(CharacterRarity r)
    {
        var gm = GameManager.Instance;
        if (gm != null && gm.rarityColors != null)
            foreach (var rc in gm.rarityColors)
                if (rc.rarity == r) return rc.color;
        return Color.white;
    }

    private Color GetTypeColor(StandardType t)
    {
        switch (t)
        {
            case StandardType.Gold:    return typeColorGold;
            case StandardType.Diamond: return typeColorDiamond;
            case StandardType.Candy:   return typeColorCandy;
            default:                   return typeColorStandard;
        }
    }
}
