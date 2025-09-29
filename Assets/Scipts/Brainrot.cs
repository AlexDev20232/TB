using UnityEngine;

public enum CharacterRarity
{
    Common,        // Обычный
    Rare,      // редкий
    Epic,          // эпический
    Legendary,     // легендарный
    Mythic,         // мифический
    God,  // бог брайнротов

    Secret        // секретный

}

[CreateAssetMenu(fileName = "NewCharacterData", menuName = "Steal a Brainrot/Character Data")]
public class Brainrot :  ScriptableObject
{
    [Header("Основные параметры")]

    // Brainrot.cs  (SO твоего брайрота)
public float kPerKgBasic = 1.0f;  // K вида (сколько $/s даёт 1 kg для BASIC)
public float baseScale = 1.0f;    // базовый масштаб префаба
public float scalePerKg = 0.05f;  // +5% к масштабу за 1 kg (подстрой если крупно)
public Sprite icon;               // если ещё нет: иконка для поп-апа


    [Tooltip("Название персонажа")]
    public string characterName;

    //[Tooltip("ID персонажа (уникальный)")]
    //public int characterID;

    [Tooltip("Редкость персонажа")]
    public CharacterRarity rarity;

    public StandardType type;

    

    [Tooltip("Префаб персонажа (3D модель)")]
    public GameObject characterPrefab;

    //  public Sprite icon;
  public GameObject iconPrefab;

    [Tooltip("Цена покупки персонажа (в монетах)")]
    public int price;

    [Tooltip("Доход в секунду")]
    public int incomePerSecond;
}
