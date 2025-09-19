using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    [Header("Panels")]
    public GameObject homePanel;     // Màn hình Home mới
    public GameObject hudPanel;
    public GameObject settingPanel;

    [Header("Buttons")]
    public Button btnStart;          // Nút Start trong Home
    public Button btnSetting;        // mở Setting khi đang chơi
    public Button btnExitSetting;    // đóng Setting
    public Button btnRestart;
    public Button btnHome;           // quay về Home

    private void Awake()
    {
        // Home Start
        if (btnStart)
            btnStart.onClick.AddListener(() => GameManager.Instance?.OnStartButtonPressed());

        // Open Settings
        if (btnSetting)
            btnSetting.onClick.AddListener(() =>
            {
                AudioManager.Instance?.PlayClick();
                GameManager.Instance?.Pause();
                ShowSettingPanel(true);
            });

        // Close Settings
        if (btnExitSetting)
            btnExitSetting.onClick.AddListener(() =>
            {
                AudioManager.Instance?.PlayClick();
                GameManager.Instance?.Resume();
                ShowSettingPanel(false);
            });

        // Restart
        if (btnRestart)
            btnRestart.onClick.AddListener(() =>
            {
                AudioManager.Instance?.PlayClick();
                GameManager.Instance?.Restart();
                ShowSettingPanel(false);
            });

        // Home
        if (btnHome)
            btnHome.onClick.AddListener(() =>
            {
                AudioManager.Instance?.PlayClick();
                GameManager.Instance?.GoHome();
            });
    }

    // ===== Public API =====
    public void OnGameStateChanged(GameState s)
    {
        switch (s)
        {
            case GameState.Boot:      // Home
                ShowHome(true);
                ShowHUD(false);
                ShowSettingPanel(false);
                break;

            case GameState.Playing:
                ShowHome(false);
                ShowHUD(true);
                ShowSettingPanel(false);
                break;

            case GameState.Paused:
                ShowHome(false);
                ShowHUD(true);
                ShowSettingPanel(true);
                break;

            case GameState.GameOver:
                ShowHome(false);
                ShowHUD(false);
                ShowSettingPanel(false);
                break;
        }
    }

    // ===== Toggle helpers =====
    public void ShowHome(bool on) => ToggleGO(homePanel, on);
    public void ShowHUD(bool on) => ToggleGO(hudPanel, on);
    public void ShowSettingPanel(bool on) => ToggleGO(settingPanel, on);

    private void ToggleGO(GameObject go, bool on)
    {
        if (!go) return;
        go.SetActive(on);
    }
}
