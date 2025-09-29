// Assets/Scripts/Ads/AdCountdown.cs
#if UNITY_EDITOR || UNITY_WEBGL
using System.Collections;
using UnityEngine;
using TMPro;
using YG;                        // плагин YandexGame 2.x

/// <summary>
/// Раз в N секунд показывает счётчик 3‑2‑1 и запускает межстраничную рекламу.
/// При появлении счётчика – игра и звук ставятся на паузу (timeScale = 0).
/// Возобновление – строго после закрытия рекламы пользователем.
/// </summary>
public class AdCountdown : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private CanvasGroup overlay;       // затемнённый экран + TMP
    [SerializeField] private TextMeshProUGUI counter;   // большие цифры 3‑2‑1

    [Header("Интервал (сек)")]
    [Tooltip("0 = брать из InterstitialAdv.interAdvInterval")]
    [SerializeField] private int seconds = 60;          // для тестов можно 15

    private Coroutine loop;           // ссылка на корутину
    private bool waitingAd;           // ждём закрытия рекламы?

    //--------------------------------------------------------------------------------
    private void Start()
    {
        if (SaveBridge.Saves.adsDisabled) return;                 // Remove Ads
        loop = StartCoroutine(Loop());
        YG2.onCloseInterAdv += OnCloseAd;                            // резюмируем игру
    }

    private void OnDestroy()
    {
        if (loop != null) StopCoroutine(loop);
        YG2.onCloseInterAdv -= OnCloseAd;
    }

    //--------------------------------------------------------------------------------
    private IEnumerator Loop()
    {
        // берём реальный интервал из SDK, если нужно
        if (seconds <= 0 && YG2.infoYG && YG2.infoYG.InterstitialAdv != null)
            seconds = YG2.infoYG.InterstitialAdv.interAdvInterval;

        // первая пауза – чтобы игрок не получил рекламу сразу
        yield return new WaitForSeconds(seconds);

        while (true)
        {
            if (!SaveBridge.Saves.adsDisabled)          // вдруг купили Remove Ads
                yield return StartCoroutine(ShowCounterAndAdv());

            yield return new WaitForSeconds(seconds);
        }
    }

    // вывод 3‑2‑1 и показ рекламы ----------------------------------------------------
    private IEnumerator ShowCounterAndAdv()
    {
        // 1. ставим паузу
        Time.timeScale = 0f;
        AudioListener.pause = true;

        overlay.alpha = 1;
        overlay.blocksRaycasts = true;

        // 2. обратный отсчёт (реальное время!)
        for (int i = 3; i >= 1; i--)
        {
            counter.text = i.ToString();
            yield return new WaitForSecondsRealtime(1f);
        }

        overlay.alpha = 0;
        overlay.blocksRaycasts = false;

        // 3. показываем рекламу и ждём закрытия
        waitingAd = true;
        YG2.InterstitialAdvShow();
        while (waitingAd) yield return null;
    }

    // событие от SDK ---------------------------------------------------------------
    private void OnCloseAd()
    {
        waitingAd = false;
        Time.timeScale = 1f;
        AudioListener.pause = false;
    }
}
#endif
