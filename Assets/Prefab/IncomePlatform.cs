using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IncomePlatform : MonoBehaviour
{
    void OnTriggerEnter(Collider other)
    {
        if (other.tag == "Player")
        {
            if (gameObject.GetComponentInChildren<IncomeDisplay>() != null)
            {
                GameManager.Instance.AddMoney(gameObject.GetComponentInChildren<IncomeDisplay>().GetCurrentIncome());
                gameObject.GetComponentInChildren<IncomeDisplay>().ResetIncome();
                SoundsManager.Instance.PlayGetMoneySound();

              if (!AutoArrowLine.Instance.IsTutorialCompleted)
                {
                    AutoArrowLine.Instance.TutorialEnd();
                }

             
           }
        }
    }
}
