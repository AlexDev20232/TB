// Assets/Scripts/Input/EscCursorToggler.cs
using UnityEngine;
using Invector.vCharacterController;

public class EscCursorToggler : MonoBehaviour
{
    [Tooltip("Ссылка на компонент vThirdPersonInput персонажа")]
    [SerializeField] private vThirdPersonInput tpi;

    void Reset()                                   // автоматическая привязка при добавлении
    {
        if (!tpi) tpi = GetComponent<vThirdPersonInput>();
    }

    void Update()
    {
        if (!tpi || Time.timeScale == 0f) return;  // игра на паузе – игнорируем

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            bool cursorLocked = Cursor.lockState == CursorLockMode.Locked;

            if (cursorLocked)
            {
                // ───── переходим в «меню» ─────
                Cursor.visible  = true;
                Cursor.lockState = CursorLockMode.None;

                tpi.SetLockAllInput(true);          // блок движения/действий
                tpi.SetLockCameraInput(true);       // блок вращения камеры
            }
            else
            {
                // ───── возвращаемся в игру ─────
                Cursor.visible  = false;
                Cursor.lockState = CursorLockMode.Locked;

                tpi.SetLockAllInput(false);
                tpi.SetLockCameraInput(false);
            }
        }
    }
}
