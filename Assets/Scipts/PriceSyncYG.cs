// PriceSyncYG.cs
using UnityEngine;
using YG;

[RequireComponent(typeof(ShopItemButton))]
public class PriceSyncYG : MonoBehaviour
{
    private ShopItemButton btn;

    private void Awake()
    {
       
    }

    void Start()
    {
 btn = GetComponent<ShopItemButton>();
   //pdatePrice();
        //if (YG2.isInitComplete)

       // else
            //YG2.onGetPayments += UpdatePrice;
    }
    
/*
    private void UpdatePrice()
    {
        var p = YG2.PurchaseByID(btn.purchaseId);
        if (p != null)         // товар найден в каталоге/симуляторе
        {
            string priceStr = $"{p.priceValue} {p.priceCurrencyCode}";
            if (btn.priceText) btn.priceText.text = priceStr;
        }
        // если p == null  → оставляем текст, проставленный в инспекторе
    }
    */
}
