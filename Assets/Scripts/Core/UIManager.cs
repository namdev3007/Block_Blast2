using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    [Header("Panels (as GameObject)")]
    public GameObject hudPanel;
    public GameObject pausePanel;

    [Header("Buttons")]
    public Button btnPause;

    private void Awake()
    {
        if (btnPause)
        {
            btnPause.onClick.AddListener(() =>
            {
                AudioManager.Instance?.PlayClick();   // SFX click
                GameManager.Instance?.Pause();
            });
        }
    }

    public void OnGameStateChanged(GameState s)
    {
        switch (s)
        {
            case GameState.Playing:
                ShowHUD(true);
                ShowPausePanel(false);
                break;

            case GameState.Paused:
                ShowHUD(true);
                ShowPausePanel(true);
                break;

            case GameState.GameOver:
                ShowHUD(false);
                ShowPausePanel(false);
                break;
        }
    }

    // ===== Hiển thị (GameObject) =====
    public void ShowHUD(bool on) => ToggleGO(hudPanel, on);
    public void ShowPausePanel(bool on) => ToggleGO(pausePanel, on);

    private void ToggleGO(GameObject go, bool on)
    {
        if (!go) return;
        go.SetActive(on);
    }
}
