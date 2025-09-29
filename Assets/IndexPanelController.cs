// Assets/Scripts/UI/IndexPanelController.cs
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class IndexPanelController : MonoBehaviour
{
    [Header("Данные")]
    [SerializeField] private TypeOfEgg brainrotTypeSet;   // ← теперь каталог ЯИЦ

    [Header("UI")]
    [SerializeField] private Transform gridParent;          // Контейнер с GridLayoutGroup
    [SerializeField] private BrainrotPreview previewPrefab; // Префаб карточки
    [SerializeField] private Button normalBtn;
    [SerializeField] private Button goldBtn;
    [SerializeField] private Button diamondBtn;
    [SerializeField] private Button candyBtn;

    void Awake()
    {
        if (normalBtn)  normalBtn.onClick.AddListener(() => ShowCategory(Category.Normal));
        if (goldBtn)    goldBtn.onClick.AddListener(() => ShowCategory(Category.Gold));
        if (diamondBtn) diamondBtn.onClick.AddListener(() => ShowCategory(Category.Diamond));
        if (candyBtn)   candyBtn.onClick.AddListener(() => ShowCategory(Category.Candy));
    }

    void OnEnable()
    {
        ShowCategory(Category.Normal);
    }

    enum Category { Normal, Gold, Diamond, Candy }

    void ShowCategory(Category cat)
    {
        ClearGrid();

        // 1) Маппим категорию в StandardType (0..3)
        StandardType gType = cat switch
        {
            Category.Normal  => StandardType.Standard,
            Category.Gold    => StandardType.Gold,
            Category.Diamond => StandardType.Diamond,
            Category.Candy   => StandardType.Candy,
            _                => StandardType.Standard
        };

        // 2) Берём ВЕСЬ список брайротов для выбранного типа через новый метод
        Brainrot[] src = brainrotTypeSet
            ? brainrotTypeSet.GetBrainrotsByType(gType)
            : System.Array.Empty<Brainrot>();
        if (src == null || src.Length == 0) return;

        // 3) Порядок редкостей (как раньше)
        var order = new Dictionary<CharacterRarity, int>();
        var rw = GameManager.Instance ? GameManager.Instance.RarityWeights : null;
        if (rw != null)
            for (int i = 0; i < rw.Length; i++) order[rw[i].rarity] = i;

        // 4) Сортируем по редкости
        var sorted = src.Where(b => b != null)
                        .OrderBy(b => order != null && order.TryGetValue(b.rarity, out int v) ? v : int.MaxValue);

        string typeName = cat.ToString();

        // 5) Рендерим карточки
        foreach (var br in sorted)
        {
            bool unlocked = BrainrotUnlocks.IsUnlocked(br);
            var item      = Instantiate(previewPrefab, gridParent);

            // Подбираем цвет редкости
            Color col = Color.white;
            var gm = GameManager.Instance;
            if (gm != null && gm.rarityColors != null)
            {
                var found = gm.rarityColors.FirstOrDefault(c => c.rarity == br.rarity);
                col = found.color;
            }

            // Передаём groupType, как и раньше
            item.Init(br, col, typeName, unlocked, gType);
        }
    }

    void ClearGrid()
    {
        for (int i = gridParent.childCount - 1; i >= 0; i--)
            Destroy(gridParent.GetChild(i).gameObject);
    }
}
