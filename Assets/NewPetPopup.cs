// Assets/Scripts/UI/NewPetPopup.cs
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class NewPetPopup : MonoBehaviour
{
    public CanvasGroup cg;
    public Image iconImage;
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI nameText;
    public float fadeIn = 0.2f, fadeOut = 0.15f;

    bool _shown;

    private void Awake()
    {
        if (cg) { cg.alpha = 0; cg.blocksRaycasts = false; }
        gameObject.SetActive(false);
    }

    public void Show(Sprite icon, string petName)
    {
        Debug.Log($"[NEWPET] Popup.Show: name={petName} icon={(icon? "OK":"NULL")} cg={(cg? "OK":"NULL")}");
        gameObject.SetActive(true);
        if (iconImage) iconImage.sprite = icon;
        if (titleText) titleText.text = "NEW PET!";
        if (nameText)  nameText.text  = petName;
        _shown = true;
        StartCoroutine(FadeTo(1f, fadeIn));
    }

    public void Hide()
    {
        
        if (!_shown) return;
        _shown = false;
        StartCoroutine(FadeTo(0f, fadeOut, () => gameObject.SetActive(false)));
    }

    private void Update()
    {
        if (_shown && (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1)))
            Hide();
    }

    System.Collections.IEnumerator FadeTo(float a, float t, System.Action onDone = null)
    {
        if (!cg) { onDone?.Invoke(); yield break; }
        float s = cg.alpha, time = 0f;
        cg.blocksRaycasts = a > 0.5f;
        while (time < t)
        {
            time += Time.unscaledDeltaTime;
            cg.alpha = Mathf.Lerp(s, a, time / t);
            yield return null;
        }
        cg.alpha = a;
        cg.blocksRaycasts = a > 0.5f;
        onDone?.Invoke();
    }
}
