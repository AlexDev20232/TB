using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class ArrowBeamNoShader : MonoBehaviour
{
    [Header("Источник/игрок")]
    [SerializeField] Transform source;

    [Header("Кого искать")]
    [SerializeField] Brainrot targetType;    // сюда в инспекторе кидаешь нужный ScriptableObject

    [Header("Визуал")]
    [SerializeField] Material arrowMat;      // материал со спрайтом стрелок (Unlit/Transparent)
    [SerializeField] float width = 0.2f;
    [SerializeField] float tilingPerUnit = 1f;
    [SerializeField] float scrollSpeed = 2f;

    [Header("Поиск")]
    [SerializeField] float searchInterval = 0.5f;

    LineRenderer lr;
    BrainrotController current;
    float searchTimer;
    float scroll;
    static readonly int MainTexST = Shader.PropertyToID("_MainTex_ST");

    void Awake()
    {
        lr = GetComponent<LineRenderer>();
        lr.positionCount = 2;
        lr.widthMultiplier = width;
        lr.material = arrowMat;
        lr.textureMode = LineTextureMode.Tile;
    }

    void LateUpdate()
    {
        if (!source)
        {
            lr.enabled = false;
            return;
        }

        if (!IsValid(current))
        {
            lr.enabled = false;
            searchTimer += Time.deltaTime;
            if (searchTimer >= searchInterval)
            {
                searchTimer = 0f;
                current = FindRandomTarget();
            }
            return;
        }

        lr.enabled = true;
        lr.SetPosition(0, source.position);
        lr.SetPosition(1, current.transform.position);

        float dist = Vector3.Distance(source.position, current.transform.position);
        Vector4 st = lr.material.GetVector(MainTexST);
        st.x = dist * tilingPerUnit;
        lr.material.SetVector(MainTexST, st);

        scroll += Time.deltaTime * scrollSpeed;
        var off = lr.material.mainTextureOffset;
        off.x = scroll;
        lr.material.mainTextureOffset = off;
    }

    bool IsValid(BrainrotController bc)
    {
        if (!bc) return false;
        if (!bc.gameObject.activeInHierarchy) return false;
        return bc.MatchesTarget(targetType);
    }

    BrainrotController FindRandomTarget()
    {
        BrainrotController[] all = FindObjectsOfType<BrainrotController>(false);
        var list = new System.Collections.Generic.List<BrainrotController>();
        foreach (var bc in all)
            if (bc.MatchesTarget(targetType))
                list.Add(bc);

        if (list.Count == 0) return null;
        return list[Random.Range(0, list.Count)];
    }

    public void SetSource(Transform t) => source = t;
}
