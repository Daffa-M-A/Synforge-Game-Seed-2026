using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[System.Serializable]
public struct SoundEffect
{
    public string name;
    public AudioClip clip;
}

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    public const string SfxUIButtonPress = "UIButtonPress";
    public const string SfxTimeticking = "TimetickingSFX";
    public const string SfxTurnChangeNormal = "TurnChangeNormalSFX";
    public const string SfxTurnChangeForced = "TurnChangeForcedSFX";
    public const string SfxGlobalTimerStop = "GlobalTimerStopSFX";
    public const string SfxGlobalTimerEnding = "GlobalTimerEndingSFX";
    public const string SfxTurnTimerEnding = "TurnTimerEndingSFX";
    public const string SfxVictory1 = "VictorySFX1";
    public const string SfxVictory2 = "VictorySFX2";
    public const string SfxDraw = "DrawSFX";
    public const string SfxRun = "RunSFX";
    public const string SfxReSpawn = "ReSpawnSFX";
    public const string SfxTutorialObjectiveCleared = "TutorialObjectiveClearedSFX";

    private const float GlobalWarningThreshold = 60f;
    private const float TurnWarningThreshold = 10f;
    private const string MusicVolumePrefKey = "AudioManager.MusicVolume";
    private const string SfxVolumePrefKey = "AudioManager.SfxVolume";
    private const string MusicSliderName = "Music Slider";
    private const string SfxSliderName = "SFX Slider";

    [Header("Audio Sources")]
    [Range(0, 1)] [SerializeField] private float musicVolume = 0.5f;
    [Range(0, 1)] [SerializeField] private float sfxVolume = 0.7f;

    public float MusicVolume => musicVolume;
    public float SfxVolume => sfxVolume;

    private AudioSource musicSource;
    private AudioSource sfxSource;
    private AudioSource runLoopSource;
    private AudioSource ambientLoopSource;
    private AudioSource globalCountdownSource;
    private AudioSource turnCountdownSource;

    [Header("BGM Clips")]
    public AudioClip mainMenuBGM;
    public AudioClip gameplayBGM;
    public AudioClip tutorialBGM;

    [Header("Scene Classification")]
    [SerializeField] private string[] menuSceneNames = { "Main Menu", "Main Menu 1" };
    [SerializeField] private string[] tutorialSceneNames = { "Tutorial Scene" };
    [SerializeField] private string[] gameplaySceneNames =
    {
        "Test_3D Intergration",
        "Tutorial Scene",
        "Prototype Chess",
        "Prototype Gameplay UI",
        "Test 1_UI Integration",
        "Test 1_UI Integration (Bahtiar)"
    };

    [Header("SFX Library")]
    public List<SoundEffect> sfxLibrary = new List<SoundEffect>();

    private readonly HashSet<int> hookedButtonIds = new HashSet<int>();
    private bool isGameplayActive;
    private bool gameplayAudioPaused;
    private TurnManager boundTurnManager;
    private Coroutine victorySequenceRoutine;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        LoadVolumeSettings();

        musicSource = CreateSource("MusicSource", loop: true);
        sfxSource = CreateSource("SfxSource");
        runLoopSource = CreateSource("RunLoopSource", loop: true);
        ambientLoopSource = CreateSource("AmbientLoopSource", loop: true);
        globalCountdownSource = CreateSource("GlobalCountdownSource", loop: true);
        turnCountdownSource = CreateSource("TurnCountdownSource", loop: true);
    }

    private void Start()
    {
        Scene activeScene = SceneManager.GetActiveScene();
        HookVolumeSlidersInScene(activeScene);
        ApplySceneAudio(activeScene);
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        UnbindTurnManager();
    }

    private AudioSource CreateSource(string sourceName, bool loop = false)
    {
        AudioSource source = gameObject.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = loop;
        source.name = sourceName;
        return source;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        HookUIButtonsInScene(scene);
        HookVolumeSlidersInScene(scene);
        ApplySceneAudio(scene);
    }

    public void SetMusicVolume(float normalizedVolume)
    {
        musicVolume = Mathf.Clamp01(normalizedVolume);
        ApplyVolumeSettings();
        SaveVolumeSettings();
        RefreshAllVolumeSliders();
    }

    public void SetSfxVolume(float normalizedVolume)
    {
        sfxVolume = Mathf.Clamp01(normalizedVolume);
        ApplyVolumeSettings();
        SaveVolumeSettings();
        RefreshAllVolumeSliders();
    }

    public void SetMusicVolumeFromSlider(float sliderValue)
    {
        SetMusicVolume(sliderValue / 100f);
    }

    public void SetSfxVolumeFromSlider(float sliderValue)
    {
        SetSfxVolume(sliderValue / 100f);
    }

    private void LoadVolumeSettings()
    {
        musicVolume = PlayerPrefs.GetFloat(MusicVolumePrefKey, musicVolume);
        sfxVolume = PlayerPrefs.GetFloat(SfxVolumePrefKey, sfxVolume);
        ApplyVolumeSettings();
    }

    private void SaveVolumeSettings()
    {
        PlayerPrefs.SetFloat(MusicVolumePrefKey, musicVolume);
        PlayerPrefs.SetFloat(SfxVolumePrefKey, sfxVolume);
        PlayerPrefs.Save();
    }

    private void ApplyVolumeSettings()
    {
        if (musicSource != null)
            musicSource.volume = musicVolume;

        ApplySfxVolumeToSource(sfxSource);
        ApplySfxVolumeToSource(runLoopSource);
        ApplySfxVolumeToSource(ambientLoopSource);
        ApplySfxVolumeToSource(globalCountdownSource);
        ApplySfxVolumeToSource(turnCountdownSource);
    }

    private void ApplySfxVolumeToSource(AudioSource source)
    {
        if (source != null)
            source.volume = sfxVolume;
    }

    private void RefreshAllVolumeSliders()
    {
        AudioVolumeSlider[] sliders = FindObjectsByType<AudioVolumeSlider>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        for (int i = 0; i < sliders.Length; i++)
            sliders[i].RefreshFromAudioManager();
    }

    private void HookVolumeSlidersInScene(Scene scene)
    {
        if (!scene.isLoaded)
            return;

        GameObject[] roots = scene.GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
        {
            Slider[] sliders = roots[i].GetComponentsInChildren<Slider>(true);
            for (int j = 0; j < sliders.Length; j++)
            {
                HookVolumeSlider(sliders[j]);
            }
        }
    }

    private void HookVolumeSlider(Slider slider)
    {
        if (slider == null)
            return;

        if (slider.name == MusicSliderName)
        {
            AudioVolumeSlider volumeSlider = slider.GetComponent<AudioVolumeSlider>();
            if (volumeSlider == null)
                volumeSlider = slider.gameObject.AddComponent<AudioVolumeSlider>();

            volumeSlider.Setup(AudioVolumeSlider.Channel.Music);
            return;
        }

        if (slider.name == SfxSliderName)
        {
            AudioVolumeSlider volumeSlider = slider.GetComponent<AudioVolumeSlider>();
            if (volumeSlider == null)
                volumeSlider = slider.gameObject.AddComponent<AudioVolumeSlider>();

            volumeSlider.Setup(AudioVolumeSlider.Channel.Sfx);
        }
    }

    private void ApplySceneAudio(Scene scene)
    {
        if (IsMenuScene(scene.name))
        {
            EnterMenuMode();
            return;
        }

        if (IsGameplayScene(scene.name))
        {
            EnterGameplayMode(scene.name);
        }
    }

    private bool IsTutorialScene(string sceneName)
    {
        for (int i = 0; i < tutorialSceneNames.Length; i++)
        {
            if (sceneName == tutorialSceneNames[i])
                return true;
        }

        return false;
    }

    private bool IsMenuScene(string sceneName)
    {
        for (int i = 0; i < menuSceneNames.Length; i++)
        {
            if (sceneName.Contains(menuSceneNames[i]))
                return true;
        }

        return false;
    }

    private bool IsGameplayScene(string sceneName)
    {
        for (int i = 0; i < gameplaySceneNames.Length; i++)
        {
            if (sceneName == gameplaySceneNames[i])
                return true;
        }

        return false;
    }

    private void EnterMenuMode()
    {
        isGameplayActive = false;
        gameplayAudioPaused = false;
        UnbindTurnManager();
        StopAllGameplayAudio(stopMusic: true);
        PlayBGM(mainMenuBGM);
    }

    private void EnterGameplayMode(string sceneName)
    {
        isGameplayActive = true;
        gameplayAudioPaused = false;
        PlayBGM(IsTutorialScene(sceneName) ? tutorialBGM : gameplayBGM);
        PlayLoopOnSource(ambientLoopSource, SfxTimeticking);
        BindTurnManager();
    }

    private void BindTurnManager()
    {
        UnbindTurnManager();
        boundTurnManager = FindFirstObjectByType<TurnManager>();
        if (boundTurnManager != null)
        {
            boundTurnManager.OnTurnEnded += HandleTurnEnded;
        }
    }

    private void UnbindTurnManager()
    {
        if (boundTurnManager != null)
        {
            boundTurnManager.OnTurnEnded -= HandleTurnEnded;
            boundTurnManager = null;
        }
    }

    private void HandleTurnEnded(bool forcedByTimeout)
    {
        if (!isGameplayActive || gameplayAudioPaused)
            return;

        PlaySFX(forcedByTimeout ? SfxTurnChangeForced : SfxTurnChangeNormal);
    }

    public void UpdateTimerWarnings(float globalRemaining, float turnRemaining, bool isGameOver)
    {
        if (!isGameplayActive || gameplayAudioPaused || isGameOver)
        {
            StopSource(globalCountdownSource);
            StopSource(turnCountdownSource);
            return;
        }

        if (globalRemaining <= GlobalWarningThreshold && globalRemaining > 0f)
            PlayLoopOnSource(globalCountdownSource, SfxGlobalTimerEnding);
        else
            StopSource(globalCountdownSource);

        if (turnRemaining <= TurnWarningThreshold && turnRemaining > 0f)
            PlayLoopOnSource(turnCountdownSource, SfxTurnTimerEnding);
        else
            StopSource(turnCountdownSource);
    }

    public void NotifyGlobalTimerExpired()
    {
        if (!isGameplayActive)
            return;

        StopSource(globalCountdownSource);
        StopSource(turnCountdownSource);
        StopSource(ambientLoopSource);
        PlaySFX(SfxGlobalTimerStop);
    }

    public void NotifyGameOver(int winningTeam, bool fromGlobalTimer)
    {
        if (victorySequenceRoutine != null)
        {
            StopCoroutine(victorySequenceRoutine);
            victorySequenceRoutine = null;
        }

        StopAllGameplayAudio(stopMusic: true);

        if (fromGlobalTimer)
        {
            if (winningTeam == -1)
                PlaySFX(SfxDraw);

            return;
        }

        if (winningTeam == 0)
        {
            victorySequenceRoutine = StartCoroutine(PlayVictorySequence());
            return;
        }

        if (winningTeam == -1)
            PlaySFX(SfxDraw);
    }

    private IEnumerator PlayVictorySequence()
    {
        AudioClip first = GetClip(SfxVictory1);
        AudioClip second = GetClip(SfxVictory2);

        if (first != null)
        {
            sfxSource.PlayOneShot(first, sfxVolume);
            yield return new WaitForSeconds(first.length);
        }

        if (second != null)
            sfxSource.PlayOneShot(second, sfxVolume);

        victorySequenceRoutine = null;
    }

    public void SetGameplayAudioPaused(bool paused)
    {
        gameplayAudioPaused = paused;

        if (paused)
        {
            PauseSource(musicSource);
            PauseSource(ambientLoopSource);
            PauseSource(globalCountdownSource);
            PauseSource(turnCountdownSource);
            PauseSource(runLoopSource);
            return;
        }

        if (!isGameplayActive)
            return;

        UnpauseSource(musicSource);
        UnpauseSource(ambientLoopSource);
        UnpauseSource(globalCountdownSource);
        UnpauseSource(turnCountdownSource);
        UnpauseSource(runLoopSource);
    }

    public void PlayLoopSFX(string sfxName)
    {
        PlayLoopOnSource(runLoopSource, sfxName);
    }

    public void StopLoopSFX()
    {
        StopSource(runLoopSource);
    }

    public void PlaySFX(string sfxName)
    {
        AudioClip clip = GetClip(sfxName);
        if (clip != null)
        {
            sfxSource.PlayOneShot(clip, sfxVolume);
            return;
        }

        Debug.LogWarning("SFX tidak ditemukan: " + sfxName);
    }

    public void PlayButtonSfx()
    {
        PlaySFX(SfxUIButtonPress);
    }

    public void StopSFX()
    {
        // Intentionally empty to preserve legacy calls without cutting active one-shots.
    }

    public void PlayBGM(AudioClip clip)
    {
        if (clip == null)
            return;

        if (musicSource.clip == clip && musicSource.isPlaying)
            return;

        musicSource.clip = clip;
        musicSource.volume = musicVolume;
        musicSource.Play();
    }

    private void StopAllGameplayAudio(bool stopMusic)
    {
        StopSource(ambientLoopSource);
        StopSource(globalCountdownSource);
        StopSource(turnCountdownSource);
        StopSource(runLoopSource);

        if (stopMusic)
            musicSource.Stop();
    }

    private void HookUIButtonsInScene(Scene scene)
    {
        if (!scene.isLoaded)
            return;

        GameObject[] roots = scene.GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
        {
            Button[] buttons = roots[i].GetComponentsInChildren<Button>(true);
            for (int j = 0; j < buttons.Length; j++)
            {
                Button button = buttons[j];
                int buttonId = button.GetInstanceID();
                if (hookedButtonIds.Contains(buttonId))
                    continue;

                hookedButtonIds.Add(buttonId);
                button.onClick.AddListener(PlayButtonSfx);
            }
        }
    }

    private void PlayLoopOnSource(AudioSource source, string sfxName)
    {
        AudioClip clip = GetClip(sfxName);
        if (clip == null || source == null)
            return;

        if (source.isPlaying && source.clip == clip)
            return;

        source.clip = clip;
        source.volume = sfxVolume;
        source.Play();
    }

    private AudioClip GetClip(string sfxName)
    {
        if (sfxName == "Button")
            sfxName = SfxUIButtonPress;

        for (int i = 0; i < sfxLibrary.Count; i++)
        {
            if (sfxLibrary[i].name == sfxName)
                return sfxLibrary[i].clip;
        }

        return null;
    }

    private static void StopSource(AudioSource source)
    {
        if (source != null && source.isPlaying)
            source.Stop();
    }

    private static void PauseSource(AudioSource source)
    {
        if (source != null && source.isPlaying)
            source.Pause();
    }

    private static void UnpauseSource(AudioSource source)
    {
        if (source != null)
            source.UnPause();
    }
}
