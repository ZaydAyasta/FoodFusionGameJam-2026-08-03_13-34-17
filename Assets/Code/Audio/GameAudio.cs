using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class GameAudio : MonoBehaviour
{
    private const float FootstepInterval = 0.32f;
    private const float MainThemeVolume = 0.65f;
    private const float CombatMusicVolume = 0.7f;
    private const float ShopMusicVolume = 0.7f;
    private const float MusicFadeDuration = 1f;
    private const float KitchenSilenceDuration = 0.25f;

    private static GameAudio instance;

    private AudioSource musicSource;
    private AudioSource shopMusicSource;
    private AudioSource sfxSource;
    private AudioClip mainTheme;
    private AudioClip shopTheme;
    private AudioClip[] combatClips;
    private AudioClip[] woodwalkClips;
    private AudioClip shootClip;
    private AudioClip dashClip;
    private int nextFootstepIndex;
    private float nextFootstepAt;
    private int nextCombatIndex;
    private MusicMode currentMusicMode;
    private Coroutine musicTransitionRoutine;

    private enum MusicMode
    {
        None,
        MainMenu,
        Combat,
        Shop
    }

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

    private void Update()
    {
        if (currentMusicMode != MusicMode.Combat || musicSource == null || musicSource.isPlaying)
            return;

        PlayNextCombatClip();
    }

    public static void PlayMainTheme()
    {
        GameAudio audio = Instance;
        audio.EnsureReady();
        if (audio.mainTheme == null || audio.currentMusicMode == MusicMode.MainMenu && audio.musicSource.isPlaying)
            return;

        audio.StopMusicTransition();
        audio.StopShopMusic();
        audio.musicSource.clip = audio.mainTheme;
        audio.musicSource.loop = true;
        audio.musicSource.volume = MainThemeVolume;
        audio.musicSource.Play();
        audio.currentMusicMode = MusicMode.MainMenu;
    }

    public static void StopMainTheme()
    {
        GameAudio audio = Instance;
        audio.EnsureReady();
        if (audio.currentMusicMode == MusicMode.MainMenu || audio.musicSource.clip == audio.mainTheme)
        {
            audio.musicSource.Stop();
            audio.currentMusicMode = MusicMode.None;
        }
    }

    public static void PlayCombatMusic()
    {
        GameAudio audio = Instance;
        audio.EnsureReady();
        if (audio.combatClips == null || audio.combatClips.Length == 0)
            return;

        audio.StopMusicTransition();
        bool crossfadingFromShop = audio.shopMusicSource != null && audio.shopMusicSource.isPlaying;
        if (audio.currentMusicMode != MusicMode.Combat || audio.musicSource.clip == null)
            audio.PlayNextCombatClip(crossfadingFromShop ? 0f : CombatMusicVolume);

        audio.musicSource.loop = false;
        audio.currentMusicMode = MusicMode.Combat;

        if (crossfadingFromShop)
        {
            audio.musicSource.volume = 0f;
            audio.musicTransitionRoutine = audio.StartCoroutine(audio.CrossfadeShopToCombat());
            return;
        }

        audio.musicSource.volume = CombatMusicVolume;
    }

    public static void PlayKitchenShopMusic()
    {
        GameAudio audio = Instance;
        audio.EnsureReady();
        if (audio.shopTheme == null)
            return;

        audio.StopMusicTransition();
        audio.musicTransitionRoutine = audio.StartCoroutine(audio.FadeCombatToShop());
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

        if (mainTheme == null || combatClips == null)
            LoadClips();
    }

    private void ConfigureSources()
    {
        if (musicSource == null)
        {
            musicSource = gameObject.AddComponent<AudioSource>();
            musicSource.playOnAwake = false;
            musicSource.loop = false;
            musicSource.volume = CombatMusicVolume;
        }

        if (shopMusicSource == null)
        {
            shopMusicSource = gameObject.AddComponent<AudioSource>();
            shopMusicSource.playOnAwake = false;
            shopMusicSource.loop = true;
            shopMusicSource.volume = 0f;
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
        shopTheme = LoadAudioClip("TAKERNAL_JapaneseSHOPFinality");
        combatClips = new[]
        {
            LoadAudioClip("Royalty-Free-Heavy-Metal-Instrumental-VIOLENCE-MACHINE-DOWNLOAD-4"),
            LoadAudioClip("Royalty-Free-Heavy-Metal-Instrumental-The-Gallows-_Creative-Commons_"),
            LoadAudioClip("Royalty-Free-Heavy-Metal-Instrumental-Game-Over-4")
        };
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

    private void PlayNextCombatClip(float volume = CombatMusicVolume)
    {
        AudioClip clip = GetNextValidCombatClip();
        if (clip == null)
            return;

        musicSource.clip = clip;
        musicSource.loop = false;
        musicSource.volume = volume;
        musicSource.Play();
    }

    private AudioClip GetNextValidCombatClip()
    {
        if (combatClips == null || combatClips.Length == 0)
            return null;

        for (int i = 0; i < combatClips.Length; i++)
        {
            AudioClip clip = combatClips[nextCombatIndex % combatClips.Length];
            nextCombatIndex++;
            if (clip != null)
                return clip;
        }

        return null;
    }

    private System.Collections.IEnumerator FadeCombatToShop()
    {
        currentMusicMode = MusicMode.Shop;
        yield return FadeSourceVolume(musicSource, 0f, MusicFadeDuration);
        if (musicSource != null)
            musicSource.Pause();

        yield return new WaitForSecondsRealtime(KitchenSilenceDuration);

        shopMusicSource.clip = shopTheme;
        shopMusicSource.loop = true;
        shopMusicSource.volume = 0f;
        if (!shopMusicSource.isPlaying)
            shopMusicSource.Play();

        yield return FadeSourceVolume(shopMusicSource, ShopMusicVolume, MusicFadeDuration);
        musicTransitionRoutine = null;
    }

    private System.Collections.IEnumerator CrossfadeShopToCombat()
    {
        if (!musicSource.isPlaying)
            musicSource.Play();

        float elapsed = 0f;
        float startCombatVolume = musicSource.volume;
        float startShopVolume = shopMusicSource.volume;
        while (elapsed < MusicFadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / MusicFadeDuration);
            musicSource.volume = Mathf.Lerp(startCombatVolume, CombatMusicVolume, t);
            shopMusicSource.volume = Mathf.Lerp(startShopVolume, 0f, t);
            yield return null;
        }

        musicSource.volume = CombatMusicVolume;
        StopShopMusic();
        musicTransitionRoutine = null;
    }

    private static System.Collections.IEnumerator FadeSourceVolume(AudioSource source, float targetVolume, float duration)
    {
        if (source == null)
            yield break;

        float elapsed = 0f;
        float startVolume = source.volume;
        float safeDuration = Mathf.Max(0.01f, duration);
        while (elapsed < safeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            source.volume = Mathf.Lerp(startVolume, targetVolume, Mathf.Clamp01(elapsed / safeDuration));
            yield return null;
        }

        source.volume = targetVolume;
    }

    private void StopMusicTransition()
    {
        if (musicTransitionRoutine == null)
            return;

        StopCoroutine(musicTransitionRoutine);
        musicTransitionRoutine = null;
    }

    private void StopShopMusic()
    {
        if (shopMusicSource == null)
            return;

        shopMusicSource.Stop();
        shopMusicSource.clip = null;
        shopMusicSource.volume = 0f;
    }

    private static AudioClip LoadAudioClip(string clipName)
    {
#if UNITY_EDITOR
        string[] guids = AssetDatabase.FindAssets(clipName + " t:AudioClip", new[] { "Assets" });
        AudioClip fallbackClip = null;
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
            if (clip != null && string.Equals(clip.name, clipName, System.StringComparison.OrdinalIgnoreCase))
                return clip;

            if (fallbackClip == null)
                fallbackClip = clip;
        }

        if (fallbackClip != null)
            return fallbackClip;
#endif

        return Resources.Load<AudioClip>("Audio/" + clipName) ?? Resources.Load<AudioClip>(clipName);
    }
}
