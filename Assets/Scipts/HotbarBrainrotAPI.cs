// Assets/Scripts/Inventory/HotbarBrainrotAPI.cs
using UnityEngine;

/// <summary>
/// Мини-API, которое PlacePetFromHand дергает у хотбара.
/// Реализуй методы так, как у тебя реально хранятся данные выбранного пета.
/// </summary>
public class HotbarBrainrotAPI : MonoBehaviour
{
    // вернуть, есть ли сейчас выбранный брайрот «в руках/на поводке»
    public virtual bool HasEquippedBrainrot() => _has;
    public virtual Brainrot EquippedBrainrotSO() => _so;
    public virtual StandardType EquippedBrainrotType() => _type;
    public virtual float EquippedBrainrotKg() => _kg;
    public virtual GameObject CurrentPetInstance() => _petInstance;

    // скрыть визуал в руке/поводке (после установки на слот)
    public virtual void HideEquipped()
    {
        if (_petInstance) Destroy(_petInstance);
        _has = false;
        _so = null; _kg = 0f; _type = StandardType.Standard;
    }

    // ------- Здесь просто заглушки/поля для примера --------
    [Header("DEBUG (можно удалить)")]
    [SerializeField] bool _has;
    [SerializeField] Brainrot _so;
    [SerializeField] StandardType _type;
    [SerializeField] float _kg;
    [SerializeField] GameObject _petInstance;

    // В реальном хотбаре заполняй эти поля при выборе слота
    public void DebugSet(Brainrot so, StandardType t, float kg, GameObject petVisual)
    { _has = true; _so = so; _type = t; _kg = kg; _petInstance = petVisual; }
}
