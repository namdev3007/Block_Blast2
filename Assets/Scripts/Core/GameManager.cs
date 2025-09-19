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

    [Header("Options")]
    public bool autoStartOnAwake = false;   // để FALSE: vào Home trước

    public GameState State { get; private set; } = GameState.Boot;

    public event Action<GameState> GameStateChanged;
    public event Action GameStarted;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
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
        SetState(GameState.Boot);      // dùng Boot như trạng thái Home
        ui?.ShowHome(true);
        ui?.ShowHUD(false);
        ui?.ShowSettingPanel(false);
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
        // Reset score
        if (score != null) score.ResetAll();

        // Reset board + seed
        if (board != null)
        {
            // xoá sạch
            board.SeedRandomOccupied(0, 0, true);
            // seed ban đầu nếu muốn
            if (board.seedAtStart)
                board.ResetAndSeed(board.initialMinOccupied, board.initialMaxOccupied, board.avoidFullRowsCols);

            // >>> Chỉ chạy wave sau khi bấm Start <<<
            board.PlayIntroWave(); // dùng config bên BoardRuntime
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
        State = s;
        GameStateChanged?.Invoke(State);
        ui?.OnGameStateChanged(State);
    }
}
