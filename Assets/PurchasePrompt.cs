using TMPro;
using UnityEngine;

public class PurchasePrompt : MonoBehaviour
{
    [Header("Настройки размеров")]
    public float minSize     = 0.5f;
    public float maxSize     = 2.0f;
    public float maxDistance = 15f;

    [Tooltip("Скорость плавного поворота (0 — мгновенно)")]
    public float rotationSpeed = 0f;

    [Header("UI")]
    public TextMeshProUGUI actionText;  // Buy / Sell
    public TextMeshProUGUI nameText;

    private Transform _cam;
    private Vector3   _initialScale;
    private Vector3   _localPos;

    // ──────────────────────────────────────────────────────────────────────
    private void Start()
    {
        _cam           = Camera.main.transform;
        _initialScale  = transform.localScale;
        _localPos      = transform.localPosition;
        SnapToCamera();
    }

    // ---------- публичные API -------------------------------------------
    public void SetPositioningMode(float sellPrice)
        => actionText.text = $"Продать: {FormatMoney(sellPrice)}";

    public void SetWalkingMode(float buyPrice)
        => actionText.text = $"Купить: {FormatMoney(buyPrice)}";

    public void SetName(string n) => nameText.text = n;

    // ---------- форматирование денег ------------------------------------
    private static string FormatMoney(float value)
    {
        if (value >= 1_000_000f)
            return (value / 1_000_000f).ToString("0.#") + " M$";
        if (value >= 1_000f)
            return (value / 1_000f).ToString("0.#") + " K$";
        return Mathf.RoundToInt(value) + "$";
    }

    // ---------- update ---------------------------------------------------
    private void Update()
    {
        if (transform.parent)                // позиция — жёстко за родителем
            transform.position = transform.parent.TransformPoint(_localPos);

        UpdateOrientation();
        UpdateScale();
    }

    private void UpdateOrientation()
    {
        Quaternion target = Quaternion.LookRotation(transform.position - _cam.position);

        transform.rotation = rotationSpeed <= 0
            ? target
            : Quaternion.Slerp(transform.rotation, target, rotationSpeed * Time.deltaTime);
    }

    private void SnapToCamera() => transform.rotation =
        Quaternion.LookRotation(transform.position - _cam.position);

    private void UpdateScale()
    {
        float dist  = Vector3.Distance(transform.position, _cam.position);
        float t     = Mathf.Clamp01(dist / maxDistance);
        float scale = Mathf.Lerp(minSize, maxSize, t);

        transform.localScale = _initialScale * scale;
    }

    // ---------- debug gizmo ---------------------------------------------
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, 0.1f);
        Gizmos.DrawLine(transform.position,
                        transform.position + transform.forward * 0.5f);
    }
}
    