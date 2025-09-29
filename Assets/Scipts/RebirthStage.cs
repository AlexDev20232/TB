using UnityEngine;

[CreateAssetMenu(fileName="RebirthStage", menuName="Game/Rebirth Stage")]
public class RebirthStage : ScriptableObject
{
    public long requiredCoins;
    public Brainrot[] requiredBrainrots;

    [Header("Rewards")]
    public int grantCoins = 0;
    public float incomeMultiplierPlus = 0f;

    [Header("Slots to add on Rebirth")]
    public int addSlotsFloor2 = 0;
    public int addSlotsFloor3 = 0;
}
