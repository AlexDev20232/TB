    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;

    public class SoundsManager : MonoBehaviour
    {
        public static SoundsManager Instance;

        
        public AudioSource audioSource;

        [Header("Звуки")]
        public AudioClip clickSound;
        public AudioClip errorSound;
        public AudioClip successSound;
        public AudioClip getMoneySound;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
            
             audioSource.volume = SaveBridge.Saves.sfxVolume;
        }

        public void PlayClickSound()
        {
            audioSource.PlayOneShot(clickSound);
        }

        public void PlayErrorSound()
        {
            audioSource.PlayOneShot(errorSound);
        }

        public void PlaySuccessSound()
        {
            audioSource.PlayOneShot(successSound);
        }

        public void PlayGetMoneySound()
        {
            audioSource.PlayOneShot(getMoneySound);
        }
    }
