using UnityEngine;

[System.Serializable]
public class TypeParametrs
{
        public StandardType type;
    public GameObject characterPrefab;
    public Brainrot[] EggBrainrot;
}

[CreateAssetMenu(fileName = "BrainrotEgg", menuName = "Eggs")]

public class EggScriptableObject : ScriptableObject
{
  [Header("Основные параметры")]

    [Tooltip("Название персонажа")]
    public string EggName;

    public Sprite icon;

    [Tooltip("Редкость персонажа")]
    public CharacterRarity rarity;

    // В EggScriptableObject
[Header("Инкубация")]
[Tooltip("Время инкубации (секунды) для всех типов этого яйца.")]
public float hatchSeconds = 30f;


     public int price;

    public TypeParametrs[] TypeParametrs;

}
