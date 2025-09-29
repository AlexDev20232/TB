using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class TradeItem
{
    public Brainrot so;
    public StandardType type;
    public float weightKg;
    public float value;   // $/s
    public Sprite icon;
}

[System.Serializable]
public class TradeOffer
{
    public List<TradeItem> playerGive = new();
    public List<TradeItem> npcGive    = new();

    public float playerValue;  // сумма $/s слева (игрок)
    public float npcValue;     // сумма $/s справа (NPC)

    // ✔ Правильная метрика честности: (NPC − Player) / Player
    // Положительное => выгодно игроку; 0 => равный обмен.
    public float FairnessRel
    {
        get
        {
            float pv = Mathf.Max(0.0001f, playerValue); // защита от деления на 0
            return (npcValue - pv) / pv;
        }
    }

    // ✔ Округление до 0.1% для устойчивой классификации (борьба с флоат-шумом)
    public float FairnessRounded => Mathf.Round(FairnessRel * 1000f) / 1000f;
}
