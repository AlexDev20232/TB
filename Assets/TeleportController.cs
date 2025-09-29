// Assets/Scripts/Utils/TeleportController.cs
using UnityEngine;
using UnityEngine.AI;

public class TeleportController : MonoBehaviour
{
   [Header("Кого телепортируем")]
    [Tooltip("Корневой объект игрока (тот, у кого Transform двигаем).")]
    public Transform playerRoot;

    [Header("Цели телепорта")]
    [Tooltip("Точка продажи брайротов (пустой объект на сцене).")]
    public Transform sellPoint;
    [Tooltip("Точка 'Моя база' (пустой объект на сцене).")]
    public Transform basePoint;

    [Header("Опции")]
    [Tooltip("Копировать ли поворот от точки назначения.")]
    public bool copyRotation = true;
    [Tooltip("Доп. смещение по высоте при телепорте.")]
    public float yOffset = 0f;

    [Tooltip("Если у игрока NavMeshAgent — использовать Warp().")]
    public bool useNavMeshAgent = true;
    [Tooltip("Если у игрока CharacterController — временно отключать при телепорте.")]
    public bool useCharacterController = true;

    NavMeshAgent _agent;
    CharacterController _cc;

    void Awake()
    {
        if (!playerRoot) playerRoot = transform;
        _agent = playerRoot.GetComponent<NavMeshAgent>();
        _cc    = playerRoot.GetComponent<CharacterController>();
    }

    // ───── Публичные методы для кнопок UI ─────
    public void TeleportToSell()
    {
        TeleportTo(sellPoint);
    }
    public void TeleportToBase()
    {
        TeleportTo(basePoint);
    }

    // ───── Внутренняя логика телепорта ─────
    void TeleportTo(Transform target)
    {
        if (!playerRoot || !target)
        {
            Debug.LogWarning("[Teleport] Не задан playerRoot или target.");
            return;
        }

        // Формируем позицию/поворот
        Vector3 dstPos = target.position;
        dstPos.y += yOffset;
        Quaternion dstRot = copyRotation ? target.rotation : playerRoot.rotation;

        // Если есть NavMeshAgent и разрешено — Warp
        if (useNavMeshAgent && _agent && _agent.enabled)
        {
            _agent.ResetPath();
            _agent.Warp(dstPos);
            playerRoot.rotation = dstRot;
            return;
        }

        // Если есть CharacterController — безопасно отключаем/включаем
        if (useCharacterController && _cc && _cc.enabled)
        {
            _cc.enabled = false;
            playerRoot.SetPositionAndRotation(dstPos, dstRot);
            _cc.enabled = true;
            return;
        }

        // Иначе просто двигаем трансформ
        playerRoot.SetPositionAndRotation(dstPos, dstRot);
    }
}
