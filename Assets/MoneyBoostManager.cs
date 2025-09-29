using UnityEngine;
using System.Collections;
using TMPro;

/// <summary> Считает суммарный множитель денег. </summary>
public class MoneyBoostManager : MonoBehaviour
{
    public static MoneyBoostManager Instance { get; private set; }

    public bool  TempBoostActive   => _timeLeft > 0f;
    public float CurrentMultiplier => _permanentBase * (TempBoostActive ? 2f : 1f);
    public float TimeLeft          => _timeLeft;

    [Header("UI")]
    [SerializeField] private TextMeshProUGUI boostText;

    private float _permanentBase = 1f;    // 1 + (MoneyX2?2) + (VIP?0.5)
    private float _timeLeft;              // секунд до конца рекламы
    private const float _tick = 1f;

    // ───────────────────────────────────────── Singleton
    private void Awake()
    {
        if (Instance && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        RecalcPermanent();
        UpdateText();
    }

    // ───────────────────────────────────────── Public API
    /// <summary>Запускает / продлевает временной ×2‑буст.</summary>
    public void Activate(float durationSec)
    {
        _timeLeft = Mathf.Max(_timeLeft, durationSec);
        UpdateText();
        StopAllCoroutines();
        StartCoroutine(TimerLoop());
    }

    /// <summary>Вызывается из ShopManager после покупки постоянного буста.</summary>
    public void RefreshPermanent()
    {
        RecalcPermanent();
        UpdateText();
    }

    // ───────────────────────────────────────── Internal
    private IEnumerator TimerLoop()
    {
        while (_timeLeft > 0f) { yield return new WaitForSecondsRealtime(_tick); _timeLeft -= _tick; }
        UpdateText();                          // реклама закончилась
    }

    private void RecalcPermanent()
    {
        _permanentBase = 1f;                   // базовый 1 ×
        if (SaveBridge.Saves.moneyX2Owned) _permanentBase += 2f;   // +2 ×
        if (SaveBridge.Saves.vipOwned) _permanentBase += .5f;  // +0.5 ×
        _permanentBase += SaveBridge.Saves.rebirthIncomeMultiplier;
    }
    
    private void UpdateText()
    {
        if (!boostText) return;
        boostText.text = $"Множитель денег x{CurrentMultiplier:0.#}";
    }
}
