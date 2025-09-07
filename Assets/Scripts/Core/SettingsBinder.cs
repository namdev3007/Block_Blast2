using UnityEngine;

public class SettingsBinder : MonoBehaviour
{
    [Header("Switches")]
    public UISwitcher swMusic;
    public UISwitcher swSfx;

    private void Start()
    {
        // Đọc từ PlayerPrefs (đã được AudioManager load sẵn), đồng bộ UI (instant, không animate)
        if (swMusic) swMusic.Set(AudioManagerState_MusicOn(), true);
        if (swSfx) swSfx.Set(AudioManagerState_SfxOn(), true);

        // Lắng nghe thay đổi để lưu + áp ngay
        if (swMusic) swMusic.onValueChanged.AddListener(on =>
        {
            AudioManager.Instance?.SetMusicEnabled(on);
        });
        if (swSfx) swSfx.onValueChanged.AddListener(on =>
        {
            AudioManager.Instance?.SetSfxEnabled(on);
        });
    }

    // Helpers lấy trạng thái hiện tại từ AudioManager
    private bool AudioManagerState_MusicOn()
        => PlayerPrefs.GetInt("am_music_on", 1) == 1;
    private bool AudioManagerState_SfxOn()
        => PlayerPrefs.GetInt("am_sfx_on", 1) == 1;
}
