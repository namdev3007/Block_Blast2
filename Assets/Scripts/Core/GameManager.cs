using System;
using UnityEngine;

public enum GameState { Boot, Playing, Paused, GameOver }

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Refs")]
    public BoardRuntime board;
    public ShapePalette palette;
    public GameScore score;
    public PopupManager popup;   // optional
    public UIManager ui;         // gán trong Scene
    public RevivePanel revivePanel; // <-- kéo script RevivePanel vào đây trong Inspector

    [Header("Options")]
    public bool autoStartOnAwake = false;   // để FALSE: vào Home trước
    public float reviveCountdownSeconds = 5f;

    public GameState State { get; private set; } = GameState.Boot;

    public event Action<GameState> GameStateChanged;
    public event Action GameStateWillChange;
    public event Action GameStarted;

    // revive guard
    private bool reviveUsed = false;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        if (revivePanel != null)
        {
            revivePanel.Accepted += OnReviveAccepted;
            revivePanel.TimedOut += OnReviveTimedOut;
            revivePanel.gameObject.SetActive(false);
        }
    }

    void Start()
    {
        if (autoStartOnAwake) StartNewGame();
        else GoHome();
    }

    // ====== HOME ======
    public void GoHome()
    {
        Time.timeScale = 1f;
        reviveUsed = false;
        SetState(GameState.Boot);
        ui?.ShowHome(true);
        ui?.ShowHUD(false);
        ui?.ShowSettingPanel(false);
        ui?.ShowRevive(false);
        ui?.ShowGameOver(false);
    }

    // Gán hàm này vào nút Start ở màn hình Home
    public void OnStartButtonPressed()
    {
        AudioManager.Instance?.PlayClick();
        StartNewGame();
    }

    // ====== GAME FLOW ======
    public void StartNewGame(int? seed = null)
    {
        // Reset revive
        reviveUsed = false;

        // Reset score
        if (score != null) score.ResetAll();

        // Reset board + seed
        if (board != null)
        {
            board.SeedRandomOccupied(0, 0, true);
            if (board.seedAtStart)
                board.ResetAndSeed(board.initialMinOccupied, board.initialMaxOccupied, board.avoidFullRowsCols);

            board.PlayIntroWave();
        }

        // Refill hand
        if (palette != null) palette.Refill();

        Time.timeScale = 1f;
        SetState(GameState.Playing);
        GameStarted?.Invoke();

        // UI
        ui?.ShowHome(false);
        ui?.ShowHUD(true);
        ui?.ShowSettingPanel(false);
        ui?.ShowRevive(false);
        ui?.ShowGameOver(false);
    }

    public void Pause()
    {
        if (State != GameState.Playing) return;
        Time.timeScale = 0f;
        SetState(GameState.Paused);
    }

    public void Resume()
    {
        if (State != GameState.Paused) return;
        Time.timeScale = 1f;
        SetState(GameState.Playing);
    }

    public void Restart()
    {
        Time.timeScale = 1f;
        StartNewGame();
    }

    private void SetState(GameState s)
    {
        if (State == s) return;
        GameStateWillChange?.Invoke();
        State = s;
        GameStateChanged?.Invoke(State);
        ui?.OnGameStateChanged(State);
    }

    public void OnNoMovesLeft()
    {
        if (reviveUsed) { Debug.Log("[GM] Revive already used"); GoGameOver(); return; }
        if (revivePanel == null) { Debug.LogWarning("[GM] revivePanel NULL"); GoGameOver(); return; }
        if (ui == null) { Debug.LogWarning("[GM] ui NULL"); GoGameOver(); return; }

        Time.timeScale = 0f;
        ui.ShowHUD(false);
        ui.ShowRevive(true);
        revivePanel.transform.SetAsLastSibling();
        revivePanel.Show(reviveCountdownSeconds);
    }


    // ====== REVIVE HANDLERS ======
    private void OnReviveAccepted()
    {
        // chỉ được 1 lần trong cả ván
        reviveUsed = true;

        // Ẩn revive
        ui?.ShowRevive(false);
        revivePanel?.Hide();

        // Xoá ghost
        board?.ClearGameOverGhosts(instant: false, fadeOut: 0.2f);

        // Refill 3 block mới (giả định Refill sẽ lấp các slot trống tới 3)
        palette?.Refill();

        // Trở lại Playing
        Time.timeScale = 1f;
        SetState(GameState.Playing);
        ui?.ShowHUD(true);

        AudioManager.Instance?.PlayStartGame(); // nếu có SFX revive/start
    }

    private void OnReviveTimedOut()
    {
        // Hết giờ không bấm -> GameOver
        ui?.ShowRevive(false);
        revivePanel?.Hide();
        GoGameOver();
    }

    // ====== GAME OVER ======
    private void GoGameOver()
    {
        // đảm bảo dọn revive UI nếu đang mở
        revivePanel?.Hide();
        ui?.ShowRevive(false);

        Time.timeScale = 1f; // tuỳ bạn muốn giữ 0f hay 1f ở màn GameOver UI
        SetState(GameState.GameOver);

        ui?.ShowGameOver(true);
    }
}
