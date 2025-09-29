using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using YG;

using TMPro;
public class BaseName : MonoBehaviour
{

    [SerializeField] private TextMeshProUGUI nameText;
    void Start()
    {
        if (YG2.player.auth && YG2.player.name != "")
        {
            nameText.text = "База " + YG2.player.name;
        }
        else
        {
            nameText.text = "Ваша База";
        }
    }
}
