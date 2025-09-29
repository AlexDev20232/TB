using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Показывает курсор, если нажали ESC или курсор над UI; прячет, если не над UI.
/// Работает с Invector (или любым контроллером), т.к. напрямую управляет Cursor.
/// </summary>
public class CursorLockerInvector : MonoBehaviour
{
    [SerializeField] KeyCode toggleKey = KeyCode.Escape;

    bool manualFree;   // Режим «свободной мыши» после ESC

    void Update()
    {
        if (Input.GetKeyDown(toggleKey))
            manualFree = !manualFree;

        if (manualFree)
        {
            // При свободном режиме: если над UI — показываем, иначе прячем
            if (IsPointerOverUI())
                ShowCursor();
            else
                HideCursor();
        }
        else
        {
            // Обычный режим: если не над UI — прячем
            if (IsPointerOverUI())
                ShowCursor();
            else
                HideCursor();
        }
    }

    bool IsPointerOverUI()
    {
        if (EventSystem.current == null) return false;

        var ped = new PointerEventData(EventSystem.current)
        {
            position = Input.mousePosition
        };
        var results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(ped, results);
        return results.Count > 0;
    }

    void ShowCursor()
    {
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    void HideCursor()
    {
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }
}
