using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BrainrotPreview : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI rarityText;
    [SerializeField] private TextMeshProUGUI typeText;
    [SerializeField] private Image           iconAnchor;

    // ---------- публичный API ----------
    public void Init(Brainrot data, Color rarityColor,
                     string typeNameEng, bool isUnlocked, StandardType groupType)
    {
        // иконка
        var iconGO = Instantiate(data.iconPrefab, iconAnchor.transform);
        iconGO.GetComponent<BrainrotIconSet>()?.Apply(groupType);

        if (!isUnlocked)
            iconGO.GetComponent<Image>().color = new Color(0, 0, 0, 0.9f);

        // подписи
        if (typeText)   typeText.text  = Cap(TypeRu(groupType));
        if (rarityText) ConfigureRarityRu(data.rarity, rarityColor);
    }

    // ---------- helpers ----------
    private static string TypeRu(StandardType t) => t switch
    {
        StandardType.Standard => "Нормальный",
        StandardType.Gold     => "Золотой",
        StandardType.Diamond  => "Aлмазный",
        StandardType.Candy    => "Конфетный",
        _                     => t.ToString()
    };

    private static string Cap(string s) =>
        string.IsNullOrEmpty(s) ? s
                                : char.ToUpperInvariant(s[0]) + s.Substring(1).ToLowerInvariant();

    private void ConfigureRarityRu(CharacterRarity r, Color color)
    {
        string ru = r switch
        {
            CharacterRarity.Common    => "Обычный",
            CharacterRarity.Rare      => "Редкий",
            CharacterRarity.Epic      => "Эпический",
            CharacterRarity.Legendary => "Легендарный",
            CharacterRarity.Mythic    => "Мифический",
            CharacterRarity.God       => "Божественный",
            CharacterRarity.Secret    => "Секретный",
            _                         => r.ToString()
        };

        switch (r)
        {
            case CharacterRarity.God:
                rarityText.text = $"<rainb>{Cap(ru)}</rainb>";
                break;
            case CharacterRarity.Secret:
                rarityText.text  = $"<incr>{Cap(ru)}</incr>";
                rarityText.color = Color.white;
                break;
            default:
                rarityText.text  = Cap(ru);
                rarityText.color = color;
                break;
        }
    }
}
