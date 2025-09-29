// Assets/Scripts/Rebirth/RebirthPanel.cs
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class RebirthPanel : MonoBehaviour
{
    [Header("UI refs")]
    public Slider coinsSlider;
    public TextMeshProUGUI coinsText;
    public Transform reqGridParent;
    public GameObject reqItemPrefab;
    public TextMeshProUGUI rewardMultiplier;
    public TextMeshProUGUI rewardCoins;
    public TextMeshProUGUI rewardSlots;   // покажем +слоты
    public Button rebirthBtn;

    RebirthStage _stage;


    [SerializeField] private GameObject unlockedPanel;

    void OnEnable()
    {
        Refresh();
        if (GameManager.Instance != null)
            GameManager.Instance.OnMoneyChange += RefreshCoins;
    }

    void OnDisable()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnMoneyChange -= RefreshCoins;
    }

    public void Refresh()
    {
        _stage = RebirthManager.Instance.CurrentStage;

        if (_stage == null)
        {
            coinsSlider.maxValue = 1;
            coinsSlider.value = 1;
            coinsText.text = "Выполнено!";
            unlockedPanel.SetActive(true);
            rewardCoins.text = "$0";
            rewardMultiplier.text = "x" + SaveBridge.Saves.rebirthIncomeMultiplier.ToString("0.##");
            rewardSlots.text = "";

            foreach (Transform c in reqGridParent) Destroy(c.gameObject);

            rebirthBtn.interactable = RebirthManager.Instance.devBypassRequirements;
            return;
        }

        // 1) прогресс по монетам
        coinsSlider.maxValue = _stage.requiredCoins;
        RefreshCoins(GameManager.Instance.Money);

        // 2) список требований
        foreach (Transform c in reqGridParent) Destroy(c.gameObject);
        foreach (var br in _stage.requiredBrainrots)
        {
            if (!br) continue;
            var go = Instantiate(reqItemPrefab, reqGridParent);

            var icon = go.transform.Find("Icon")?.GetComponent<Image>();
           // if (icon)
           // {
                var instantiatedIcon = Instantiate(br.iconPrefab, icon.transform);
                instantiatedIcon.GetComponent<Image>().sprite = instantiatedIcon.GetComponent<BrainrotIconSet>().skins[0];
                instantiatedIcon.GetComponent<Image>().color = Color.black;
               // var set = br.iconPrefab ? br.iconPrefab.GetComponent<BrainrotIconSet>() : null;
                //  if (set && set.skins != null && set.skins.Length > 0)
                //   {
                //  icon.sprite = set.skins[0];

                // }
          //  }

            var nameTxt = go.GetComponentInChildren<TextMeshProUGUI>();
            if (nameTxt) nameTxt.text = br.characterName;

            bool have = BaseController.Instance.HasBrainrotByName(br.characterName);
            var tick = go.transform.Find("Tick");
            if (tick) tick.gameObject.SetActive(have);
            
            if (have)
            {
                  instantiatedIcon.GetComponent<Image>().color = Color.white;
             //   icon.color = Color.white;
            }
        }

        // 3) награды
        rewardCoins.text = "$" + MoneyFormatter.Short(_stage.grantCoins);
        int stageIndex = SaveBridge.Saves.currentRebirthStage ; 
        if( stageIndex < 1) stageIndex = 1; // чтобы не было нуля
      rewardMultiplier.text = "x" + (_stage.incomeMultiplierPlus * stageIndex).ToString("0.##"); //+ SaveBridge.Saves.rebirthIncomeMultiplier

        // красивый текст по слотам
        string slotsTxt = "";
        if (_stage.addSlotsFloor2 > 0) slotsTxt += $"+{_stage.addSlotsFloor2}";
        if (_stage.addSlotsFloor3 > 0) slotsTxt += $"+{_stage.addSlotsFloor3}";
        rewardSlots.text = slotsTxt.Trim();

        // 4) кнопка: DEV — всегда активна
        rebirthBtn.interactable =
            RebirthManager.Instance.devBypassRequirements ||
            RebirthManager.Instance.RequirementsMet();
    }

    void RefreshCoins(long money)
    {
        if (_stage == null)
        {
            rebirthBtn.interactable = RebirthManager.Instance.devBypassRequirements;
            return;
        }

        coinsSlider.value = Mathf.Min(money, _stage.requiredCoins);
        coinsText.text = $"${MoneyFormatter.Short(money)} / ${MoneyFormatter.Short(_stage.requiredCoins)}";
        rebirthBtn.interactable =
            RebirthManager.Instance.devBypassRequirements ||
            RebirthManager.Instance.RequirementsMet();
    }

    public void OnClickRebirth() => RebirthManager.Instance.DoRebirth();
}
