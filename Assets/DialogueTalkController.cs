// Assets/Scripts/NPCDialogue/DialogueTalkController.cs
using System.Collections;
using UnityEngine;

public class DialogueTalkController : MonoBehaviour
{
    [Header("Ссылки на якоря")]
    public Transform playerAnchor;                // пустышка над игроком (голова)
    public Transform npcAnchor;                   // пустышка над NPC (голова)

    [Header("Презентация")]
    public SpeechBubbleBillboard bubblePrefab;    // префаб облачка (с SpeechBubbleBillboard)
    public Vector3 playerOffset = new Vector3(0, 1.8f, 0);
    public Vector3 npcOffset    = new Vector3(0, 2.0f, 0);
    public float npcReplyDelay  = 0.4f;

    [Header("Ответы NPC по индексам выбора")]
    [TextArea(1,3)]
    public string[] npcReplies;                   // ответ на каждую кнопку

    SpeechBubbleBillboard _playerBubble;
    SpeechBubbleBillboard _npcBubble;
    Coroutine _replyCoro;

    /// <summary>Вызывается меню при выборе варианта.</summary>
    public void OnPlayerChoice(int index, string playerText)
    {
        if (!_playerBubble)
            _playerBubble = Instantiate(bubblePrefab, transform);
        _playerBubble.Init(playerAnchor, playerText, playerOffset);

        // отменим прежнюю корутину ответа
        if (_replyCoro != null) StopCoroutine(_replyCoro);
        _replyCoro = StartCoroutine(ReplyAfterDelay(index));
    }

    IEnumerator ReplyAfterDelay(int index)
    {
        yield return new WaitForSeconds(npcReplyDelay);

        string reply = (index >= 0 && index < npcReplies.Length) ? npcReplies[index] : "...";

        if (!_npcBubble)
            _npcBubble = Instantiate(bubblePrefab, transform);
        _npcBubble.Init(npcAnchor, reply, npcOffset);

        _replyCoro = null;
    }

    public void ClearBubbles()
    {
        if (_playerBubble) { Destroy(_playerBubble.gameObject); _playerBubble = null; }
        if (_npcBubble)    { Destroy(_npcBubble.gameObject);    _npcBubble = null;    }
        if (_replyCoro != null) { StopCoroutine(_replyCoro); _replyCoro = null; }
    }
}
