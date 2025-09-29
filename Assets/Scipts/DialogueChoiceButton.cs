using UnityEngine;
using UnityEngine.UI;

public class DialogueChoiceButton : MonoBehaviour
{
    public int choiceIndex = 1;
    [TextArea] public string playerLine;
    [TextArea] public string npcAnswer; // для обычных пунктов (не №1)

    public DialogueManager dialogueManager;

    Button _btn;
    void Awake()
    {
        _btn = GetComponent<Button>();
        if (_btn) _btn.onClick.AddListener(OnClick);
    }

    void OnClick()
    {
        if (!dialogueManager)
        {
            Debug.LogWarning("[DialogueChoiceButton] Нет ссылки на DialogueManager");
            return;
        }
        dialogueManager.ShowDialogue(choiceIndex, playerLine, npcAnswer);
        GameObject parent = transform.parent.gameObject;
        parent.SetActive(false);
    }
}
