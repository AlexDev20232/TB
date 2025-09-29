/*********************************************************
 *  RewardZoneYG.cs  (fixed)
 *********************************************************/
using UnityEngine;
using UnityEngine.UI;
using YG;

[RequireComponent(typeof(Collider))]
public class RewardZone : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Image radialFill;

    [Header("Время")]
    [SerializeField] private float fillSeconds = 3f;
    [SerializeField] private bool  useUnscaledTime = true;

    [Header("Награда")]
    [SerializeField] private int    rewardCoins = 250;
    [SerializeField] private string rewardId    = "CoinsReward250";

    // ─────────────────────────────── internal state
    bool  playerInside;
    bool  adShowing;
    float timer;
    float DeltaTime => useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;

    void Awake()
    {
        if (radialFill) radialFill.fillAmount = 0f;
    }

    // ─────────────────────────────── trigger
    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player") || adShowing) return;

        playerInside = true;
        timer        = 0f;
        if (radialFill) radialFill.fillAmount = 0f;
    }

    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        ResetProgress();
    }

    // ─────────────────────────────── update
    void Update()
    {
        if (!playerInside || adShowing) return;

        timer += DeltaTime;
        float k = Mathf.Clamp01(timer / Mathf.Max(0.001f, fillSeconds));
        if (radialFill) radialFill.fillAmount = k;

        if (k >= 1f)
            StartCoroutine(ShowRewardedAd());
    }

    // ─────────────────────────────── ad logic
    System.Collections.IEnumerator ShowRewardedAd()
    {
        adShowing = true;
        if (radialFill) radialFill.fillAmount = 0f;

        bool rewardGiven = false;

        // подписываемся на факт закрытия ЛЮБОЙ рекламы
        bool adClosed = false;
        void OnAdClose() => adClosed = true;
        YG2.onCloseAnyAdv += OnAdClose;

        // стартуем показ
        YG2.RewardedAdvShow(rewardId, () => rewardGiven = true);

        // ждём, пока реклама перестанет отображаться
        yield return new WaitUntil(() => adClosed == true);

        YG2.onCloseAnyAdv -= OnAdClose;        // отписались

        if (rewardGiven)
        {

            if(rewardId == "CoinsReward250")
            {
                int mult = SaveBridge.Saves.moneyX2Owned ? 2 : 1;
            GameManager.Instance.AddMoney(rewardCoins * mult);
            Debug.Log($"[RewardZone] +{rewardCoins * mult} монет");
            }
            else
            {
                 int mult = 1;
    if (SaveBridge.Saves.moneyX2Owned || SaveBridge.Saves.vipOwned) mult *= 2;
    GameManager.Instance.AddMoney(rewardCoins * mult);

    // 2. Временной буст дохода на 120 секунд
    MoneyBoostManager.Instance.Activate(120f);
    Debug.Log("[RewardZone] Активирован временной x2 на 2 минуты");
            } 
          
        }

        ResetProgress();
        adShowing = false;
    }

    // ─────────────────────────────── helpers
    void ResetProgress()
    {
        playerInside = false;
        timer = 0f;
        if (radialFill) radialFill.fillAmount = 0f;
    }
}
