using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class LeashRenderer : MonoBehaviour
{
    public Transform startPoint;       // рука игрока
    public Transform endPoint;         // центр брайрота

    [Range(2,64)] public int segments = 32;
    public float startWidth = 0.8f;
    public float endWidth   = 0.6f;

    [Range(0f,1f)] public float sagAmount = 0.35f; // провис
    public Vector3 sagDirection = Vector3.down;

    public float maxLength = 20f;

    private LineRenderer lr;
    private Vector3[] pts;

   public Color lineColor = new Color32(165, 42, 42, 255);



    void Awake()
    {
        lr = GetComponent<LineRenderer>();
        lr.useWorldSpace = true;
        lr.startWidth = startWidth;
        lr.endWidth   = endWidth;
        EnsureBuffer();
        EnsureMaterial();
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        // если меняешь значения в инспекторе — обновим буфер
        if (segments < 2) segments = 2;
        EnsureBuffer();
        if (lr == null) lr = GetComponent<LineRenderer>();
        if (lr != null) { lr.startWidth = startWidth; lr.endWidth = endWidth; }
    }
#endif

    void LateUpdate()
    {
        if (!startPoint || !endPoint) { if (lr) lr.enabled = false; return; }
        if (!lr) lr = GetComponent<LineRenderer>();
        lr.enabled = true;

        // страховки
        if (segments < 2) segments = 2;
        EnsureBuffer();

        Vector3 a = startPoint.position;
        Vector3 b = endPoint.position;

        // ограничение длины
        float dist = Vector3.Distance(a, b);
        if (dist > maxLength)
        {
            Vector3 dir = (b - a).normalized;
            b = a + dir * maxLength;
        }

        BuildSagCurve(a, b);

        lr.positionCount = pts.Length;
        lr.SetPositions(pts);
    }

    private void BuildSagCurve(Vector3 a, Vector3 b)
{
    int n = pts.Length;
    if (n < 2) return;

    Vector3 mid = (a + b) * 0.5f;
    float len = Vector3.Distance(a, b);

    // масштаб провиса: 0 при нулевой длине, 1 при maxLength (не выше)
    float sagScale = Mathf.Clamp01(len / Mathf.Max(0.0001f, maxLength));
    // мягче: 0.25 вместо 0.5, и умножаем на sagScale
    Vector3 c = mid + sagDirection.normalized * (sagAmount * sagScale * len * 0.25f);

    for (int i = 0; i < n; i++)
    {
        float t = (float)i / (n - 1);
        Vector3 p1 = Vector3.Lerp(a, c, t);
        Vector3 p2 = Vector3.Lerp(c, b, t);
        pts[i] = Vector3.Lerp(p1, p2, t);
    }
}


    private void EnsureBuffer()
    {
        if (segments < 2) segments = 2;
        if (pts == null || pts.Length != segments)
            pts = new Vector3[segments];
        if (lr != null && lr.positionCount != segments)
            lr.positionCount = segments;
    }

   void EnsureMaterial()
{
    if (lr.sharedMaterial == null)
        lr.sharedMaterial = new Material(Shader.Find("Sprites/Default"));
    lr.sharedMaterial.color = lineColor;         // ← цвет
    lr.startColor = lr.endColor = lineColor;
}

}
