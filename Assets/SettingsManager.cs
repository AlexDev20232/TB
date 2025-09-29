// Assets/Scripts/Settings/SettingsManager.cs
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;
using UnityEngine.Rendering;
using Invector.vCamera;

public class SettingsManager : MonoBehaviour
{
    [Header("Links")]
    [SerializeField] private Slider  musicSlider;
    [SerializeField] private Slider  sfxSlider;
    [SerializeField] private Slider  sensSlider;          // ← НОВЫЙ слайдер
    [SerializeField] private AudioSource    musicSource;
    [SerializeField] private SoundsManager  soundsManager;
    [SerializeField] private vThirdPersonCamera playerCamera; // ссылка на камеру

    private bool _initialised;

    private void Start()
    {
        // ---------- восстановление ----------
        musicSlider.value = Mathf.Clamp01(SaveBridge.Saves.musicVolume);
        sfxSlider.value   = Mathf.Clamp01(SaveBridge.Saves.sfxVolume);
        sensSlider.value  = Mathf.Clamp(SaveBridge.Saves.mouseSensitivity, 2f, 10f);

        ApplyMusic(musicSlider.value);
        ApplySfx  (sfxSlider.value);
        ApplySens (sensSlider.value);

        // ---------- подписки ----------
        musicSlider.onValueChanged.AddListener(ApplyMusic);
        sfxSlider.onValueChanged.AddListener(ApplySfx);
        sensSlider.onValueChanged.AddListener(ApplySens);

        _initialised = true;
    }

    // ---------- Audio ----------
    private void ApplyMusic(float v)
    {
        if (musicSource) musicSource.volume = v;
        if (_initialised) { SaveBridge.Saves.musicVolume = v; SaveBridge.Save(); }
    }

    private void ApplySfx(float v)
    {
        if (soundsManager && soundsManager.audioSource)
            soundsManager.audioSource.volume = v;
        if (_initialised) { SaveBridge.Saves.sfxVolume = v; SaveBridge.Save(); }
    }

    // ---------- Mouse sensitivity ----------
   private void ApplySens(float v)
{
    // 1. Обновляем все Camera‑states в списке
    if (playerCamera && playerCamera.CameraStateList)
    {
        foreach (var st in playerCamera.CameraStateList.tpCameraStates)
        {
            st.xMouseSensitivity = v;
            st.yMouseSensitivity = v;
        }

        /* 2. Обновляем уже активное состояние.
         * Достаточно синхронизировать lerpState, потому что именно он
         * копируется в currentState в каждом кадре.
         */
        if (playerCamera.lerpState != null)
        {
            playerCamera.lerpState.xMouseSensitivity = v;
            playerCamera.lerpState.yMouseSensitivity = v;
        }
    }

    // 3. Сохраняем настройку и применяем немедленно
    if (_initialised)
    {
        SaveBridge.Saves.mouseSensitivity = v;
        SaveBridge.Save();                              // локально + облако
    }
}
}
