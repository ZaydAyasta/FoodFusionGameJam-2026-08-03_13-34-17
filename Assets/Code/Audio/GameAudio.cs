using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class GameAudio : MonoBehaviour
{
    private const float FootstepInterval = 0.32f;

    private static GameAudio instance;

    private AudioSource musicSource;
    private AudioSource sfxSource;
    private AudioClip mainTheme;
    private AudioClip[] woodwalkClips;
    private AudioClip shootClip;
    private AudioClip dashClip;
    private int nextFootstepIndex;
    private float nextFootstepAt;

    private static GameAudio Instance
    {
        get
        {
            if (instance == null)
                instance = new GameObject("GameAudio").AddComponent<GameAudio>();

            return instance;
        }
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
        ConfigureSources();
        LoadClips();
    }

    public static void PlayMainTheme()
    {
        GameAudio audio = Instance;
        audio.EnsureReady();
        if (audio.mainTheme == null || audio.musicSource.clip == audio.mainTheme && audio.musicSource.isPlaying)
            return;

        audio.musicSource.clip = audio.mainTheme;
        audio.musicSource.loop = true;
        audio.musicSource.Play();
    }

    public static void StopMainTheme()
    {
        GameAudio audio = Instance;
        audio.EnsureReady();
        if (audio.musicSource.clip == audio.mainTheme)
            audio.musicSource.Stop();
    }

    public static void PlayFootstep()
    {
        if (Time.timeScale <= 0f)
            return;

        GameAudio audio = Instance;
        audio.EnsureReady();
        if (Time.time < audio.nextFootstepAt || audio.woodwalkClips == null || audio.woodwalkClips.Length == 0)
            return;

        AudioClip clip = audio.woodwalkClips[audio.nextFootstepIndex % audio.woodwalkClips.Length];
        audio.nextFootstepIndex++;
        audio.nextFootstepAt = Time.time + FootstepInterval;
        audio.PlaySfx(clip, 0.75f);
    }

    public static void PlayShoot()
    {
        if (Time.timeScale <= 0f)
            return;

        GameAudio audio = Instance;
        audio.EnsureReady();
        audio.PlaySfx(audio.shootClip, 0.85f);
    }

    public static void PlayDash()
    {
        if (Time.timeScale <= 0f)
            return;

        GameAudio audio = Instance;
        audio.EnsureReady();
        audio.PlaySfx(audio.dashClip, 0.9f);
    }

    private void EnsureReady()
    {
        if (musicSource == null || sfxSource == null)
            ConfigureSources();

        if (mainTheme == null)
            LoadClips();
    }

    private void ConfigureSources()
    {
        if (musicSource == null)
        {
            musicSource = gameObject.AddComponent<AudioSource>();
            musicSource.playOnAwake = false;
            musicSource.loop = true;
            musicSource.volume = 0.65f;
        }

        if (sfxSource == null)
        {
            sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.playOnAwake = false;
            sfxSource.loop = false;
            sfxSource.volume = 1f;
        }
    }

    private void LoadClips()
    {
        mainTheme = LoadAudioClip("TAKERNAL_MainTHEME");
        shootClip = LoadAudioClip("snd_squashyattackshort");
        dashClip = LoadAudioClip("snd_dash");
        woodwalkClips = new[]
        {
            LoadAudioClip("snd_woodwalk1"),
            LoadAudioClip("snd_woodwalk2"),
            LoadAudioClip("snd_woodwalk3"),
            LoadAudioClip("snd_woodwalk4")
        };
    }

    private void PlaySfx(AudioClip clip, float volume)
    {
        if (clip == null || sfxSource == null)
            return;

        sfxSource.PlayOneShot(clip, volume);
    }

    private static AudioClip LoadAudioClip(string clipName)
    {
#if UNITY_EDITOR
        string[] guids = AssetDatabase.FindAssets(clipName + " t:AudioClip", new[] { "Assets" });
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
            if (clip != null && string.Equals(clip.name, clipName, System.StringComparison.OrdinalIgnoreCase))
                return clip;
        }
#endif

        return Resources.Load<AudioClip>(clipName);
    }
}
