using UnityEngine;

/// <summary>
/// Вешай на объект-финиш (нужен Collider с галкой IsTrigger).
/// Когда в триггер входит объект с BrainrotMover — он удаляется.
/// </summary>
[RequireComponent(typeof(Collider))]
public class Finish : MonoBehaviour
{
    public static Finish Instance { get; private set; }

    [Tooltip("Удалять объект сразу при входе в триггер? Если нет — используем проверку дистанции.")]
    [SerializeField] private bool deleteOnTrigger = true;

    [Tooltip("Доп. радиус проверки (если нужно удалять только при очень близком подходе).")]
    [SerializeField] private float extraDistance = 0.1f;

    private void Awake()
    {
        Instance = this;
        // Убедимся, что коллайдер — триггер
        var col = GetComponent<Collider>();
        if (col) col.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!deleteOnTrigger) return;
        TryDelete(other);
    }

    private void OnTriggerStay(Collider other)
    {
        if (deleteOnTrigger) return;

        // Если нужен «буквально вошёл» — можно проверить дистанцию центров
        if (Vector3.Distance(other.bounds.center, GetComponent<Collider>().bounds.center) <= extraDistance)
            TryDelete(other);
    }

    private void TryDelete(Collider other)
    {
        if (!other) return;
        if (other.GetComponent<BrainrotMover>() != null)
        {
            Destroy(other.gameObject);
        }
    }
}
