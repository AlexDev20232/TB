using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

public class ButtonChose : MonoBehaviour
{
    // 1) Твой enum
    public enum Type
    {
        Yes, Add, No
    }

    [Header("Настройка кнопки")]
    private Button button;         
    [SerializeField] private Type currentType = Type.Yes; 


    public static event Action<Type> OnChoice; 


    private void Awake()
    {
        if (!button) button = GetComponent<Button>();
    }

    private void OnEnable()
    {
        if (button)
            button.onClick.AddListener(ChoiceEvent);
    }

    private void OnDisable()
    {
        if (button)
            button.onClick.RemoveListener(ChoiceEvent);
    }

    // Вызывается по клику
    private void ChoiceEvent()
    {
        OnChoice?.Invoke(currentType);
    }
}
