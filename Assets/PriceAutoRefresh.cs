using UnityEngine;
using YG;

public class PriceAutoRefresh : MonoBehaviour
{
    void OnEnable()  => YG2.onGetPayments += Redraw;
    void OnDisable() => YG2.onGetPayments -= Redraw;

    void Redraw()
    {
        foreach (var p in Resources.FindObjectsOfTypeAll<PurchaseYG>())
        {
            var data = YG2.PurchaseByID(p.id);
            if (data != null) p.UpdateEntries(data);   // перерисовать цену
        }
    }
}
