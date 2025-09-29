using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LimitedOfferVisual : MonoBehaviour
{

    [SerializeField] private GameObject offerPanel; // Панель с предложением
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            offerPanel.SetActive(true); // Показываем панель предложения
        }
    }
}
