using System;
using System.Collections.Generic;
using UnityEngine;

public enum GameState { Boot, Playing, Paused, GameOver }

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Refs")]
    public BoardRuntime board;
    public ShapePalette palette;
    public GameScore score;
    public PopupManager popup;   // có thể null nếu bạn đã hiển thị popup từ ShapeDragItem
    public UIManager ui;         // gán trong Scene

    [Header("Options")]
    public bool autoStartOnAwake = true;

    public GameState State { get; private set; } = GameState.Boot;

    // Sự kiện cho UI/khác
    public event Action<GameState> GameStateChanged;
    public event Action GameStarted;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        if (autoStartOnAwake)
            StartNewGame();
    }

    // ===== API chính =====
    public void StartNewGame(int? seed = null)
    {
        // Reset score
        if (score != null) score.ResetAll();

        // Reset board (seed tuỳ ý)
        if (board != null)
        {
            // Xoá sạch
            board.SeedRandomOccupied(0, 0, true);
            // Nếu muốn seed ban đầu:
            if (board.seedAtStart)
                board.SeedRandomOccupied(board.initialMinOccupied, board.initialMaxOccupied, board.avoidFullRowsCols);
        }

        // Refill hand
        if (palette != null)
            palette.Refill();

        SetState(GameState.Playing);
        GameStarted?.Invoke();
        ui?.ShowHUD(true);
        //ui?.ShowGameOverPanel(false);
    }

    public void Pause()
    {
        if (State != GameState.Playing) return;
        SetState(GameState.Paused);
        Time.timeScale = 0f;
        ui?.ShowPausePanel(true);
    }

    public void Resume()
    {
        if (State != GameState.Paused) return;
        Time.timeScale = 1f;
        ui?.ShowPausePanel(false);
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
