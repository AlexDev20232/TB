// Скрипт уже содержит всю старую логику + учёт угла «лица» модели.
// Замени свой BrainrotMover этим файлом.
// Комментарии максимально подробные (на русском).

using UnityEngine;

public class BrainrotMover : MonoBehaviour
{
    // ───── НАСТРОЙКИ ДВИЖЕНИЯ ──────────────────────────────────────────────
    [Header("Скорости")]
    [SerializeField] private float baseMoveSpeed   = 7f;  // обычное хождение
    [SerializeField] private float targetMoveSpeed = 10f; // бег к базе

    [Header("Поворот")]
    [Tooltip("На сколько градусов модель повернута вокруг Y относительно +Z.\n" +
             "  0   – лицо смотрит по +Z (вперёд)\n" +
             " -90  – лицо смотрит вправо  (+X)\n" +
             "  90  – лицо смотрит влево   (-X)\n" +
             " 180  – лицо смотрит назад   (-Z)")]
    [SerializeField] private float yawOffset = -90f;      // ← меняйте в Инспекторе

    [Header("Прочее")]
    [SerializeField] private LayerMask groundLayer;       // для расчёта смещения до земли

    // ───── ВНУТРЕННИЕ ПОЛЯ ─────────────────────────────────────────────────
    public enum MoveState { Walking, MovingToBase, Positioning }
    public MoveState currentState = MoveState.Walking;

    private Vector3    targetPosition;
    private float      groundOffset;          // смещение до земли
    private bool       groundOffsetCalculated;
    private Quaternion offsetQuat;            // кватернион с yawOffset

    private BaseController.SlotFloor _floor;
    private int _slotIndex = -1;
    private point _slot;

    // ───── АВТОЗАПУСК ──────────────────────────────────────────────────────
    private void Awake()
    {
        // Предвычисляем поворот – чтобы не делать Euler каждый кадр
        offsetQuat = Quaternion.Euler(0, yawOffset, 0);
    }

public Quaternion GetOffsetQuat() => offsetQuat;
    private void Start()
    {
        CalculateGroundOffset();
    }

    private void Update()
    {
        switch (currentState)
        {
            case MoveState.Walking:
                transform.position += new Vector3(baseMoveSpeed * Time.deltaTime, 0, 0);
                break;

            case MoveState.MovingToBase:
                MoveTowardsBase();
                break;

            case MoveState.Positioning:
                // Стоим на месте
                break;
        }
    }

    // ───── ДВИЖЕНИЕ К БАЗЕ ────────────────────────────────────────────────
    private void MoveTowardsBase()
    {
        transform.position = Vector3.MoveTowards(
            transform.position, targetPosition, targetMoveSpeed * Time.deltaTime);

        // Если на объекте нет собственного скрипта Rotate – поворачиваем вручную
        if (!TryGetComponent<Rotater>(out _))
        {
            Vector3 dir = targetPosition - transform.position;
            dir.y = 0f;                                          // игнорируем высоту
            if (dir.sqrMagnitude > 0.001f)                       // защита от нуля
            {
                Quaternion look = Quaternion.LookRotation(dir) * offsetQuat;
                transform.rotation = Quaternion.Slerp(
                    transform.rotation, look, 5f * Time.deltaTime);
            }
        }
    }

    // ───── ЗАПУСК ДВИЖЕНИЯ ────────────────────────────────────────────────
     public void SetTarget(point reservedSlot, BaseController.SlotFloor floor, int index)
    {
        _slot = reservedSlot;
        _floor = floor;
        _slotIndex = index;

        currentState = MoveState.MovingToBase;
        targetPosition = BaseController.Instance.GetBaseEntrancePosition();
        if (groundOffsetCalculated) targetPosition.y += groundOffset; else targetPosition.y = transform.position.y;
    }

    // ───── ОПРЕДЕЛЯЕМ СМЕЩЕНИЕ ДО ЗЕМЛИ ───────────────────────────────────
    private void CalculateGroundOffset()
    {
        if (Physics.Raycast(transform.position, Vector3.down, out var hit,
                            Mathf.Infinity, groundLayer))
        {
            groundOffset = transform.position.y - hit.point.y;
            groundOffsetCalculated = true;
        }
        else groundOffsetCalculated = false;
    }

    // ───── ФИНАЛЬНЫЙ СНАП НА СЛОТ ─────────────────────────────────────────
private void SnapToFinalPosition()
{
    currentState = MoveState.Positioning;

    if (!BaseController.Instance.TryFindFreeSlotIgnoringReservation(out var floor, out var idx, out var slot))
        return;

    Vector3 finalPos = slot.FreeSlot.position;
    finalPos.y += groundOffsetCalculated ? groundOffset : 0f;

    transform.position = finalPos;
    transform.rotation = slot.FreeSlot.rotation * offsetQuat;
    transform.SetParent(slot.FreeSlot, true);

    if (slot.isReserved)
        BaseController.Instance.ReleaseSlot(floor, idx);

    BaseController.Instance.ConfirmSlotOccupied(floor, idx);

    // если это брайрот
    GetComponent<BrainrotController>()?.OnReachedPosition(slot);
    // если это яйцо
    GetComponent<EggController>()?.OnPlaced(slot);

    GetComponentInChildren<Animator>()?.SetBool("Idle", true);
}


    // ───── КОЛЛИЗИИ ───────────────────────────────────────────────────────
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("BaseTrigger") && currentState == MoveState.MovingToBase)
            SnapToFinalPosition();

        if (other.CompareTag("Finish"))
            Destroy(gameObject);
    }

    // ───── ГИЗМОС ─────────────────────────────────────────────────────────
    private void OnDrawGizmos()
    {
        if (groundOffsetCalculated)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position,
                            transform.position - Vector3.up * groundOffset);
        }
    }
}
