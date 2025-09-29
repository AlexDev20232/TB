// Assets/Scripts/Shop/DialogueChoicesController.cs
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Управляет показом панели выбора фраз:
///  - включает панель,
///  - выключает все кнопки,
///  - включает по одной с задержкой stepDelay.
/// </summary>
public class DialogueChoicesController : MonoBehaviour
{
    [Header("Ссылки")]
    [Tooltip("Родительский объект панели (включается/выключается целиком).")]
    public GameObject panelRoot;
    [Tooltip("Родитель, внутри которого лежат кнопки-варианты.")]
    public Transform choicesParent;

    [Header("Появление кнопок")]
    [Tooltip("Пауза перед показом первой кнопки.")]
    public float firstDelay = 0.1f;
    [Tooltip("Интервал между появлением кнопок.")]
    public float stepDelay = 0.35f;

    Coroutine _reveal;
    readonly List<GameObject> _buttons = new();

    void Awake()
    {
        CollectButtons();
        // на старте панель должна быть выключена — но если включена,
        // всё равно принудительно выключим, чтобы не мигало
        if (panelRoot && panelRoot.activeSelf) panelRoot.SetActive(false);
    }

    void CollectButtons()
    {
        _buttons.Clear();
        if (!choicesParent) return;

        for (int i = 0; i < choicesParent.childCount; i++)
        {
            var t = choicesParent.GetChild(i);
            _buttons.Add(t.gameObject);
        }
    }

    public void Open()
    {
        if (!panelRoot || !choicesParent)
        {
            Debug.LogWarning("[DialogueChoices] panelRoot/choicesParent не назначены.");
            return;
        }

        if (_reveal != null) StopCoroutine(_reveal);

        panelRoot.SetActive(true);
        // выключаем все кнопки
        foreach (var b in _buttons) if (b) b.SetActive(false);

        _reveal = StartCoroutine(RevealRoutine());
    }

    public void Close()
    {
        if (_reveal != null) StopCoroutine(_reveal);
        _reveal = null;
        if (panelRoot) panelRoot.SetActive(false);
    }

    IEnumerator RevealRoutine()
    {
        yield return new WaitForSeconds(firstDelay);

        foreach (var b in _buttons)
        {
            if (b) b.SetActive(true);
            yield return new WaitForSeconds(stepDelay);
        }
        _reveal = null;
    }

#if UNITY_EDITOR
    // чтобы можно было обновить список кнопок, если их состав изменили в редакторе
    [ContextMenu("Refresh Buttons List")]
    void RefreshButtonsInEditor()
    {
        CollectButtons();
    }
#endif
}
