using UnityEngine;
using UnityEngine.Audio;
using System.Collections;
using System.Collections.Generic;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    public AudioClip defaultBgm;

    public AudioMixer mixer;
    public string bgmVolumeParam = "BGMVol";
    public string sfxVolumeParam = "SFXVol";
    public AudioMixerGroup musicGroup;
    public AudioMixerGroup sfxGroup;

    public AudioClip clickSfx;
    public AudioClip startGameSfx;
    public AudioClip increaseSfx;
    public AudioClip pickupSfx;
    public AudioClip dropSfx;
    public AudioClip[] clearComboSfxByTier;
    public AudioClip goodSfx;
    public AudioClip greatSfx;
    public AudioClip excellentSfx;
    public AudioClip fantasticSfx;
    public AudioClip legendarySfx;
    public AudioClip unbelievableSfx;

    public AudioSource musicSource;
    public AudioSource sfxSource;

    public int sfxPolyphony = 8;
    public int sameClipCooldownMs = 35;
    public float pitchJitter = 0.08f;

    public const string KEY_MUSIC = "Audio.MusicEnabled";
    public const string KEY_SFX = "Audio.SfxEnabled";
    public const string KEY_MUSIC_VOL = "Audio.MusicVol01";
    public const string KEY_SFX_VOL = "Audio.SfxVol01";

    const float DEFAULT_MUSIC_VOL_01 = 0.3162278f;

    private bool _musicEnabled = true;
    private bool _sfxEnabled = true;
    [Range(0f, 1f)][SerializeField] private float _musicVolume = DEFAULT_MUSIC_VOL_01;
    [Range(0f, 1f)][SerializeField] private float _sfxVolume = 1f;

    private AudioSource[] _sfxVoices;
    private int _sfxVoiceCursor = 0;
    private readonly Dictionary<AudioClip, float> _lastPlayTime = new();

    void Awake()
    {
        if (Instance && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        _musicEnabled = PlayerPrefs.GetInt(KEY_MUSIC, 1) == 1;
        _sfxEnabled = PlayerPrefs.GetInt(KEY_SFX, 1) == 1;
        _musicVolume = PlayerPrefs.GetFloat(KEY_MUSIC_VOL, DEFAULT_MUSIC_VOL_01);
        _sfxVolume = PlayerPrefs.GetFloat(KEY_SFX_VOL, 1f);

        if (musicSource && musicGroup) musicSource.outputAudioMixerGroup = musicGroup;
        if (sfxSource && sfxGroup) sfxSource.outputAudioMixerGroup = sfxGroup;

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
        PlayBgm(defaultBgm, true); // thay vì PlayMenuBgm()
    }

    private void EnsureBgmPlaying()
    {
        if (!musicSource) return;
        if (!musicSource.clip) musicSource.clip = defaultBgm;
        musicSource.loop = true;
        if (_musicEnabled && musicSource.clip && !musicSource.isPlaying)
        {
            AudioListener.pause = false;
            musicSource.Play();
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

    public void SetMusicEnabled(bool on)
    {
        _musicEnabled = on;
        PlayerPrefs.SetInt(KEY_MUSIC, on ? 1 : 0);
        PlayerPrefs.Save();
        UpdateMusicGain();
        if (!musicSource) return;
        if (on) EnsureBgmPlaying(); else musicSource.Pause();
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

    private void PlayVar(AudioClip clip, float vol = 1f)
    {
        if (!_sfxEnabled || clip == null || _sfxVoices == null || _sfxVoices.Length == 0) return;

        float now = Time.unscaledTime;
        if (_lastPlayTime.TryGetValue(clip, out float last))
        {
            if ((now - last) * 1000f < sameClipCooldownMs) return;
        }
        _lastPlayTime[clip] = now;

        var src = _sfxVoices[_sfxVoiceCursor];
        _sfxVoiceCursor = (_sfxVoiceCursor + 1) % _sfxVoices.Length;

        float p = 1f + Random.Range(-pitchJitter, pitchJitter);
        src.pitch = p;
        src.PlayOneShot(clip, vol);
        src.pitch = 1f;
    }

    public void PlayClick() => PlayVar(clickSfx, 0.9f);
    public void PlayStartGame() => PlayVar(startGameSfx, 1f);
    public void PlayIncrease() => PlayVar(increaseSfx, 1f);
    public void PlayPickup() => PlayVar(pickupSfx, 0.9f);
    public void PlayDrop() => PlayVar(dropSfx, 1f);

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

    public void PlayUnbelievable()
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
