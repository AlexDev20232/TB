using UnityEngine;
using TMPro;

/// <summary>
/// Автоматически ищет нужного Brainrot по его ScriptableObject,
/// выбирает случайного из подходящих, держит его, пока не исчезнет.
/// Если цели нет — ждёт и ищет снова.
/// </summary>
[RequireComponent(typeof(ArrowLine))]
public class AutoArrowLine : MonoBehaviour
{
    [SerializeField] private Brainrot targetTypeSO;
    [SerializeField] private Transform startPoint;
    [SerializeField] private float searchInterval = 0.5f;

    [Header("Куда указывать после прихода на базу")]
    [SerializeField] private Transform afterBasePoint;

    private ArrowLine arrowLine;
    private BrainrotController currentTarget;
    private float searchTimer;

    private bool boughtNotified;
    private bool arrivedNotified;

    [Header("Туториал")]
    public GameObject TutorialPanel;
    private TextMeshProUGUI TutorialText;
    [Tooltip("0 — подсказка до покупки; 1 — после покупки; 2 — после прибытия на базу")]
    public string[] TutorialMessage;

    public bool IsTutorialCompleted { get; private set; }

    public static AutoArrowLine Instance { get; private set; }

    private void Awake()
    {
        Instance = this;
        arrowLine = GetComponent<ArrowLine>();
        TutorialText = TutorialPanel ? TutorialPanel.GetComponentInChildren<TextMeshProUGUI>() : null;

        // Читаем флаг из сейва
        IsTutorialCompleted = YG.YG2.saves.tutorialCompleted;

        if (IsTutorialCompleted)
        {
            if (TutorialPanel) TutorialPanel.SetActive(false);
            arrowLine.ActiveArrowLine(false);
            if (arrowLine.lr) arrowLine.lr.gameObject.SetActive(false);
            boughtNotified = true;
            arrivedNotified = true;
        }
        else
        {
            // Показать первую подсказку, если есть текст
            if (TutorialText && TutorialMessage != null && TutorialMessage.Length > 0)
                TutorialText.text = TutorialMessage[0];
        }
    }

private void Start()
{
    if (IsTutorialCompleted) {      // ← если туториал уже пройден — ничего не делаем
        arrowLine.ActiveArrowLine(false);
        if (arrowLine.lr) arrowLine.lr.gameObject.SetActive(false);
        enabled = false;            // ← полностью отключаем скрипт
        return;
    }

    TryFindTarget();
    ApplyTarget();
}


    private void Update()
    {
        /*
        // Сброс туториала (для теста)
        if (Input.GetKeyDown(KeyCode.R))
        {
            YG.YG2.saves.tutorialCompleted = false;
            SaveBridge.Save(); // локально + облако
            UnityEngine.SceneManagement.SceneManager.LoadScene(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
        }

        */

         if (IsTutorialCompleted || !enabled) return;  // ← ранний выход

        if (!startPoint)
        {
            arrowLine.ActiveArrowLine(false);
            return;
        }

        if (!IsValid(currentTarget))
        {
            arrowLine.ActiveArrowLine(false);
            searchTimer += Time.deltaTime;
            if (searchTimer >= searchInterval)
            {
                searchTimer = 0f;
                TryFindTarget();
                ApplyTarget();
                boughtNotified = false;
                arrivedNotified = false;
            }
            return;
        }

        // 1) Купили, но ещё не дошёл
        if (currentTarget.IsBought && !boughtNotified)
        {
            Debug.Log("Вы купили нужного брейнрота");
            if (arrowLine.lr) arrowLine.lr.gameObject.SetActive(false);
            boughtNotified = true;

            if (TutorialText && TutorialMessage != null && TutorialMessage.Length > 1)
                TutorialText.text = TutorialMessage[1];

            return;
        }

        // 2) Дошёл до базы — переключаем на другую точку
        if (currentTarget.IsPurchased && !arrivedNotified)
        {
            arrivedNotified = true;

            if (TutorialText && TutorialMessage != null && TutorialMessage.Length > 2)
                TutorialText.text = TutorialMessage[2];

            if (arrowLine.lr) arrowLine.lr.gameObject.SetActive(true);
        }

        if (arrivedNotified && afterBasePoint)
        {
            arrowLine.StartArrowLine(startPoint, afterBasePoint);
            return;
        }

        // Иначе обычная стрелка на цель
        arrowLine.StartArrowLine(startPoint, currentTarget.transform);
    }
    
    private void OnEnable()
{
    if (IsTutorialCompleted)
    {
        arrowLine.ActiveArrowLine(false);
        if (arrowLine.lr) arrowLine.lr.gameObject.SetActive(false);
        enabled = false;             // ← снова выключаемся
    }
}

   public void TutorialEnd()
    {
        if (IsTutorialCompleted) return;

        IsTutorialCompleted = true;

        if (TutorialPanel) TutorialPanel.SetActive(false);
        arrowLine.ActiveArrowLine(false);
        if (arrowLine.lr) arrowLine.lr.gameObject.SetActive(false);

        YG.YG2.saves.tutorialCompleted = true;
        SaveBridge.Save();

        enabled = false;                 // ← главное: отключить компонент, чтобы он больше не включал линию
    }


    private bool IsValid(BrainrotController bc)
    {
        if (!bc) return false;
        if (!bc.gameObject.activeInHierarchy) return false;
        return bc.MatchesTarget(targetTypeSO);
    }

    private void TryFindTarget()
    {
        var all = FindObjectsOfType<BrainrotController>(false);
        var list = new System.Collections.Generic.List<BrainrotController>();
        foreach (var bc in all)
            if (bc.MatchesTarget(targetTypeSO))
                list.Add(bc);

        currentTarget = list.Count > 0 ? list[Random.Range(0, list.Count)] : null;
    }

    private void ApplyTarget()
    {
        if (IsValid(currentTarget))
            arrowLine.StartArrowLine(startPoint, currentTarget.transform);
        else
            arrowLine.ActiveArrowLine(false);
    }

    public void SetStart(Transform t) => startPoint = t;

    public void SetTargetType(Brainrot so)
    {
        targetTypeSO = so;
        currentTarget = null;
    }
}
