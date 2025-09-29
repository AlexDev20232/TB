// Assets/Scripts/Inventory/EggInventory.cs
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Общий инвентарь: умеет стекать яйца и добавлять брайротов отдельными слотами.
/// </summary>
public class EggInventory : MonoBehaviour
{
    public static EggInventory Instance { get; private set; }

    [Header("Параметры")]
    [Min(1)] public int MaxSlots = 10;

    [SerializeField] private List<EggStack> items = new();

    /// <summary>Сигнал интерфейсу/логике: состав изменился (яйца/брайроты).</summary>
    public System.Action OnChanged;

    /// <summary>Срабатывает один раз при переходе из состояния «нет брайротов» → «появился хотя бы один».</summary>
    public static System.Action OnFirstBrainrotGained;

    private void Awake()
    {
        if (Instance && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    // --- иконки для UI ---
    private Sprite ResolveIcon(EggScriptableObject egg, Brainrot br, StandardType type = StandardType.Standard)
{
    // --- яйца ---
    if (egg && egg.icon) 
        return egg.icon;

    // --- брайроты ---
    if (br && br.icon) 
        return br.icon;

    var prefab = br ? br.iconPrefab : null;
    if (prefab)
    {
        var set = prefab.GetComponent<BrainrotIconSet>();
        if (set && set.skins != null && set.skins.Length > 0)
        {
            // Выбираем по типу
            int idx = 0;
            switch (type)
            {
                case StandardType.Gold:    idx = 1; break;
                case StandardType.Diamond: idx = 2; break;
                case StandardType.Candy:   idx = 3; break;
                default:                   idx = 0; break;
            }

            // Проверка, что индекс в пределах массива
            if (idx >= 0 && idx < set.skins.Length)
                return set.skins[idx];

            // fallback: хотя бы [0]
            return set.skins[0];
        }
    }

    return null;
}

    public List<EggStack> GetItemsSnapshot()
    {
        var copy = new List<EggStack>(items.Count);
        foreach (var s in items)
        {
            if (s.IsEgg)
                copy.Add(new EggStack(s.egg, s.type, s.count, s.icon));
            else
                copy.Add(new EggStack(s.brainrot, s.type, s.weightKg, s.icon));
        }
        return copy;
    }

    /// <summary>Есть ли сейчас в инвентаре хотя бы один брайрот?</summary>
    public bool HasAnyBrainrot()
    {
        for (int i = 0; i < items.Count; i++) if (items[i].IsBrainrot) return true;
        return false;
    }

    // --- яйца: стекуем ---
    public bool AddEgg(EggScriptableObject egg, StandardType type, int amount = 1)
    {
        if (!egg || amount <= 0) return false;

        for (int i = 0; i < items.Count; i++)
        {
            var s = items[i];
            if (s.egg == egg && s.type == type)
            {
                s.count += amount;
                OnChanged?.Invoke();
                return true;
            }
        }

        if (items.Count >= MaxSlots) return false;

        items.Add(new EggStack(egg, type, amount, ResolveIcon(egg, null)));
        OnChanged?.Invoke();
        return true;
    }

    public bool TryConsume(EggScriptableObject egg, StandardType type)
    {
        for (int i = 0; i < items.Count; i++)
        {
            var s = items[i];
            if (s.egg == egg && s.type == type && s.count > 0)
            {
                s.count--;
                if (s.count <= 0) items.RemoveAt(i);
                OnChanged?.Invoke();
                return true;
            }
        }
        return false;
    }

    // --- брайроты: НЕ стекуем, каждый в отдельный слот ---
    public bool AddBrainrot(Brainrot so, StandardType type, float kg)
    {
        if (!so) return false;
        if (items.Count >= MaxSlots) return false;

        // до добавления — было ли хоть что-то?
        bool hadAny = HasAnyBrainrot();

        items.Add(new EggStack(so, type, kg, ResolveIcon(null, so)));
        OnChanged?.Invoke();

        // если до этого не было ни одного, а теперь есть — шлём событие
        if (!hadAny)
        {
            Debug.Log("[Inventory] First brainrot gained → notify NPC");
            OnFirstBrainrotGained?.Invoke();
        }
        return true;
    }

    // Assets/Scripts/Inventory/EggInventory.cs
public bool RemoveBrainrot(Brainrot so, StandardType type, float kg, float tol = 0.02f)
{
    for (int i = 0; i < items.Count; i++)
    {
        var s = items[i];
        if (s.IsBrainrot && s.brainrot == so && s.type == type && Mathf.Abs(s.weightKg - kg) <= tol)
        {
            items.RemoveAt(i);
            OnChanged?.Invoke();
            return true;
        }
    }
    return false;
}


    public void ClearAll()
    {
        items.Clear();
        OnChanged?.Invoke();
        // здесь событие не шлём: нас интересует только «стало > 0», а не обратный переход
    }
}
