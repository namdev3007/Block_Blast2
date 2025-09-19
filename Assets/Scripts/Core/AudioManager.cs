using UnityEngine;
using UnityEngine.Audio;
using System.Collections;
using System.Collections.Generic; // <== thêm

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Clips & Mixer")]
    public AudioClip defaultBgm;
    public AudioMixer mixer;
    public string bgmVolumeParam = "BGMVol";
    public string sfxVolumeParam = "SFXVol";
    [Tooltip("Group đầu ra cho BGM phải thuộc Mixer có expose BGMVol")]
    public AudioMixerGroup musicGroup;
    [Tooltip("Group đầu ra cho SFX phải thuộc Mixer có expose SFXVol")]
    public AudioMixerGroup sfxGroup;

    [Header("UI / Meta SFX")]
    public AudioClip clickSfx;
    public AudioClip startGameSfx;

    [Header("Gameplay SFX")]
    public AudioClip pickupSfx;
    public AudioClip dropSfx;
    public AudioClip[] clearComboSfxByTier;

    [Header("Praise SFX")]
    public AudioClip goodSfx;       // 2
    public AudioClip greatSfx;      // 3
    public AudioClip excellentSfx;  // 4
    public AudioClip fantasticSfx;  // 5
    public AudioClip legendarySfx;  // 6+
    public AudioClip unbelievableSfx;

    [Header("Sources")]
    public AudioSource musicSource;       // loop BGM
    public AudioSource sfxSource;         // one-shot SFX (giữ lại để tương thích)

    [Header("SFX Polyphony & Anti-Spam")]
    [Tooltip("Số voice SFX có thể phát chồng cùng lúc")]
    public int sfxPolyphony = 8;
    [Tooltip("Giãn cách phát lại cùng 1 clip (ms) để tránh spam)")]
    public int sameClipCooldownMs = 35;
    [Tooltip("Jitter cao độ để bớt nhàm chán")]
    public float pitchJitter = 0.08f;

    // PlayerPrefs keys
    public const string KEY_MUSIC = "Audio.MusicEnabled";
    public const string KEY_SFX = "Audio.SfxEnabled";
    public const string KEY_MUSIC_VOL = "Audio.MusicVol01"; // 0..1
    public const string KEY_SFX_VOL = "Audio.SfxVol01";     // 0..1

    private bool _musicEnabled = true;
    private bool _sfxEnabled = true;

    [Range(0f, 1f)][SerializeField] private float _musicVolume = 1f;
    [Range(0f, 1f)][SerializeField] private float _sfxVolume = 1f;

    // ==== NEW: SFX voice pool & anti-spam ====
    private AudioSource[] _sfxVoices;
    private int _sfxVoiceCursor = 0;
    private readonly Dictionary<AudioClip, float> _lastPlayTime = new(); // clip -> Time.unscaledTime

    void Awake()
    {
        if (Instance && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        _musicEnabled = PlayerPrefs.GetInt(KEY_MUSIC, 1) == 1;
        _sfxEnabled = PlayerPrefs.GetInt(KEY_SFX, 1) == 1;
        _musicVolume = PlayerPrefs.GetFloat(KEY_MUSIC_VOL, 1f);
        _sfxVolume = PlayerPrefs.GetFloat(KEY_SFX_VOL, 1f);

        if (musicSource && musicGroup) musicSource.outputAudioMixerGroup = musicGroup;
        if (sfxSource && sfxGroup) sfxSource.outputAudioMixerGroup = sfxGroup;

        // NEW: tạo voice pool
        BuildSfxVoices();

        UpdateMusicGain();
        UpdateSfxGain();
    }

    void Start()
    {
        StartCoroutine(BootAtEndOfFrame());
    }

    private void BuildSfxVoices()
    {
        // tái dùng sfxSource làm voice[0] để tương thích
        sfxPolyphony = Mathf.Max(1, sfxPolyphony);
        _sfxVoices = new AudioSource[sfxPolyphony];
        for (int i = 0; i < sfxPolyphony; i++)
        {
            if (i == 0 && sfxSource != null)
            {
                _sfxVoices[0] = sfxSource;
            }
            else
            {
                var go = new GameObject($"SFX_Voice_{i}");
                go.transform.SetParent(transform, false);
                var src = go.AddComponent<AudioSource>();
                src.playOnAwake = false;
                src.loop = false;
                src.outputAudioMixerGroup = sfxGroup ? sfxGroup : (sfxSource ? sfxSource.outputAudioMixerGroup : null);
                _sfxVoices[i] = src;
            }
        }
        _sfxVoiceCursor = 0;
    }

    IEnumerator BootAtEndOfFrame()
    {
        yield return null;
        UpdateMusicGain();
        UpdateSfxGain();
        EnsureBgmPlaying();
    }

    private void EnsureBgmPlaying()
    {
        if (!musicSource) return;
        if (!musicSource.clip) musicSource.clip = defaultBgm;

        if (_musicEnabled && musicSource.clip && !musicSource.isPlaying)
        {
            AudioListener.pause = false;
            musicSource.PlayDelayed(0.01f);
        }
        else if (!_musicEnabled && musicSource.isPlaying)
        {
            musicSource.Pause();
        }
    }

    private static float Volume01ToDb(float v01)
    {
        if (v01 <= 0.0001f) return -80f;
        return Mathf.Log10(Mathf.Clamp01(v01)) * 20f;
    }

    private void UpdateMusicGain()
    {
        if (!mixer) return;
        float v = _musicEnabled ? _musicVolume : 0f;
        mixer.SetFloat(bgmVolumeParam, Volume01ToDb(v));
    }

    private void UpdateSfxGain()
    {
        if (!mixer) return;
        float v = _sfxEnabled ? _sfxVolume : 0f;
        mixer.SetFloat(sfxVolumeParam, Volume01ToDb(v));
    }

    // ===== Settings API =====
    public void SetMusicEnabled(bool on)
    {
        _musicEnabled = on;
        PlayerPrefs.SetInt(KEY_MUSIC, on ? 1 : 0);
        PlayerPrefs.Save();
        UpdateMusicGain();

        if (!musicSource) return;
        if (on)
        {
            if (!musicSource.clip) musicSource.clip = defaultBgm;
            EnsureBgmPlaying();
        }
        else musicSource.Pause();
    }

    public void SetSfxEnabled(bool on)
    {
        _sfxEnabled = on;
        PlayerPrefs.SetInt(KEY_SFX, on ? 1 : 0);
        PlayerPrefs.Save();
        UpdateSfxGain();
    }

    public void SetMusicVolume01(float v01)
    {
        _musicVolume = Mathf.Clamp01(v01);
        PlayerPrefs.SetFloat(KEY_MUSIC_VOL, _musicVolume);
        PlayerPrefs.Save();
        UpdateMusicGain();
        EnsureBgmPlaying();
    }

    public void SetSfxVolume01(float v01)
    {
        _sfxVolume = Mathf.Clamp01(v01);
        PlayerPrefs.SetFloat(KEY_SFX_VOL, _sfxVolume);
        PlayerPrefs.Save();
        UpdateSfxGain();
    }

    public float GetMusicVolume01() => _musicVolume;
    public float GetSfxVolume01() => _sfxVolume;
    public bool IsMusicEnabled() => _musicEnabled;
    public bool IsSfxEnabled() => _sfxEnabled;

    // ===== NEW: Play helpers với voice pool & cooldown =====
    private void PlayVar(AudioClip clip, float vol = 1f)
    {
        if (!_sfxEnabled || clip == null || _sfxVoices == null || _sfxVoices.Length == 0) return;

        // chống spam cùng clip quá dày
        float now = Time.unscaledTime;
        if (_lastPlayTime.TryGetValue(clip, out float last))
        {
            if ((now - last) * 1000f < sameClipCooldownMs) return;
        }
        _lastPlayTime[clip] = now;

        // round-robin voice
        var src = _sfxVoices[_sfxVoiceCursor];
        _sfxVoiceCursor = (_sfxVoiceCursor + 1) % _sfxVoices.Length;

        float p = 1f + Random.Range(-pitchJitter, pitchJitter);
        src.pitch = p;
        src.PlayOneShot(clip, vol);
        src.pitch = 1f;
    }

    public void PlayClick() => PlayVar(clickSfx, 0.9f);
    public void PlayStartGame() => PlayVar(startGameSfx, 1f);
    public void PlayPickup() => PlayVar(pickupSfx, 0.9f);     // NEW
    public void PlayDrop() => PlayVar(dropSfx, 1.0f);       // NEW

    public void PlayPraiseForLines(int linesCleared)
    {
        if (linesCleared < 2) return;
        AudioClip praise = linesCleared switch
        {
            2 => goodSfx,
            3 => greatSfx,
            4 => excellentSfx,
            5 => fantasticSfx,
            _ => legendarySfx
        };
        PlayVar(praise, 1f);
    }

    public void PlayUnbelievable() // NEW: bonus clear sạch bảng
    {
        PlayVar(unbelievableSfx, 1f);
    }

    public void PlayBgm(AudioClip clip, bool loop = true)
    {
        if (!musicSource) return;
        musicSource.loop = loop;
        musicSource.clip = clip ? clip : defaultBgm;
        EnsureBgmPlaying();
    }
    public void PlayComboTier(int comboLevel)
    {
        if (!_sfxEnabled || clearComboSfxByTier == null || clearComboSfxByTier.Length == 0) return;

        int idx = Mathf.Clamp(comboLevel - 1, 0, clearComboSfxByTier.Length - 1);
        var clip = clearComboSfxByTier[idx];
        PlayVar(clip, 1f);
    }

}
