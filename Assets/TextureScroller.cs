using UnityEngine;

/// <summary>
/// Прокручивает текстуру по UV. Текстура должна быть с WrapMode = Repeat.
/// Даёт публичные свойства Direction и SpeedAbs для синхронизации физики (конвейера) с визуалом.
/// Повесь на объект с Renderer, у которого материал содержит _MainTex (Built-in) или _BaseMap (URP).
/// </summary>
[RequireComponent(typeof(Renderer))]
public class TextureScroller : MonoBehaviour
{
    [Tooltip("Скорость прокрутки по X (UV). Положительное значение — вправо.")]
    public float scrollSpeedX = 0f;

    [Tooltip("Скорость прокрутки по Y (UV). Положительное — вверх, отрицательное — вниз.")]
    public float scrollSpeedY = -0.5f;

    [Tooltip("Сколько раз повторить текстуру по X/Y (тайлинг).")]
    public Vector2 tiling = new Vector2(1f, 12f);

    [Tooltip("Создавать инстанс материала, чтобы не портить sharedMaterial.")]
    public bool instantiateMaterial = true;

    // Публичные свойства для других скриптов
    public Vector2 Direction => new Vector2(scrollSpeedX, scrollSpeedY); // UV-направление
    public float SpeedAbs => Mathf.Max(Mathf.Abs(scrollSpeedX), Mathf.Abs(scrollSpeedY)); // модуль «скорости»

    private Renderer _rend;
    private Material _mat;
    private string _texProp = "_MainTex"; // Built-in; в URP будет "_BaseMap"
    private Vector2 _offset;

    void Awake()
    {
        _rend = GetComponent<Renderer>();
        _mat  = instantiateMaterial ? _rend.material : _rend.sharedMaterial;

        if (_mat.HasProperty("_BaseMap")) _texProp = "_BaseMap"; // URP
        else _texProp = "_MainTex";                              // Built-in

        if (_mat.HasProperty(_texProp))
        {
            _mat.SetTextureScale(_texProp, tiling);

            var tex = _mat.GetTexture(_texProp);
            if (tex && tex.wrapMode != TextureWrapMode.Repeat)
                Debug.LogWarning($"{name}: у текстуры '{tex.name}' WrapMode = {tex.wrapMode}. Поставь Repeat для бесшовной прокрутки.");
        }
        else
        {
            Debug.LogWarning($"{name}: материал не содержит {_texProp}. Используй Unlit/Texture (Built-in) или URP/Unlit.");
        }
    }

    void Update()
    {
        if (_mat == null || !_mat.HasProperty(_texProp)) return;

        _offset.x = Mathf.Repeat(_offset.x + scrollSpeedX * Time.unscaledDeltaTime, 1f);
        _offset.y = Mathf.Repeat(_offset.y + scrollSpeedY * Time.unscaledDeltaTime, 1f);
        _mat.SetTextureOffset(_texProp, _offset);
    }

    // По желанию: изменить параметры на лету
    public void SetTiling(Vector2 newTiling)
    {
        tiling = newTiling;
        if (_mat != null && _mat.HasProperty(_texProp))
            _mat.SetTextureScale(_texProp, tiling);
    }
}
