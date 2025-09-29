// ──────────────────────────────────────────────────────────────
// EscCursorUnlocker.cs   (добавьте в любую сцену, один экземпляр)
// ──────────────────────────────────────────────────────────────
using UnityEngine;



public class EscCursorUnlocker : MonoBehaviour
{
    // чтобы гарантировать один экземпляр
    private static EscCursorUnlocker _instance;
    void Awake()
    {
        if (_instance && _instance != this) { Destroy(gameObject); return; }
        _instance = this;
        DontDestroyOnLoad(gameObject);

        // курсор всегда виден и не заблокирован по умолчанию
        UnlockCursor();
    }

    void Update()
    {
        // Esc → возврат курсора
        if (Input.GetKeyDown(KeyCode.Escape))
            UnlockCursor();

        // Правая кнопка мыши → вращаем камеру, но курсор всё равно остаётся видимым
        // Left / Right Button – никакой блокировки не делаем
    }

    public static void UnlockCursor()
    {
        Cursor.visible   = true;
        Cursor.lockState = CursorLockMode.None;
    }
}
