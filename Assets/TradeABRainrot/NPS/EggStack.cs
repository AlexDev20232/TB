// Assets/Scripts/Inventory/EggStack.cs
using UnityEngine;

[System.Serializable]
public class EggStack
{
    // ОБЩЕЕ
    public StandardType type;     // Standard/Gold/Diamond/Candy
    public Sprite icon;           // иконка для UI

    // ЯЙЦО
    public EggScriptableObject egg;
    public int count;             // xN (только для яиц)

    // БРАЙРОТ
    public Brainrot brainrot;     // если это брайрот
    public float weightKg;        // вес (для брайрота)

    // --- конструкторы ---
    public EggStack(EggScriptableObject egg, StandardType type, int amount, Sprite icon)
    {
        this.egg   = egg;
        this.type  = type;
        this.count = Mathf.Max(1, amount);
        this.icon  = icon;
        this.brainrot = null;
        this.weightKg = 0f;
    }

    public EggStack(Brainrot so, StandardType type, float kg, Sprite icon)
    {
        this.brainrot = so;
        this.type     = type;
        this.weightKg = kg;
        this.icon     = icon;
        this.egg      = null;
        this.count    = 1; // x1 для UI можно не показывать
    }

    public bool IsEgg      => egg      != null;
    public bool IsBrainrot => brainrot != null;
}
