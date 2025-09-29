using UnityEngine;
using TMPro;

public class BotIncomeDisplay : MonoBehaviour
{
    [SerializeField] private GameObject incomeDisplayPrefab;
    [SerializeField] private Vector3 offset = new Vector3(0, 1.5f, 0);
    
    private GameObject incomeDisplay;
    private TextMeshProUGUI incomeText;
    private Brainrot botStats;

    private void Start()
    {
        botStats = GetComponent<BrainrotController>().GetStats();
        SpawnIncomeDisplay();
    }

    private void SpawnIncomeDisplay()
    {
        // Спавним префаб над ботом
        incomeDisplay = Instantiate(incomeDisplayPrefab, 
                                  transform.position + offset, 
                                  Quaternion.identity, 
                                  transform);
        
        incomeText = incomeDisplay.GetComponentInChildren<TextMeshProUGUI>();
        incomeText.text = $"+{botStats.incomePerSecond}";
    }

    public void UpdateIncomeDisplay(float bonus = 0)
    {
        incomeText.text = $"+{botStats.incomePerSecond + bonus}/сек";
    }
}