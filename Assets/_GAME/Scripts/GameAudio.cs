using System.Collections;
using UnityEngine;

// Все звуки игры в одном месте: клипы и громкости настраиваются в инспекторе.
// Монстры и порталы звучат в 3D из своей точки, меню и экраны — в 2D.
// Музыка главного меню играет громко в меню и приглушается во время игры.
// Пустой клип = звук просто не играет (без ошибок).
public class GameAudio : MonoBehaviour
{
    public static GameAudio Instance { get; private set; }

    [SerializeField] GameStateManager gameState;

    [Header("Монстр (3D)")]
    [SerializeField] AudioClip monsterAttackClip;
    [SerializeField, Range(0f, 1f)] float monsterAttackVolume = 1f;
    [SerializeField] AudioClip monsterDeathClip;
    [SerializeField, Range(0f, 1f)] float monsterDeathVolume = 1f;

    [Header("Портал (3D)")]
    [SerializeField] AudioClip portalOpenClip;
    [SerializeField, Range(0f, 1f)] float portalOpenVolume = 0.8f;

    [Header("Настройки 3D-звука")]
    [SerializeField] float minDistance3D = 1f;
    [SerializeField] float maxDistance3D = 20f;

    [Header("Меню (2D)")]
    [SerializeField] AudioClip buttonClickClip;
    [SerializeField, Range(0f, 1f)] float buttonClickVolume = 0.7f;

    [Header("Экраны (2D)")]
    [SerializeField] AudioClip victoryClip;
    [SerializeField, Range(0f, 1f)] float victoryVolume = 1f;
    [SerializeField] AudioClip defeatClip;
    [SerializeField, Range(0f, 1f)] float defeatVolume = 1f;

    [Header("Музыка главного меню (2D)")]
    [SerializeField] AudioClip menuMusicClip;
    [SerializeField, Range(0f, 1f)] float musicVolumeInMenu = 0.5f;
    // 0 — в игре музыка выключена
    [SerializeField, Range(0f, 1f)] float musicVolumeInGame = 0f;
    [SerializeField] float musicFadeTime = 1f;

    AudioSource uiSource;
    AudioSource musicSource;
    Coroutine musicFade;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }
        Instance = this;

        if (gameState == null)
            gameState = FindFirstObjectByType<GameStateManager>();

        uiSource = gameObject.AddComponent<AudioSource>();
        uiSource.playOnAwake = false;
        uiSource.spatialBlend = 0f;

        musicSource = gameObject.AddComponent<AudioSource>();
        musicSource.playOnAwake = false;
        musicSource.loop = true;
        musicSource.spatialBlend = 0f;
        musicSource.volume = 0f;
    }

    void OnEnable()
    {
        if (gameState != null)
            gameState.StateChanged += OnStateChanged;
    }

    void OnDisable()
    {
        if (gameState != null)
            gameState.StateChanged -= OnStateChanged;
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    // --- Вызовы из игры ---

    public void PlayMonsterAttack(Vector3 position) => PlayAt(monsterAttackClip, position, monsterAttackVolume);
    public void PlayMonsterDeath(Vector3 position) => PlayAt(monsterDeathClip, position, monsterDeathVolume);
    public void PlayPortalOpen(Vector3 position) => PlayAt(portalOpenClip, position, portalOpenVolume);
    public void PlayButtonClick() => PlayUi(buttonClickClip, buttonClickVolume);

    void OnStateChanged(GameState state)
    {
        switch (state)
        {
            case GameState.Menu:
                FadeMusic(musicVolumeInMenu);
                break;
            case GameState.Playing:
                FadeMusic(musicVolumeInGame);
                break;
            case GameState.Victory:
                PlayUi(victoryClip, victoryVolume);
                break;
            case GameState.Defeat:
                PlayUi(defeatClip, defeatVolume);
                break;
        }
    }

    void PlayUi(AudioClip clip, float volume)
    {
        if (clip != null)
            uiSource.PlayOneShot(clip, volume);
    }

    // Отдельный временный источник в точке: spatialBlend = 1, чтобы было слышно, откуда звук
    void PlayAt(AudioClip clip, Vector3 position, float volume)
    {
        if (clip == null)
            return;

        var go = new GameObject($"SFX {clip.name}");
        go.transform.position = position;
        var source = go.AddComponent<AudioSource>();
        source.clip = clip;
        source.volume = volume;
        source.spatialBlend = 1f;
        source.rolloffMode = AudioRolloffMode.Linear;
        source.minDistance = minDistance3D;
        source.maxDistance = maxDistance3D;
        source.Play();
        Destroy(go, clip.length + 0.1f);
    }

    void FadeMusic(float targetVolume)
    {
        if (menuMusicClip == null)
            return;

        if (musicSource.clip != menuMusicClip)
            musicSource.clip = menuMusicClip;
        if (!musicSource.isPlaying && targetVolume > 0f)
        {
            // После приглушения в игре продолжаем с того же места, а не с начала
            if (musicSource.time > 0f)
                musicSource.UnPause();
            else
                musicSource.Play();
        }

        if (musicFade != null)
            StopCoroutine(musicFade);
        musicFade = StartCoroutine(FadeMusicRoutine(targetVolume));
    }

    // Unscaled-время: музыка меняется и при Time.timeScale = 0
    IEnumerator FadeMusicRoutine(float targetVolume)
    {
        float start = musicSource.volume;
        float t = 0f;
        while (t < musicFadeTime)
        {
            t += Time.unscaledDeltaTime;
            musicSource.volume = Mathf.Lerp(start, targetVolume, musicFadeTime > 0f ? t / musicFadeTime : 1f);
            yield return null;
        }
        musicSource.volume = targetVolume;
        if (targetVolume <= 0f)
            musicSource.Pause();
        musicFade = null;
    }
}
