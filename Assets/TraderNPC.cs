using UnityEngine;

/// <summary>
/// Простой НПС: идёт к цели, поворачивается, один раз отдаёт OnArrivedToTarget.
/// </summary>
[RequireComponent(typeof(Animator))]
public class TraderNPC : MonoBehaviour
{
    [Header("Движение")]
    public float moveSpeed    = 7f;
    public float stopDistance = 0.35f;
    public float turnSlerp    = 10f;

    [Header("Анимация (опц.)")]
    public string runBoolName = "Run";
    //public string idleTrigger = "Idle";

    public System.Action OnArrivedToTarget;

    private Vector3 _target;
    private bool _hasTarget;
    private bool _arrivedInvoked;
    private Animator _anim;

    public bool HasArrived { get; private set; }

    void Awake() => _anim = GetComponentInChildren<Animator>();

    public void SetTarget(Vector3 pos)
    {
        _target = pos;
        _hasTarget = true;
        HasArrived = false;
        _arrivedInvoked = false;
    }
    public void SetTarget(Transform t) => SetTarget(t ? t.position : transform.position);

    void Update()
    {
        if (!_hasTarget) return;

        Vector3 to = _target - transform.position;
        to.y = 0f;
        float sqr = to.sqrMagnitude;
        float stopSqr = stopDistance * stopDistance;

        //  if (_anim && !string.IsNullOrEmpty(runBoolName))
        //    _anim.SetBool(runBoolName, sqr > stopSqr + 0.01f);
        

        if (sqr > stopSqr)
        {
            if (sqr > 0.0001f)
            {
                var look = Quaternion.LookRotation(to.normalized, Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, look, turnSlerp * Time.deltaTime);
            }
            transform.position += to.normalized * moveSpeed * Time.deltaTime;
        }
        else
        {
            transform.position = new Vector3(_target.x, transform.position.y, _target.z);
            _hasTarget = false;
            HasArrived = true;

           // if (_anim && !string.IsNullOrEmpty(idleTrigger))
                //_anim.SetTrigger(idleTrigger);
          //  if (_anim && !string.IsNullOrEmpty(runBoolName))
                _anim.SetBool(runBoolName, false);

            if (!_arrivedInvoked)
            {
                _arrivedInvoked = true;
                OnArrivedToTarget?.Invoke();
            }
        }
    }
}
