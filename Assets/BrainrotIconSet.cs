// Assets/Scripts/UI/BrainrotIconSet.cs
using UnityEngine;
using UnityEngine.UI;

public class BrainrotIconSet : MonoBehaviour
{
    [Header("Спрайты по порядку: Standard, Gold, Diamond, Candy")]
    public Sprite[] skins = new Sprite[4];

    [Header("Куда ставить спрайт (Image)")]
    [SerializeField] private Image targetImage;

    public void Apply(StandardType type)
    {
        int id = (int)type;                     // Standard = 0 ...
        if (id < 0 || id >= skins.Length || !skins[id])
        {
            Debug.LogWarning($"Skin {type} не задан у {name}");
            return;
        }
        targetImage.sprite = skins[id];
    }
}
