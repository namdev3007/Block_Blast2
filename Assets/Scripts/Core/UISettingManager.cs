using UnityEngine;
using UnityEngine.UI;

public class UISettingManager : MonoBehaviour
{
    [Header("Buttons")]
    public Button btnResume;
    public Button bntAudio;   // Toggle SFX
    public Button bntMusic;   // Toggle Music
    public Button bntVibrate;
    public Button bntHome;
    public Button btnRestart;

    private void Awake()
    {
        if (btnResume)
            btnResume.onClick.AddListener(() =>
            {
                AudioManager.Instance?.PlayClick();
                GameManager.Instance?.Resume();
            });

        if (btnRestart)
            btnRestart.onClick.AddListener(() =>
            {
                AudioManager.Instance?.PlayClick();
                GameManager.Instance?.Restart();
            });

        if (bntAudio)
            bntAudio.onClick.AddListener(() =>
            {
                AudioManager.Instance?.PlayClick();
                AudioManager.Instance?.ToggleSfx();
                RefreshBtnLabel(bntAudio, AudioManager.Instance && AudioManager.Instance.SfxEnabled);
            });

        if (bntMusic)
            bntMusic.onClick.AddListener(() =>
            {
                AudioManager.Instance?.PlayClick();
                AudioManager.Instance?.ToggleMusic();
                RefreshBtnLabel(bntMusic, AudioManager.Instance && AudioManager.Instance.MusicEnabled);
            });

        if (bntVibrate)
            bntVibrate.onClick.AddListener(() => AudioManager.Instance?.PlayClick());

        if (bntHome)
            bntHome.onClick.AddListener(() => AudioManager.Instance?.PlayClick());
    }

    private void Start()
    {
        // đồng bộ label ban đầu theo trạng thái AudioManager
        if (AudioManager.Instance)
        {
            RefreshBtnLabel(bntAudio, AudioManager.Instance.SfxEnabled);
            RefreshBtnLabel(bntMusic, AudioManager.Instance.MusicEnabled);
        }
    }

    private void RefreshBtnLabel(Button btn, bool isOn)
    {
        if (!btn) return;

        // Nếu button có child Text (UGUI) → đổi text; 
        // Nếu dùng TextMeshProUGUI thì bạn có thể thay GetComponentInChildren<TMP_Text>().
        var txt = btn.GetComponentInChildren<UnityEngine.UI.Text>();
        if (txt != null)
        {
            // Ví dụ: "SFX: On/Off" hoặc "Music: On/Off"
            // Nếu bạn đặt sẵn text "SFX" hoặc "Music" trên nút, ta giữ prefix đó.
            string prefix = txt.text;
            int colon = prefix.IndexOf(':');
            if (colon >= 0) prefix = prefix.Substring(0, colon);
            txt.text = $"{prefix}: {(isOn ? "On" : "Off")}";
        }
    }
}
