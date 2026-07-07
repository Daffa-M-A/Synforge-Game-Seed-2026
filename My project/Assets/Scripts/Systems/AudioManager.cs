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
    private AudioSource sfxSource;      // Khusus suara instan (OneShot)
    private AudioSource loopSfxSource;  // Khusus suara berulang (Jalan/Run)

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
            
            // Inisialisasi semua AudioSource di dalam blok Singleton yang aman
            musicSource = gameObject.AddComponent<AudioSource>();
            sfxSource = gameObject.AddComponent<AudioSource>();
            loopSfxSource = gameObject.AddComponent<AudioSource>(); // Tambahkan di sini

            musicSource.loop = true;
            musicSource.playOnAwake = false;

            loopSfxSource.loop = true; // Set loop source agar selalu melingkar
            loopSfxSource.playOnAwake = false;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    // ==========================================
    // LOGIKA SFX LOOPING (Jalan, Mesin, dll)
    // ==========================================
    public void PlayLoopSFX(string sfxName)
    {
        SoundEffect sfx = sfxLibrary.Find(s => s.name == sfxName);
        if (sfx.clip == null) return;
        
        // Jangan putar ulang jika sfx yang sama sedang berjalan
        if (loopSfxSource.isPlaying && loopSfxSource.clip == sfx.clip) return;

        loopSfxSource.clip = sfx.clip;
        loopSfxSource.volume = sfxVolume;
        loopSfxSource.Play();
    }
    
    public void StopLoopSFX()
    {
        if (loopSfxSource.isPlaying)
            loopSfxSource.Stop();
    }

    // ==========================================
    // LOGIKA SFX INSTAN (Tangkap, Klik, Menang)
    // ==========================================
    public void PlaySFX(string sfxName)
    {
        SoundEffect sfx = sfxLibrary.Find(s => s.name == sfxName);
        if (sfx.clip != null)
        {
            // PlayOneShot tidak akan terpotong jika sfxSource.Stop() tidak dipanggil
            sfxSource.PlayOneShot(sfx.clip, sfxVolume);
        }
        else
        {
            Debug.LogWarning("SFX tidak ditemukan: " + sfxName);
        }
    }

    // JANGAN GUNAKAN INI UNTUK MENGHENTIKAN SUARA JALAN
    // Fungsi ini akan dikosongkan agar tidak memutus suara penangkapan pion
    public void StopSFX()
    {
        // Sengaja dikosongkan. 
        // Pemanggilan StopSFX() dari script lama tidak akan lagi mematikan semua suara.
    }

    // ==========================================
    // LOGIKA BGM & SCENE
    // ==========================================
    private void OnEnable() { SceneManager.sceneLoaded += OnSceneLoaded; }
    private void OnDisable() { SceneManager.sceneLoaded -= OnSceneLoaded; }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name.Contains("Main Menu")) PlayBGM(mainMenuBGM);
        else if (scene.name == "Test_3D Integration") PlayBGM(gameplayBGM);
    }

    public void PlayBGM(AudioClip clip)
    {
        if (clip == null || musicSource.clip == clip) return;
        musicSource.clip = clip;
        musicSource.volume = musicVolume;
        musicSource.Play();
    }
}