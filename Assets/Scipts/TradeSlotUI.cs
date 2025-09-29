using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Заполняет слот иконкой и значением $/s.
/// </summary>
public class TradeSlotUI : MonoBehaviour
{
    public Image icon;
    public TextMeshProUGUI valueText; // "$123.2/s"

    public void Bind(TradeItem t)
    {
        if (icon)      { icon.sprite = t.icon; icon.enabled = t.icon != null; }
        if (valueText) valueText.text = $"${t.value:0.0}/s";
    }
}
