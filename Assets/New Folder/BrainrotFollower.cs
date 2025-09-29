using UnityEngine;
public class BrainrotFollower : MonoBehaviour
{
    public Transform target;
    public float followDistance = 8f;
    public float moveSpeed = 13f;
    public float turnSpeed = 12f;
    public float gravity = -9.8f;

    private CharacterController cc;
    private Animator anim;
    private Vector3 velocity;

    // ⬇️ оффсет лица (yawOffset), чтобы не шёл боком
    private Quaternion faceOffset = Quaternion.identity;

    public void Init(Transform player) => target = player;

    public float maxLeashLength = 6f;            // зададим в инспекторе (копируй из LeashRenderer.maxLength)
public bool hardClampWhenOutOfLeash = true;  // включить «жёсткую телепортацию»


    // ⬇️ новая удобная инициализация: цель + оффсет
    public void Init(Transform player, Quaternion offset)
    {
        target = player;
        faceOffset = offset;
    }

    private void Awake()
    {
        cc = GetComponent<CharacterController>();
        anim = GetComponentInChildren<Animator>();
    }

   private void Update()
{
    if (!target) return;

    // вектор к игроку
    Vector3 to = target.position - transform.position;
    to.y = 0f;                           // игнорим высоту
    float dist = to.magnitude;

    bool needMove = dist > followDistance + 0.1f; // запас

    // нормализованное направление на игрока
    Vector3 dir = (dist > 0.001f) ? to / dist : Vector3.zero;

    // ПОВОРОТ «лицом к цели» с учётом faceOffset (yawOffset модели)
    if (dir.sqrMagnitude > 0.0001f)
    {
        Quaternion look = Quaternion.LookRotation(dir, Vector3.up) * faceOffset;
        transform.rotation = Quaternion.Slerp(transform.rotation, look, turnSpeed * Time.deltaTime);
    }
    
    // Жёсткий телепорт, если ушёл дальше длины поводка
if (hardClampWhenOutOfLeash && dist > maxLeashLength + 0.2f)
{
    Vector3 clampPos = target.position - dir * followDistance;
    clampPos.y = transform.position.y;     // по высоте оставим как есть (или под Raycast на землю)

    // если есть CharacterController — на время выключим, чтобы телепорт прошёл чисто
    bool hadCC = cc != null && cc.enabled;
    if (hadCC) cc.enabled = false;
    transform.position = clampPos;
    if (hadCC) cc.enabled = true;

    velocity = Vector3.zero;
    if (anim) anim.SetBool("Idle", true);
    return;                                 // кадр завершён
}


    // ДВИЖЕНИЕ: идём прямо к игроку, а не вперёд по transform.forward
        Vector3 desiredXZ = needMove ? dir * moveSpeed : Vector3.zero;

    // сгладим текущую горизонтальную скорость к целевой
    Vector3 currentXZ = new Vector3(velocity.x, 0f, velocity.z);
    Vector3 newXZ = Vector3.Lerp(currentXZ, desiredXZ, 10f * Time.deltaTime);
    velocity.x = newXZ.x;
    velocity.z = newXZ.z;

    // гравитация
    if (cc.isGrounded) velocity.y = -0.5f;
    else velocity.y += gravity * Time.deltaTime;

    cc.Move(velocity * Time.deltaTime);

    // анимация
    if (anim)
    {
        bool running = newXZ.magnitude > 0.1f;
        anim.SetBool("Idle", !running);          // бежим → Idle = false
    }
}

}
