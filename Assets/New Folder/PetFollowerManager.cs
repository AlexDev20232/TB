// Assets/Scripts/Pets/PetFollowerManager.cs
using UnityEngine;

public class PetFollowerManager : MonoBehaviour
{
    public static PetFollowerManager Instance { get; private set; }

    [Header("Игрок и поводок")]
    public Transform player;       // корень игрока (обязателен)
    public Transform leashStart;   // пустышка на руке игрока (обязательна)

    [Header("Спавн")]
    public Vector3 spawnOffset = new Vector3(-1.5f, 0f, -1.5f);

    private GameObject _current;

    private void Awake()
    {
        if (Instance && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    /// <summary>Спавн питомца из слота (EggStack с брайротом).</summary>
    public void SpawnFromStack(EggStack s)
    {
        if (s == null || !s.IsBrainrot)
        {
            Debug.LogWarning("[PetFollower] Slot is null or not a brainrot");
            return;
        }
        if (!player)
        {
            Debug.LogError("[PetFollower] player is NULL — назначь Transform игрока в инспекторе");
            return;
        }
        if (!leashStart)
        {
            Debug.LogError("[PetFollower] leashStart is NULL — назначь пустышку на руке игрока");
            return;
        }

        // убрать предыдущего питомца
        Despawn();

        var so = s.brainrot;
        if (!so || !so.characterPrefab)
        {
            Debug.LogError("[PetFollower] Brainrot SO или characterPrefab пустой");
            return;
        }

        // 1) инстансим бота
        _current = Instantiate(so.characterPrefab, player.position + spawnOffset, Quaternion.identity);



        // 2) масштаб по весу
        float scale = so.baseScale * (1f + s.weightKg * so.scalePerKg);
        _current.transform.localScale = Vector3.one * Mathf.Max(0.01f, scale);

        // 3) развернуть лицом к игроку (учёт yawOffset)
        var mv = _current.GetComponent<BrainrotMover>();
        Quaternion look = Quaternion.identity;
        Vector3 toPlayer = player.position - _current.transform.position;
        toPlayer.y = 0f;
        if (toPlayer.sqrMagnitude > 0.0001f)
            look = Quaternion.LookRotation(toPlayer.normalized, Vector3.up);
        if (mv) look *= mv.GetOffsetQuat();
        _current.transform.rotation = look;

        // 4) отключить «базовую» логику на базе (если есть)
        var bc = _current.GetComponent<BrainrotController>();
        if (bc) bc.enabled = false;

        // 5) следование
        var cc = _current.GetComponent<CharacterController>();
        if (!cc) cc = _current.AddComponent<CharacterController>(); // чтобы BrainrotFollower не упал
        // подстройка контроллера (простая)
        cc.center = new Vector3(0, 0.9f, 0);
        cc.radius = 0.4f;
        cc.height = 1.8f;

        var follower = _current.GetComponent<BrainrotFollower>();
        if (!follower) follower = _current.AddComponent<BrainrotFollower>();
        follower.followDistance = 5f;   // дальше от игрока
        follower.moveSpeed = 7.0f;   // быстрее
        follower.turnSpeed = 12f;
        follower.Init(player, mv ? mv.GetOffsetQuat() : Quaternion.identity);

        // 6) поводок (LineRenderer)
        var leash = _current.GetComponent<LeashRenderer>();
        if (!leash) leash = _current.AddComponent<LeashRenderer>();
        leash.startPoint = leashStart;



  // привязка не к корню, а к «ошейнику»
        Transform endAnchor = _current.transform.Find("LeashEnd");
        if (!endAnchor)
        {
            endAnchor = new GameObject("LeashEnd").transform;
            endAnchor.SetParent(_current.transform, false);
            endAnchor.localPosition = new Vector3(0f, 0.9f, 0.1f);
        }
        leash.endPoint = endAnchor;


        leash.segments = 32;
        leash.startWidth = 0.06f;
        leash.endWidth = 0.04f;
        leash.sagAmount = 0.35f;
        leash.maxLength = 50f;
        follower.maxLeashLength = 50f;              // тот же, что и leash.maxLength
follower.hardClampWhenOutOfLeash = true;

        
    }

    public void Despawn()
    {
        if (_current)
        {
            Destroy(_current);
            _current = null;
        }
    }
}
