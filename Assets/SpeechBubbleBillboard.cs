// Assets/Scripts/NPCDialogue/SpeechBubbleBillboard.cs
using UnityEngine;
using TMPro;

public class SpeechBubbleBillboard : MonoBehaviour
{
    public Transform anchor;
    public Vector3 offset = new Vector3(0, 1.8f, 0);
    public TextMeshProUGUI label;

    public void Init(Transform anchor, string text, Vector3 offset)
    {
        this.anchor = anchor;
        this.offset = offset;
        if (label) label.text = text;
        LateUpdate(); // сразу поставить
        gameObject.SetActive(true);
    }

    public void SetText(string text)
    {
        if (label) label.text = text;
    }

    void LateUpdate()
    {
        if (!anchor) return;
        transform.position = anchor.position + anchor.TransformVector(offset);
        if (Camera.main)
            transform.rotation = Quaternion.LookRotation(Camera.main.transform.forward);
    }
}
