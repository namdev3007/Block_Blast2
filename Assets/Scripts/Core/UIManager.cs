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
    public GameObject bestScorePanel;

    [Header("Loading")]
    public LoadingPanel loadingPanel;

    [Header("Buttons")]
    public Button btnStart;
    public Button btnSetting;

    private void Awake()
    {
        if (btnStart) btnStart.onClick.AddListener(() =>
        {
            AudioManager.Instance?.PlayClick();
            GameManager.Instance?.ContinueFromSave();
        });

        if (btnSetting) btnSetting.onClick.AddListener(() =>
        {
            AudioManager.Instance?.PlayClick();
            GameManager.Instance?.Pause();
            ShowSettingPanel(true);
        });

        ShowRevive(false);
        ShowGameOver(false);
        ShowBestScore(false);
        ShowLoading(true);
        ShowHome(false);
        ShowHUD(false);
    }

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
                ShowBestScore(false);
                break;
            case GameState.Playing:
                ShowHome(false);
                ShowHUD(true);
                ShowSettingPanel(false);
                ShowRevive(false);
                ShowGameOver(false);
                ShowBestScore(false);
                break;
            case GameState.Paused:
                ShowHome(false);
                ShowHUD(true);
                ShowSettingPanel(true);
                break;
            case GameState.GameOver:
                ShowHome(false);
                ShowHUD(true);
                ShowSettingPanel(false);
                break;
            case GameState.BestScore:
                ShowHome(false);
                ShowHUD(true);
                ShowSettingPanel(false);
                ShowRevive(false);
                ShowGameOver(false);
                ShowBestScore(true);
                break;
        }
    }

    public void ShowHome(bool on) => ToggleGO(homePanel, on);
    public void ShowHUD(bool on) => ToggleGO(hudPanel, on);
    public void ShowSettingPanel(bool on) => ToggleGO(settingPanel, on);
    public void ShowRevive(bool on) => ToggleGO(revivePanel, on);
    public void ShowGameOver(bool on) => ToggleGO(gameOverPanel, on);
    public void ShowBestScore(bool on) => ToggleGO(bestScorePanel, on);

    public void ShowLoading(bool on)
    {
        if (loadingPanel) loadingPanel.Show(on);
    }

    public void SetLoadingProgress01(float p)
    {
        if (loadingPanel) loadingPanel.SetProgress01(p);
    }

    private void ToggleGO(GameObject go, bool on)
    {
        if (!go) return;
        go.SetActive(on);
    }
}
