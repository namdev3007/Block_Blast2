using UnityEngine;
using UnityEngine.UI;

public class UISettingManager : MonoBehaviour
{
    public Button btnExitSetting;
    public Button btnRestart;
    public Button btnHome;

    private void Awake()
    {
        if (btnExitSetting) btnExitSetting.onClick.AddListener(() =>
        {
            AudioManager.Instance?.PlayClick();
            GameManager.Instance?.Resume();
            GameManager.Instance?.ui?.ShowSettingPanel(false);
        });

        if (btnRestart) btnRestart.onClick.AddListener(() =>
        {
            AudioManager.Instance?.PlayClick();
            GameManager.Instance?.Restart();
            GameManager.Instance?.ui?.ShowSettingPanel(false);
        });

        if (btnHome) btnHome.onClick.AddListener(() =>
        {
            AudioManager.Instance?.PlayClick();
            GameManager.Instance?.GoHome();
        });
    }
}
