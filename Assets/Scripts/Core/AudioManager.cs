using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Mixer (optional)")]
    public AudioMixer mixer;                 // Có thể để trống
    public string bgmVolumeParam = "BGMVol"; // Tên Exposed Param trong Mixer (nếu dùng)
    public string sfxVolumeParam = "SFXVol";

    [Header("Default Clips")]
    public AudioClip defaultBgm;
    public AudioClip clickSfx;

    [Header("Sources")]
    [Tooltip("2 nguồn BGM để crossfade")]
    public AudioSource bgmA;
    public AudioSource bgmB;

    [Tooltip("Pool AudioSource phát SFX")]
    public int sfxPoolSize = 6;
    public AudioSource sfxTemplate; // AudioSource mẫu (mute clip) để clone làm pool

    [Header("Volumes")]
    [Range(0f, 1f)] public float bgmVolume = 0.8f;
    [Range(0f, 1f)] public float sfxVolume = 1f;

    [Header("Tweaks")]
    [Tooltip("Thời gian crossfade giữa hai track BGM.")]
    public float bgmCrossfade = 0.75f;
    [Tooltip("Độ dao động pitch ngẫu nhiên cho SFX.")]
    [Range(0f, 0.5f)] public float sfxPitchJitter = 0.03f;

    // ==== Persistence keys ====
    private const string PREF_BGM = "am_bgm";
    private const string PREF_SFX = "am_sfx";
    private const string PREF_MUSIC_ON = "am_music_on";
    private const string PREF_SFX_ON = "am_sfx_on";

    // ==== Runtime ====
    private readonly List<AudioSource> _sfxPool = new();
    private bool _useA = true; // đang dùng A phát nhạc, B để crossfade
    private Coroutine _xfadeRoutine;
    private float _cachedSfxMaster = 1f;

    // Bật/tắt nhạc & sfx
    [SerializeField] private bool musicEnabled = true;
    [SerializeField] private bool sfxEnabled = true;

    public bool MusicEnabled => musicEnabled;
    public bool SfxEnabled => sfxEnabled;

    private void Awake()
    {
        // Singleton
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // (Tùy chọn) Giữ qua scene – CHỈ nếu là root để tránh warning
        if (transform.parent == null)
            DontDestroyOnLoad(gameObject);

        // Load prefs
        bgmVolume = PlayerPrefs.GetFloat(PREF_BGM, bgmVolume);
        sfxVolume = PlayerPrefs.GetFloat(PREF_SFX, sfxVolume);
        musicEnabled = PlayerPrefs.GetInt(PREF_MUSIC_ON, 1) == 1;
        sfxEnabled = PlayerPrefs.GetInt(PREF_SFX_ON, 1) == 1;

        // Prepare SFX pool
        BuildSfxPool();

        // Áp âm lượng + cờ on/off
        ApplyVolumes();

        // Phát nhạc nền mặc định (nếu có)
        if (defaultBgm)
            PlayBGM(defaultBgm, forceInstant: true);
    }

    private void BuildSfxPool()
    {
        if (sfxTemplate == null)
        {
            // Tạo tạm nếu quên kéo thả
            var go = new GameObject("SFX_Template");
            go.transform.SetParent(transform);
            sfxTemplate = go.AddComponent<AudioSource>();
            sfxTemplate.playOnAwake = false;
            sfxTemplate.loop = false;
            sfxTemplate.spatialBlend = 0f;
        }

        for (int i = 0; i < sfxPoolSize; i++)
        {
            var s = Instantiate(sfxTemplate, transform);
            s.name = $"SFX_{i:00}";
            _sfxPool.Add(s);
        }
    }

    // ================== Public API ==================

    public void PlayClick()
    {
        if (clickSfx) PlaySFX(clickSfx);
    }

    public void PlaySFX(AudioClip clip, float volume01 = 1f, float pitch = 1f)
    {
        if (!sfxEnabled || clip == null) return;

        var src = GetFreeSfxSource();
        if (!src) return;

        src.pitch = pitch * (1f + Random.Range(-sfxPitchJitter, sfxPitchJitter));
        src.volume = Mathf.Clamp01(_cachedSfxMaster) * Mathf.Clamp01(volume01);
        src.clip = clip;
        src.Play();
    }

    /// <summary>
    /// Phát BGM với crossfade (hoặc phát ngay nếu forceInstant = true).
    /// </summary>
    public void PlayBGM(AudioClip bgm, bool forceInstant = false)
    {
        if (bgm == null) return;

        var from = _useA ? bgmA : bgmB;
        var to = _useA ? bgmB : bgmA;
        _useA = !_useA;

        if (to == null)
        {
            Debug.LogWarning("AudioManager: Missing BGM source!");
            return;
        }

        to.clip = bgm;
        to.loop = true;
        to.volume = forceInstant ? (musicEnabled ? bgmVolume : 0f) : 0f;
        to.Play();

        if (_xfadeRoutine != null) StopCoroutine(_xfadeRoutine);

        if (forceInstant || from == null)
        {
            if (from) { from.Stop(); from.volume = 0f; }
            to.volume = musicEnabled ? bgmVolume : 0f;
        }
        else
        {
            _xfadeRoutine = StartCoroutine(CrossfadeRoutine(from, to, bgmCrossfade));
        }
    }

    public void StopBGM(bool fadeOut = true, float fadeTime = 0.5f)
    {
        var cur = _useA ? bgmB : bgmA;
        if (cur == null) return;

        if (!fadeOut)
        {
            cur.Stop();
            cur.volume = 0f;
            return;
        }
        StartCoroutine(FadeOutAndStop(cur, fadeTime));
    }

    public void SetMusicEnabled(bool on)
    {
        musicEnabled = on;
        PlayerPrefs.SetInt(PREF_MUSIC_ON, on ? 1 : 0);
        ApplyVolumes();
    }

    public void SetSfxEnabled(bool on)
    {
        sfxEnabled = on;
        PlayerPrefs.SetInt(PREF_SFX_ON, on ? 1 : 0);
        ApplyVolumes();
    }

    public void SetBgmVolume(float v)
    {
        bgmVolume = Mathf.Clamp01(v);
        PlayerPrefs.SetFloat(PREF_BGM, bgmVolume);
        ApplyVolumes();
    }

    public void SetSfxVolume(float v)
    {
        sfxVolume = Mathf.Clamp01(v);
        PlayerPrefs.SetFloat(PREF_SFX, sfxVolume);
        ApplyVolumes();
    }

    public void MuteAll(bool mute)
    {
        AudioListener.pause = mute;
        AudioListener.volume = mute ? 0f : 1f;
    }

    // ================== Internals ==================

    private void ApplyVolumes()
    {
        float musicVol = musicEnabled ? bgmVolume : 0f;
        float sfxVol = sfxEnabled ? sfxVolume : 0f;

        // Nếu dùng Mixer → set dB; nếu không → set trực tiếp
        if (mixer)
        {
            mixer.SetFloat(bgmVolumeParam, LinearToDb(musicVol));
            mixer.SetFloat(sfxVolumeParam, LinearToDb(sfxVol));
        }
        else
        {
            if (bgmA) bgmA.volume = (bgmA.isPlaying ? musicVol : 0f);
            if (bgmB) bgmB.volume = (bgmB.isPlaying ? musicVol : 0f);
            // SFX volume áp khi phát, lưu lại master để nhân
            _cachedSfxMaster = sfxVol;
        }
    }

    private float LinearToDb(float v)
    {
        return (v <= 0.0001f) ? -80f : Mathf.Log10(v) * 20f;
    }

    private AudioSource GetFreeSfxSource()
    {
        foreach (var s in _sfxPool)
            if (!s.isPlaying) return s;

        return _sfxPool.Count > 0 ? _sfxPool[0] : null; // fallback
    }

    private IEnumerator CrossfadeRoutine(AudioSource from, AudioSource to, float time)
    {
        float t = 0f;
        float fromStart = from ? from.volume : 0f;
        float toStart = to ? to.volume : 0f;

        float target = musicEnabled ? bgmVolume : 0f;

        while (t < time)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / time);
            if (from) from.volume = Mathf.Lerp(fromStart, 0f, k);
            if (to) to.volume = Mathf.Lerp(toStart, target, k);
            yield return null;
        }

        if (from) { from.volume = 0f; from.Stop(); }
        if (to) to.volume = target;
        _xfadeRoutine = null;
    }

    private IEnumerator FadeOutAndStop(AudioSource src, float time)
    {
        float t = 0f;
        float start = src.volume;
        while (t < time)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / time);
            src.volume = Mathf.Lerp(start, 0f, k);
            yield return null;
        }
        src.Stop();
        src.volume = 0f;
    }
}
