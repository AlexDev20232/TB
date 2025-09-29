// Assets/Scripts/Eggs/TypeOfEgg.cs
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Каталог всех яиц. У каждого яйца внутри есть массив TypeParametrs[4],
/// где индекс 0=Standard, 1=Gold, 2=Diamond, 3=Candy.
/// </summary>
[CreateAssetMenu(fileName = "EggTypeSet", menuName = "Steal a Brainrot/Egg Type Set")]
public class TypeOfEgg : ScriptableObject
{
    [Header("Все яйца, доступные в игре")]
    public EggScriptableObject[] allEgs;

    /// <summary>
    /// Вернуть весь список Brainrot'ов, которые могут выпасть
    /// из яиц данного типа (Standard/Gold/Diamond/Candy).
    /// Дубликаты по ссылке автоматически убираются.
    /// </summary>
    public Brainrot[] GetBrainrotsByType(StandardType type)
    {
        int idx = TypeIndex(type); // 0..3
        var set = new HashSet<Brainrot>(); // чтобы не было дублей

        if (allEgs == null) return System.Array.Empty<Brainrot>();

        foreach (var egg in allEgs)
        {
            if (egg == null || egg.TypeParametrs == null) continue;
            if (idx < 0 || idx >= egg.TypeParametrs.Length) continue;

            var tp = egg.TypeParametrs[idx];
            if (tp == null || tp.EggBrainrot == null) continue;

            foreach (var br in tp.EggBrainrot)
                if (br) set.Add(br);
        }

        var result = new Brainrot[set.Count];
        set.CopyTo(result);
        return result;
    }

    /// <summary>
    /// Случайное яйцо указанной редкости (например, Legendary/Mythic).
    /// </summary>
    public EggScriptableObject GetRandomEggByRarity(CharacterRarity rarity)
    {
        if (allEgs == null || allEgs.Length == 0)
        {
            Debug.LogWarning("[TypeOfEgg] В каталоге нет яиц.");
            return null;
        }

        var pool = allEgs.Where(e => e != null && e.rarity == rarity).ToArray();
        if (pool.Length == 0)
        {
            Debug.LogWarning("[TypeOfEgg] Нет яиц с редкостью: " + rarity);
            return null;
        }

        return pool[Random.Range(0, pool.Length)];
    }

    /// <summary>
    /// Достаёт конкретный набор параметров для яйца по типу (Standard/Gold/Diamond/Candy).
    /// </summary>
    public static TypeParametrs GetParamsForType(EggScriptableObject egg, StandardType type)
    {
        if (egg == null || egg.TypeParametrs == null) return null;
        foreach (var tp in egg.TypeParametrs)
            if (tp != null && tp.type == type)
                return tp;
        return null;
    }

    /// <summary>Маппинг типа → индекс в массиве TypeParametrs.</summary>
    private static int TypeIndex(StandardType type) => type switch
    {
        StandardType.Standard => 0,
        StandardType.Gold     => 1,
        StandardType.Diamond  => 2,
        StandardType.Candy    => 3,
        _ => 0
    };
}

/// <summary>Тип яиц (такой же индекс в массиве TypeParametrs).</summary>
public enum StandardType
{
    Standard,
    Gold,
    Diamond,
    Candy
}
