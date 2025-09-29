using UnityEngine;

public class BrainrotController : MonoBehaviour
{
    public Brainrot stats; // Параметры из ScriptableObject
    [SerializeField] private GameObject incomeDisplayPrefab; // Префаб UI дохода
    
    public bool IsPurchased { get; private set; }
     public bool IsBought { get; private set; }


    public bool MatchesTarget(Brainrot targetSo) => stats == targetSo;


    // Инициализация при спавне
    public void Init(Brainrot parameters)
    {
        stats = parameters;
    }

    // Вызывается, когда бот занял слот
  // BrainrotController.cs
public void OnReachedPosition(point slot)
{
    IsPurchased = true;
    SpawnIncomeDisplay(slot.incomePointTrans);

    // ↓↓↓  ДОБАВЬТЕ ЭТИ СТРОКИ  ↓↓↓
    var mover = GetComponent<BrainrotMover>();
    if (mover) mover.currentState = BrainrotMover.MoveState.Positioning;

    var anim = GetComponentInChildren<Animator>();
    anim?.SetBool("Idle", true);     // включаем стоячую анимацию
}


 public void MarkBought()
    {
        IsBought = true;
    }

    private void SpawnIncomeDisplay(Transform posToSpawn)
    {
        float dist = 2.5f;
        Vector3 offset = transform.up * dist;

        GameObject display = Instantiate(
            incomeDisplayPrefab,
            posToSpawn.position + offset,
            Quaternion.identity,
            posToSpawn
        );

        display.GetComponent<IncomeDisplay>().Init(stats.incomePerSecond);
    }

    public Brainrot GetStats() => stats;
}