using UnityEngine;


public class ArrowLine : MonoBehaviour
{
    [Header("Скорость прокрутки текстуры по X")]
    [SerializeField] private float textureScrollSpeed = 1f;

    [Header("Фиксированный Tiling по X (не меняем во время игры)")]
    [SerializeField] private float fixedTilingX = 5f;
    public float arrowWidth = 1f;
    [SerializeField] private float lineWidth = 0.2f;

     public LineRenderer lr;
    private Material lineMat;
    private bool isActive;

    private Transform startT;
    private Transform endT;

    void Awake()
    {
        
       // lr.widthMultiplier = lineWidth;
      //  lr.textureMode = LineTextureMode.Tile;

        // Делаем инстанс материала
        lr.material = new Material(lr.material);
        lineMat = lr.material;
        

        // Задаём постоянный tiling один раз
        Vector2 scale = lineMat.mainTextureScale;
       // scale.x = fixedTilingX;
        lineMat.mainTextureScale = scale;

        ActiveArrowLine(false);
    }
public void ForceOff()
{
    ActiveArrowLine(false);
    if (lr) lr.gameObject.SetActive(false);
}
   void Update()
{
    if (!isActive) return;

    if (startT && endT)
    {
        lr.SetPosition(0, GetCenter(startT));
        lr.SetPosition(1, GetCenter(endT));

        float dist = Vector3.Distance(GetCenter(startT), GetCenter(endT));
        Vector2 scale = lineMat.mainTextureScale;
        scale.x = dist / arrowWidth;
        lineMat.mainTextureScale = scale;
    }
    else
    {
        ActiveArrowLine(false);
        return;
    }

    Vector2 off = lineMat.mainTextureOffset;
    off.x = (off.x - textureScrollSpeed * Time.deltaTime) % 1f;
    lineMat.mainTextureOffset = off;
}

static Vector3 GetCenter(Transform t)
{
    // 1) Renderer
    var rend = t.GetComponentInChildren<Renderer>();
    if (rend) return rend.bounds.center;

    // 2) Collider
    var col = t.GetComponentInChildren<Collider>();
    if (col) return col.bounds.center;

#if UNITY_2D
    var col2d = t.GetComponentInChildren<Collider2D>();
    if (col2d) return col2d.bounds.center;
    var sr = t.GetComponentInChildren<SpriteRenderer>();
    if (sr) return sr.bounds.center;
#endif

    return t.position;
}


    public void StartArrowLine(Transform start, Transform end)
    {
        startT = start;
        endT   = end;
        ActiveArrowLine(start && end);
    }

    public void ActiveArrowLine(bool v)
    {
        
        isActive = v;
        lr.enabled = v;
    }
}
