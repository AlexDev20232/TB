// Assets/Scripts/Shop/ShopTalkPrompt.cs
using UnityEngine;
using TMPro;

/// <summary>
/// Показ подсказки "E – Talk" над NPC при входе игрока в триггер.
/// По нажатию E скрывает подсказку и открывает меню диалога.
/// Когда диалог полностью завершён (событие DialogueManager.OnConversationFinished),
/// подсказка возвращается (с задержкой).
/// </summary>
[RequireComponent(typeof(Collider))]
public class ShopTalkPrompt : MonoBehaviour
{
    [Header("Подсказка")]
    public GameObject promptPrefab;        // префаб подсказки (World-Space Canvas)
    public Transform promptAnchor;         // точка над NPC
    public Vector3 promptOffset = new Vector3(0f, 1.4f, 0f);
    public KeyCode interactKey = KeyCode.E;

    [Header("Диалог")]
    public DialogueChoicesController dialogue; // панель с вариантами
    public DialogueManager dialogueManager;    // менеджер реплик

    [Header("Возврат подсказки")]
    [Tooltip("Через сколько секунд вернуть подсказку после завершения диалога.")]
    public float promptReturnDelay = 0.5f;

    [Header("Надпись у NPC (необязательно)")]
    public TextMeshProUGUI npcIdleText;        // текст над NPC
    public string idleTextAfter = "Sell brainrots";

    [Header("Игрок")]
    public string playerTag = "Player";

    bool _insideZone;
    bool _dialogueActive;
    GameObject _prompt;
    Coroutine _returnCo;

    void Reset()
    {
        var col = GetComponent<Collider>();
        if (col) col.isTrigger = true;

        if (!promptAnchor)  promptAnchor  = transform;
        if (!dialogue)      dialogue      = FindObjectOfType<DialogueChoicesController>(true);
        if (!dialogueManager) dialogueManager = FindObjectOfType<DialogueManager>(true);
    }

    void OnEnable()
    {
        if (dialogueManager)
            dialogueManager.OnConversationFinished += OnConversationFinished;
    }

    void OnDisable()
    {
        if (dialogueManager)
            dialogueManager.OnConversationFinished -= OnConversationFinished;

        HidePrompt();
        _insideZone = false;
        _dialogueActive = false;

        if (_returnCo != null) { StopCoroutine(_returnCo); _returnCo = null; }
    }

    // ─────────────────────────── триггер ───────────────────────────

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;
        _insideZone = true;

        // показываем подсказку только если диалог сейчас не идёт
        if (!_dialogueActive) ShowPrompt();
    }

    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;
        _insideZone = false;

        HidePrompt();
        if (dialogue) dialogue.Close();      // закрыть меню, если открыто
    }

    // ─────────────────────────── апдейт ───────────────────────────

    void LateUpdate()
    {
        // обновляем позицию/поворот подсказки
        if (_prompt && promptAnchor)
        {
            _prompt.transform.position = promptAnchor.position + promptAnchor.TransformVector(promptOffset);
            if (Camera.main)
                _prompt.transform.rotation = Quaternion.LookRotation(Camera.main.transform.forward);
        }

        // открытие диалога по E
        if (_insideZone && !_dialogueActive && Input.GetKeyDown(interactKey) && dialogue)
        {
            _dialogueActive = true;
            HidePrompt();       // спрятать подсказку на время диалога
            dialogue.Open();    // открыть меню вариантов
        }
    }

    // ─────────────────────────── прятать/показывать ───────────────────────────

    void ShowPrompt()
    {
        if (_prompt || !promptPrefab) return;
        _prompt = Instantiate(promptPrefab);
        _prompt.transform.position = promptAnchor.position + promptAnchor.TransformVector(promptOffset);
        if (Camera.main)
            _prompt.transform.rotation = Quaternion.LookRotation(Camera.main.transform.forward);
    }

    void HidePrompt()
    {
        if (_prompt) { Destroy(_prompt); _prompt = null; }
    }

    // ─────────────────────────── конец диалога ───────────────────────────

    void OnConversationFinished()
    {
        _dialogueActive = false;

        if (_returnCo != null) StopCoroutine(_returnCo);
        _returnCo = StartCoroutine(ReturnPromptDelayed());
    }

    System.Collections.IEnumerator ReturnPromptDelayed()
    {
        // обновить надпись у NПС (если есть)
        if (npcIdleText) npcIdleText.text = idleTextAfter;

        yield return new WaitForSeconds(Mathf.Max(0f, promptReturnDelay));

        if (_insideZone) ShowPrompt(); // вернуть подсказку только если игрок всё ещё рядом
        _returnCo = null;
    }
}
