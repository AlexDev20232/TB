using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Очередь из NPC на 3 точки: P2 → P1 → P0 (у стойки).
/// Панель открывается только когда первый NPC реально пришёл.
/// </summary>
public class TradeQueueController : MonoBehaviour
{
    [Header("Точки")]
    public Transform startPoint;
    public List<Transform> queuePoints; // 0=P0 у стойки, 1=P1, 2=P2

    [Header("Префаб NPC")]
    public GameObject npcPrefab;

    [Header("Параметры")]
    public int maxInQueue = 3;
    public Vector2 spawnDelayRange = new Vector2(2f, 4f);

    private readonly List<TraderNPC> _queue = new();
    private float _nextSpawnAt = -1f;

    void Start()
    {
        if (!startPoint) Debug.LogWarning("[TradeQueue] StartPoint не задан");
        if (queuePoints == null || queuePoints.Count < 3) Debug.LogWarning("[TradeQueue] Укажи 3 QueuePoints");
        if (!npcPrefab) Debug.LogWarning("[TradeQueue] npcPrefab не задан");

        _nextSpawnAt = Time.time + Random.Range(spawnDelayRange.x, spawnDelayRange.y);
    }

    void Update()
    {
        for (int i = _queue.Count - 1; i >= 0; i--)
            if (_queue[i] == null) _queue.RemoveAt(i);

        if (_queue.Count < Mathf.Max(1, maxInQueue))
        {
            if (_nextSpawnAt < 0f) _nextSpawnAt = Time.time + Random.Range(spawnDelayRange.x, spawnDelayRange.y);
            if (Time.time >= _nextSpawnAt) { SpawnOne(); _nextSpawnAt = -1f; }
        }

        for (int i = 0; i < _queue.Count; i++)
        {
            var npc = _queue[i];
            if (!npc) continue;

            var p = GetPoint(i);
            if (p) npc.SetTarget(p);

            var ctrl = npc.GetComponent<TraderNPCController>();
            if (i == 0 && ctrl != null)
            {
                npc.OnArrivedToTarget = () =>
                {
                    ctrl.OnFinished = OnNpcFinished;
                    ctrl.OpenOffer();
                };
            }
            else
            {
                npc.OnArrivedToTarget = null;
            }
        }
    }

    void SpawnOne()
    {
        var go = Instantiate(npcPrefab, startPoint.position, startPoint.rotation);
        var npc = go.GetComponent<TraderNPC>();
        if (!npc) npc = go.AddComponent<TraderNPC>();

        int idx = Mathf.Min(_queue.Count, Mathf.Max(0, queuePoints.Count - 1));
        var p = GetPoint(idx);
        if (p) npc.SetTarget(p);

        _queue.Add(npc);
    }

    Transform GetPoint(int index)
    {
        if (queuePoints == null || queuePoints.Count == 0) return null;
        int i = Mathf.Clamp(index, 0, queuePoints.Count - 1);
        return queuePoints[i];
    }

    void OnNpcFinished(TraderNPCController who, string result)
    {
        Debug.Log($"[Queue] NPC finished: {result}");
        FrontLeave();
    }

    public void FrontLeave()
    {
        if (_queue.Count == 0) return;
        var first = _queue[0];
        _queue.RemoveAt(0);
        if (first) Destroy(first.gameObject);
    }
}
