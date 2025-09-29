using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Конвейерная платформа: двигает Rigidbody, стоящие сверху,
/// в заданном направлении с заданной скоростью.
/// Повесь на объект с Collider (isTrigger = false).
/// </summary>
[RequireComponent(typeof(Collider))]
public class ConveyorPlatform : MonoBehaviour
{
    [Header("Скорость конвейера")]
    [Tooltip("Базовая скорость переноса по поверхности (м/с). Если указан TextureScroller — может быть заменена его SpeedAbs.")]
    public float conveyorSpeed = 3f;

    [Header("Направление")]
    [Tooltip("Если true — использовать transform.forward платформы. Иначе берём worldDirection.")]
    public bool usePlatformForward = true;

    [Tooltip("Мировое направление переноса, если usePlatformForward = false.")]
    public Vector3 worldDirection = new Vector3(0f, 0f, 1f);

    [Header("Фильтрация контакта")]
    [Tooltip("Насколько контакт 'сверху'. 0.5 ≈ сверху, 0 ≈ боком.")]
    [Range(0f, 1f)] public float minUpDot = 0.35f;

    [Tooltip("Обнулять вертикаль у направления (рекомендуется).")]
    public bool horizontalOnly = true;

    [Header("Синхронизация со скроллом (опционально)")]
    [Tooltip("Ссылка на TextureScroller для синхронизации скорости с визуалом.")]
    public TextureScroller textureScroller;

    private readonly HashSet<Rigidbody> _touching = new HashSet<Rigidbody>();
    private Collider _coll;

    void Awake()
    {
        _coll = GetComponent<Collider>();
        //if (_coll.isTrigger)
            //Debug.LogWarning($"{name}: коллайдер — Trigger. Для OnCollision* нужен не-триггер.");
    }

    void OnCollisionStay(Collision collision)
    {
        var rb = collision.rigidbody;
        if (rb == null) return;

        bool topContact = false;
        foreach (var cp in collision.contacts)
        {
            float upDot = Vector3.Dot(cp.normal, Vector3.up);
            if (upDot >= minUpDot) { topContact = true; break; }
        }
        if (topContact) _touching.Add(rb);
    }

    void OnCollisionExit(Collision collision)
    {
        var rb = collision.rigidbody;
        if (rb != null) _touching.Remove(rb);
    }

    void FixedUpdate()
    {
        Vector3 dir = usePlatformForward ? transform.forward : worldDirection;
        if (horizontalOnly) dir.y = 0f;
        if (dir.sqrMagnitude < 1e-6f) return;
        dir.Normalize();

        // Базовая скорость
        float speed = conveyorSpeed;

        // Если дан скроллер — берём его модуль скорости (визуал ~= физике).
        if (textureScroller != null)
        {
            // Можешь вместо SpeedAbs использовать явное сопоставление по оси,
            // но для простоты берём максимальную компоненту скорости скролла.
            float s = textureScroller.SpeedAbs;
            if (s > 1e-6f) speed = s;
        }

        Vector3 desiredSurfaceVelocity = dir * speed;

        if (_touching.Count == 0) return;

        foreach (var rb in _touching)
        {
            if (rb == null) continue;

            Vector3 currAlong   = Vector3.Project(rb.velocity, dir);
            Vector3 targetAlong = desiredSurfaceVelocity;
            Vector3 delta       = targetAlong - currAlong;

            rb.AddForce(delta, ForceMode.VelocityChange);
        }

        _touching.Clear();
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Vector3 dir = usePlatformForward ? transform.forward : worldDirection;
        if (horizontalOnly) dir.y = 0f;
        if (dir.sqrMagnitude < 1e-6f) return;
        dir.Normalize();
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.9f);
        Vector3 p = transform.position + Vector3.up * 0.05f;
        Gizmos.DrawLine(p, p + dir * 2f);
        Gizmos.DrawSphere(p + dir * 2f, 0.07f);
    }
#endif
}
