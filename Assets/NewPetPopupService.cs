// Assets/Scripts/UI/NewPetPopupService.cs
using UnityEngine;

public class NewPetPopupService : MonoBehaviour
{
    public static NewPetPopupService I { get; private set; }

    [Header("Prefabs & Parents")]
    public NewPetPopup popupPrefab;
    public Transform uiParent;

    private NewPetPopup _active;

    private void Awake()
    {
        if (I && I != this) { Destroy(gameObject); return; }
        I = this;
        DontDestroyOnLoad(gameObject);
    }

    /// <summary>
    /// Показать поп-ап. Если уже открыт старый — УДАЛЯЕМ его и создаём новый.
    /// </summary>
   public void ShowReplacing(Sprite icon, string petName)
{
    if (!popupPrefab)
    {
        Debug.LogWarning("[NEWPET] Service: popupPrefab is NULL");
        return;
    }

    if (_active)
    {
        Debug.Log("[NEWPET] Service: destroying previous popup");
        Destroy(_active.gameObject);
        _active = null;
    }

    Transform parent = uiParent;
    if (!parent)
    {
        var canvas = FindObjectOfType<Canvas>();
        parent = canvas ? canvas.transform : null;
        Debug.Log($"[NEWPET] Service: parent={(parent ? parent.name : "NULL")}");
    }

    _active = Instantiate(popupPrefab, parent);
    Debug.Log($"[NEWPET] Service: popup instantiated = {_active.name}");
    _active.Show(icon, petName);
}


    /// <summary>Спрятать активный поп-ап (если нужен где-то).</summary>
    public void HideActive()
    {
        if (_active)
        {
            _active.Hide();
            // если хочешь прям удалять: Destroy(_active.gameObject); _active = null;
        }
    }
}
