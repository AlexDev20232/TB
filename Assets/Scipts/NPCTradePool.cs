// Assets/Scripts/Trade/NPCTradePool.cs
using UnityEngine;

[CreateAssetMenu(fileName="NPCTradePool", menuName="Trade/NPC Pool")]
public class NPCTradePool : ScriptableObject
{
    [Header("Какими SO торгует NPC")]
    public Brainrot[] candidates;

    [Header("Ограничения")]
    [Tooltip("NPC не предлагает редкость выше игрока + delta")]
    public int maxRarityDelta = 0;

    [Header("Множители типа (если используешь)")]
    public float multGold    = 1.25f;
    public float multDiamond = 1.5f;
    public float multCandy   = 4f;

    public float TypeMult(StandardType t) => t switch
    {
        StandardType.Gold    => multGold,
        StandardType.Diamond => multDiamond,
        StandardType.Candy   => multCandy,
        _                    => 1f
    };
}
