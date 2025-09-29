using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Грид-инвентарь. Надёжно ждёт появления EggInventory.Instance и ребилдит UI.
/// </summary>
public class EggInventoryUI : MonoBehaviour
{
    [Header("Вёрстка")]
    [SerializeField] private Transform gridParent; // объект с GridLayoutGroup
    [SerializeField] private EggSlotUI slotPrefab; // префаб одного слота

    [Header("Бейджи типов")]
    public Sprite goldBadge;
    public Sprite diamondBadge;
    public Sprite candyBadge;

    [Header("Отрисовка")]
    [Tooltip("Показывать пустые слоты до MaxSlots (пустые ячейки без предмета).")]
    public bool showEmptySlots = false;

    private bool _subscribed;

    private void OnEnable()
    {
        StartCoroutine(WaitAndSubscribe());
    }

    private void OnDisable()
    {
        if (_subscribed && EggInventory.Instance != null)
            EggInventory.Instance.OnChanged -= Rebuild;

        _subscribed = false;
    }

    private IEnumerator WaitAndSubscribe()
    {
        // ждём, пока появится Singleton
        float t = 0f;
        while (EggInventory.Instance == null && t < 2f) // 2 сек на всякий
        {
            t += Time.unscaledDeltaTime;
            yield return null;
        }

        if (EggInventory.Instance != null && !_subscribed)
        {
            EggInventory.Instance.OnChanged += Rebuild;
            _subscribed = true;
        }

        Rebuild();
    }

    public void Rebuild()
    {
        if (!gridParent || !slotPrefab)
        {
            Debug.LogWarning("[EggInventoryUI] gridParent/slotPrefab не назначены");
            return;
        }

        // очистка
        for (int i = gridParent.childCount - 1; i >= 0; i--)
            Destroy(gridParent.GetChild(i).gameObject);

        if (EggInventory.Instance == null) return;

        List<EggStack> items = EggInventory.Instance.GetItemsSnapshot();
        int max = Mathf.Max(1, EggInventory.Instance.MaxSlots);

        int countToDraw = showEmptySlots ? max : Mathf.Min(items.Count, max);

        for (int i = 0; i < countToDraw; i++)
        {
            var ui = Instantiate(slotPrefab, gridParent);

            if (i < items.Count)
            {
                var s = items[i];
                ui.Bind(s, i, BadgeFor(s.type)); // обычный слот с предметом
            }
            else
            {
                // пустой слот (если включено showEmptySlots)
                ui.BindEmpty(i);
            }
        }
    }

    private Sprite BadgeFor(StandardType t)
    {
        switch (t)
        {
            case StandardType.Gold:    return goldBadge;
            case StandardType.Diamond: return diamondBadge;
            case StandardType.Candy:   return candyBadge;
            default:                   return null; // Standard — без бейджа
        }
    }
}
