/*  ErrorMessagePopup.cs
 *  Автор: вы
 *  Поместите на объект‑контейнер сообщения (UI‑панель, 3D‑текст и т.п.)
 *  При gameObject.SetActive(true) он проиграет анимацию появления,
 *  подержится N секунд и сам выключится.
 */

using UnityEngine;

[RequireComponent(typeof(Transform))]
public class ErrorMessagePopup : MonoBehaviour
{
    [Header("Анимация появления")]
    [Tooltip("С какого масштаба начинать (0 = из точки)")]
    public Vector3 startScale = Vector3.zero;

    [Tooltip("За сколько секунд раскрыться до нормального размера")]
    public float scaleTime = 0.25f;

    [Header("Отображение")]
    [Tooltip("Сколько секунд держать сообщение, прежде чем выключить объект")]
    public float holdTime = 2f;

    [Tooltip("Использовать независимое от Time.timeScale время (для паузы)")]
    public bool unscaledTime = true;

    // исходный масштаб (считываем при Awake)
    private Vector3 _originalScale;

    private void Awake()
    {
        _originalScale = transform.localScale;
    }

    private void OnEnable()
    {
        // каждый раз при включении запускаем корутину
        StopAllCoroutines();
        StartCoroutine(ShowRoutine());
    }

    /// <summary>Основная корутина: scale‑in → hold → deactivate.</summary>
    private System.Collections.IEnumerator ShowRoutine()
    {
        // 1. ставим стартовый масштаб
        transform.localScale = startScale;

        // 2. плавно раскручиваемся до оригинала
        float t = 0f;
        while (t < 1f)
        {
            t += (unscaledTime ? Time.unscaledDeltaTime : Time.deltaTime) / Mathf.Max(0.0001f, scaleTime);
            float k = Mathf.SmoothStep(0, 1, t);           // сглаженная кривая
            transform.localScale = Vector3.LerpUnclamped(startScale, _originalScale, k);
            yield return null;
        }
        transform.localScale = _originalScale;             // гарантируем точное значение

        // 3. ждём holdTime секунд
        if (holdTime > 0f)
            yield return unscaledTime
                ? new WaitForSecondsRealtime(holdTime)
                : new WaitForSeconds(holdTime);

        // 4. выключаем объект
        gameObject.SetActive(false);
    }

    // Метод «вручной перезапуск», если вдруг понадобится
    public void ShowAgain()
    {
        gameObject.SetActive(false);   // сброс
        gameObject.SetActive(true);    // триггер анимации OnEnable
    }
}
