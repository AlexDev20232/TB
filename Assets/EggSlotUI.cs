// Assets/Scripts/Inventory/UI/EggSlotUI.cs
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Assets/Scripts/Inventory/UI/EggSlotUI.cs

public class EggSlotUI : MonoBehaviour
{
    public Image icon;
    public Image typeBadge;
    public TextMeshProUGUI countText;   // "xN" — только для яиц
    public TextMeshProUGUI weightText;  // "7.71kg" — только для брайротов
    public TextMeshProUGUI indexText;   // номер слота (если есть)

    public void Bind(EggStack s, int slotIndex, Sprite badgeForType)
    {
        if (indexText)  indexText.text = (slotIndex + 1).ToString();

        if (icon)
        {
            icon.sprite  = s.icon;
            icon.enabled = (icon.sprite != null);
        }

        if (typeBadge)
        {
            if (s.type == StandardType.Standard || badgeForType == null)
                typeBadge.enabled = false;
            else
            {
                typeBadge.enabled = true;
                typeBadge.sprite  = badgeForType;
            }
        }

        if (s.IsEgg)
        {
            if (countText)  { countText.gameObject.SetActive(true);  countText.text = "x" + s.count; }
            if (weightText) { weightText.gameObject.SetActive(false); }
        }
        else // брайрот
        {
            if (countText)  { countText.gameObject.SetActive(false); }
            if (weightText) { weightText.gameObject.SetActive(true);  weightText.text = $"{s.weightKg:0.##}kg"; }
        }
    }

    /// <summary>Пустой слот (для showEmptySlots=true).</summary>
    public void BindEmpty(int slotIndex)
    {
        if (indexText) indexText.text = (slotIndex + 1).ToString();

        if (icon)      { icon.sprite = null; icon.enabled = false; }
        if (typeBadge) { typeBadge.enabled = false; }
        if (countText) { countText.gameObject.SetActive(false); }
        if (weightText){ weightText.gameObject.SetActive(false); }
        // при желании тут можно включить какой-нибудь "плюсик"/плейсхолдер
    }
}

