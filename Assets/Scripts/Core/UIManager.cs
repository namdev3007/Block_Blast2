using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    [Header("Panels")]
    public GameObject homePanel;
    public GameObject hudPanel;
    public GameObject settingPanel;
    public GameObject revivePanel;
    public GameObject gameOverPanel;

    [Header("Buttons")]
    public Button btnStart;
    public Button btnSetting;
    public Button btnExitSetting;
    public Button btnRestart;
    public Button btnHome;

    private void Awake()
    {
        if (btnStart) btnStart.onClick.AddListener(() => GameManager.Instance?.OnStartButtonPressed());
        if (btnSetting) btnSetting.onClick.AddListener(() =>
        {
            AudioManager.Instance?.PlayClick();
            GameManager.Instance?.Pause();
            ShowSettingPanel(true);
        });
        if (btnExitSetting) btnExitSetting.onClick.AddListener(() =>
        {
            AudioManager.Instance?.PlayClick();
            GameManager.Instance?.Resume();
            ShowSettingPanel(false);
        });
        if (btnRestart) btnRestart.onClick.AddListener(() =>
        {
            AudioManager.Instance?.PlayClick();
            GameManager.Instance?.Restart();
            ShowSettingPanel(false);
        });
        if (btnHome) btnHome.onClick.AddListener(() =>
        {
            AudioManager.Instance?.PlayClick();
            GameManager.Instance?.GoHome();
        });

        // Khởi tạo ẩn
        ShowRevive(false);
        ShowGameOver(false);
    }

    // ===== Public API =====
    public void OnGameStateChanged(GameState s)
    {
        switch (s)
        {
            case GameState.Boot:
                ShowHome(true);
                ShowHUD(false);
                ShowSettingPanel(false);
                ShowRevive(false);
                ShowGameOver(false);
                break;

            case GameState.Playing:
                ShowHome(false);
                ShowHUD(true);
                ShowSettingPanel(false);
                ShowRevive(false);
                ShowGameOver(false);
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
    public void ShowRevive(bool on) => ToggleGO(revivePanel, on);
    public void ShowGameOver(bool on) => ToggleGO(gameOverPanel, on);

    private void ToggleGO(GameObject go, bool on)
    {
        if (!go) return;
        go.SetActive(on);
    }
}
