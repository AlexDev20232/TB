using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using YG;
public class AdsController : MonoBehaviour
{

    public static void AddCheck()
    {
     YG2.onGetSDKData += () =>
        {
            Debug.Log("ADD CHEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEECK!!");
            if (SaveBridge.Saves.adsDisabled)
                DisableInterAdv();
            else
            {
                EnableBannedAd(true);
            }
        };
}

       

    public static void EnableBannedAd(bool Active)
    {
        Debug.Log("EnableBannedAd");
             YG2.StickyAdActivity(Active);
        }


    /// <summary>Полностью блокирует межстраничную рекламу.</summary>
    public static void DisableInterAdv()
    {
        Debug.Log("DisableInterAdv()");
        if (SaveBridge.Saves.adsDisabled) return;   // уже выключено

        SaveBridge.Saves.adsDisabled = true;
        SaveBridge.Save();                          // локально + облако

        YG2.StickyAdActivity(false);
        Debug.Log("YG2.StickyAdActivity(false)");
        // 1) выключаем все активные таймеры
        foreach (var t in Object.FindObjectsOfType<TimerBeforeAdsYG>())
            t.gameObject.SetActive(false);

        // 2) опционально: если используете SDK‑шный интервал,
        //    ставим огромное значение, чтобы страховочно "отодвинуть" рекламу.
        //YG2.SetInterstitialInterval(int.MaxValue);
    }
}
