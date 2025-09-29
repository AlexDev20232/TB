// Файл: SavesYG.Fields.cs
using System;
using System.Collections.Generic;

namespace YG
{
    /// <summary>
    /// Контейнер сохранений плагина (часть с нашими полями).
    /// ВАЖНО: эти поля объявлены только здесь и нигде больше.
    /// </summary>
    [Serializable]
    public partial class SavesYG
    {
        // Список ключей купленных/открытых Brainrot
        public List<string> unlocked = new();
    public  long coins = 0;            
        // Состояние занятых слотов на базе + накопленный доход
        public List<SlotSave> placed = new();
 public bool tutorialCompleted = false; // пройден ли туториал
   public float mouseSensitivity = 1.5f; 
 public float musicVolume = 0.09f;   // 0…1
        public float sfxVolume = 0.8f;   // 0…1

// part of SavesYG
public int currentRebirthStage = 0;   // 0 = первая стадия
public float rebirthIncomeMultiplier = 0;   // суммарный множитель от всех перерождений
public bool secondFloorOpened = false;
        public int secondFloorSlotsUnlocked = 0;   // сколько слотов на 2м этаже уже включено



public bool thirdFloorOpened;
public int  thirdFloorSlotsUnlocked;

        public bool vipOwned = false;
public bool luckX3Owned     = false;
        public bool moneyX2Owned = false;
        public bool limitedOfferOwned = false;

public bool starterPackOwned = false;   // ← куплен ли бандл
public bool adsDisabled     = false;   // отдельный флаг выключения рекламы
        // Конструктор — на всякий случай инициализируем, если десериализация вернула null
        public SavesYG()
        {
            if (unlocked == null) unlocked = new List<string>();
            if (placed == null) placed = new List<SlotSave>();
        }


    }
    [Serializable]
    public class SlotSave
    {
        public int slotIndex;     // индекс в своём списке
        public int floor;         // 0 = 1 этаж, 1 = 2 этаж, 2 = 3 этаж

        // ---- если это брайрот ----
        public string brainrotKey;
        public int storedIncome;

        // ---- если это яйцо ----
        public bool isEgg;         // true = на слоте яйцо
        public string eggKey;        // имя ScriptableObject яйца
        public int eggType;       // (int) StandardType (0=Standard,1=Gold,2=Diamond,3=Candy)

        public float eggIncubationLeft; // оставшееся время до открытия (сек), на будущее
    
      public float  weightKg;
}

}
