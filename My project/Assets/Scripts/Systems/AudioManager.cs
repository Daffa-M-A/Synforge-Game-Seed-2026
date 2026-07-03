using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

[System.Serializable]
public struct SoundEffect {
    public string name;
    public AudioClip clip;
}

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    [Header("Audio Sources")]
    [Range(0, 1)] public float musicVolume = 0.5f;
    [Range(0, 1)] public float sfxVolume = 0.7f;
    
    private AudioSource musicSource;
    private AudioSource sfxSource;

    [Header("BGM Clips")]
    public AudioClip mainMenuBGM;
    public AudioClip gameplayBGM;

    [Header("SFX Library")]
    public List<SoundEffect> sfxLibrary = new List<SoundEffect>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            
            // Setup AudioSources secara otomatis
            musicSource = gameObject.AddComponent<AudioSource>();
            sfxSource = gameObject.AddComponent<AudioSource>();
            
            musicSource.loop = true;
            musicSource.playOnAwake = false;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void OnEnable() { SceneManager.sceneLoaded += OnSceneLoaded; }
    private void OnDisable() { SceneManager.sceneLoaded -= OnSceneLoaded; }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Auto-Play BGM berdasarkan nama scene
        if (scene.name == "Main Menu") PlayBGM(mainMenuBGM);
        else if (scene.name == "Test_3D Integration") PlayBGM(gameplayBGM);
    }

    public void PlayBGM(AudioClip clip)
    {
        if (clip == null || musicSource.clip == clip) return;
        musicSource.clip = clip;
        musicSource.volume = musicVolume;
        musicSource.Play();
    }

    public void PlaySFX(string sfxName)
    {
        SoundEffect sfx = sfxLibrary.Find(s => s.name == sfxName);
        if (sfx.clip != null)
        {
            sfxSource.PlayOneShot(sfx.clip, sfxVolume);
        }
        else
        {
            Debug.LogWarning("SFX tidak ditemukan: " + sfxName);
        }
    }

    public void StopAllSFX() { sfxSource.Stop(); }
}