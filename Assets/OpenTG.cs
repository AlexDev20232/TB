using UnityEngine;

public class OpenTG : MonoBehaviour
{
    // ссылка на твой канал
    [SerializeField] private string tgLink = "https://t.me/yourchannel";

    // вызывать этот метод при клике на кнопку (через OnClick в инспекторе)
    public void OpenChannel()
    {
        Application.OpenURL(tgLink);
        Debug.Log("Открыта ссылка: " + tgLink);
    }
}
