using System;
using System.Collections;
using UnityEngine;

public enum GameState { Boot, Playing, Paused, GameOver, BestScore }

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public BoardRuntime board;
    public ShapePalette palette;
    public GameScore score;
    public PopupManager popup;
    public UIManager ui;
    public RevivePanel revivePanel;

    public bool autoStartOnAwake = false;
    public float reviveCountdownSeconds = 5f;

    public float endWaveRowStep = 0.05f;
    public float endWaveColJitter = 0.01f;
    public float endWaveAlpha = 0.85f;
    public float endWaveExtraWait = 0.30f;
    public bool endWaveOverwriteOnOccupied = true;

    public float minLoadingSeconds = 1.2f;
    public bool preloadAudio = true;

    // CHANGED: không build grid ở boot
    public bool buildGridOnBoot = false;

    public AudioClip startGameSfx;

    public bool ReviveUsed => reviveUsed;
    public GameState State { get; private set; } = GameState.Boot;

    public event Action<GameState> GameStateChanged;
    public event Action GameStateWillChange;
    public event Action GameStarted;

    private bool reviveUsed = false;
    private int _hsAtRunStart;
    private bool _endFlowRunning;

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
        StartCoroutine(CoBootFlow());
    }

    private IEnumerator CoBootFlow()
    {
        Time.timeScale = 1f;
        ui?.ShowLoading(true);
        ui?.ShowHome(false);
        ui?.ShowHUD(false);
        ui?.ShowSettingPanel(false);
        ui?.ShowRevive(false);
        ui?.ShowGameOver(false);
        ui?.ShowBestScore(false);

        float t0 = Time.realtimeSinceStartup;
        int totalSteps = 2;
        int step = 0;

        yield return StartCoroutine(WarmUpAudio());
        step++; ui?.SetLoadingProgress01(step / (float)totalSteps);

        if (buildGridOnBoot && board != null)
        {
            board.EnsureGridBuilt();
            board.ShowBoard(true);
            yield return null;
        }
        step++; ui?.SetLoadingProgress01(step / (float)totalSteps);

        float elapsed = Time.realtimeSinceStartup - t0;
        if (elapsed < minLoadingSeconds) yield return new WaitForSecondsRealtime(minLoadingSeconds - elapsed);

        GoHome();
        ui?.ShowLoading(false);

        if (autoStartOnAwake) StartNewGame();
    }

    private IEnumerator WarmUpAudio()
    {
        if (!preloadAudio) yield break;
        yield return null;
        yield return null;
    }

    public void GoHome()
    {
        Time.timeScale = 1f;
        reviveUsed = false;
        _endFlowRunning = false;
        SetState(GameState.Boot);

        ui?.ShowHome(true);
        ui?.ShowHUD(false);
        ui?.ShowSettingPanel(false);
        ui?.ShowRevive(false);
        ui?.ShowGameOver(false);
        ui?.ShowBestScore(false);

        board?.ClearGameOverGhosts(true, 0f);
        board?.ShowBoard(false);   // CHANGED: ẩn board ở Home
        board?.ClearAllCells();    // CHANGED: dọn sạch hiển thị
    }

    public void OnStartButtonPressed()
    {
        AudioManager.Instance?.PlayClick();
        StartNewGame();
    }

    public void StartNewGame(int? seed = null)
    {
        SaveService.Clear();
        reviveUsed = false;
        _endFlowRunning = false;

        if (score != null) score.ResetAll();

        if (board != null)
        {
            board.ShowBoard(true);       // CHANGED: hiện board khi start
            board.EnsureGridBuilt();     // CHANGED: build lần đầu

            if (board.seedAtStart)
                board.ResetAndSeed(board.initialMinOccupied, board.initialMaxOccupied, board.avoidFullRowsCols);
            else
                board.SeedRandomOccupied(0, 0, true);

            board.PlayIntroWave();
            board.ClearGameOverGhosts(true, 0f);
        }

        if (palette != null) palette.Refill();

        _hsAtRunStart = (score != null) ? score.HighScore : 0;
        Time.timeScale = 1f;
        SetState(GameState.Playing);
        GameStarted?.Invoke();

        ui?.ShowHome(false);
        ui?.ShowHUD(true);
        ui?.ShowSettingPanel(false);
        ui?.ShowRevive(false);
        ui?.ShowGameOver(false);
        ui?.ShowBestScore(false);

        if (startGameSfx && AudioManager.Instance)
            AudioManager.Instance.startGameSfx = startGameSfx;
        AudioManager.Instance?.PlayStartGame();
    }

    public void SaveSnapshotNow()
    {
        var snap = SaveService.Capture(this, board, palette);
        SaveService.Save(snap);
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
        if (_endFlowRunning) return;
        if (score == null) { StartCoroutine(CoEndWaveThenGameOver()); return; }

        int cur = score.TotalScore;

        if (cur > _hsAtRunStart) { StartCoroutine(CoEndWaveThenBestScore()); return; }
        if (!ShouldOfferRevive()) { StartCoroutine(CoEndWaveThenGameOver()); return; }
        if (revivePanel == null || ui == null) { StartCoroutine(CoEndWaveThenGameOver()); return; }

        StartCoroutine(CoEndWaveThenRevive());
    }

    private IEnumerator CoEndWaveThenRevive()
    {
        _endFlowRunning = true;
        RunEndWaveOnce();
        yield return new WaitForSeconds(ComputeEndWaveDuration());
        Time.timeScale = 0f;
        ui.ShowRevive(true);
        revivePanel.transform.SetAsLastSibling();
        revivePanel.Show(reviveCountdownSeconds);
        _endFlowRunning = false;
    }

    private IEnumerator CoEndWaveThenGameOver()
    {
        _endFlowRunning = true;
        RunEndWaveOnce();
        yield return new WaitForSeconds(ComputeEndWaveDuration());
        GoGameOver();
        _endFlowRunning = false;
    }

    private IEnumerator CoEndWaveThenBestScore()
    {
        _endFlowRunning = true;
        RunEndWaveOnce();
        yield return new WaitForSeconds(ComputeEndWaveDuration());
        GoBestScore();
        _endFlowRunning = false;
    }

    private void RunEndWaveOnce()
    {
        if (board == null) return;
        Time.timeScale = 1f;
        board.ClearGameOverGhosts(true, 0f);
        board.PlayGameOverWave(endWaveRowStep, endWaveColJitter, endWaveAlpha, endWaveOverwriteOnOccupied);
    }

    private float ComputeEndWaveDuration()
    {
        int rows = (board != null && board.gridView != null) ? board.gridView.rows : 0;
        return Mathf.Max(0f, rows * endWaveRowStep + endWaveExtraWait);
    }

    private void OnReviveAccepted()
    {
        reviveUsed = true;
        ui?.ShowRevive(false);
        revivePanel?.Hide();
        board?.ClearGameOverGhosts(false, 0.2f);
        palette?.Refill();
        Time.timeScale = 1f;
        SetState(GameState.Playing);
        ui?.ShowHUD(true);
        AudioManager.Instance?.PlayStartGame();
    }

    private void OnReviveTimedOut()
    {
        ui?.ShowRevive(false);
        revivePanel?.Hide();
        GoGameOver();
    }

    private void GoGameOver()
    {
        revivePanel?.Hide();
        ui?.ShowRevive(false);
        Time.timeScale = 1f;
        SetState(GameState.GameOver);
        ui?.ShowGameOver(true);
    }

    private void GoBestScore()
    {
        revivePanel?.Hide();
        ui?.ShowRevive(false);
        Time.timeScale = 1f;
        SetState(GameState.BestScore);
        ui?.ShowGameOver(false);
        ui?.ShowBestScore(true);
    }

    public void ContinueFromSave()
    {
        if (!SaveService.TryLoad(out var s))
        {
            StartNewGame();
            return;
        }

        Time.timeScale = 1f;
        SetState(GameState.Playing);

        if (board != null)
        {
            board.EnsureGridBuiltForLoad();
            board.ShowBoard(true);
            board.LoadFromSave(s);
            board.ClearGameOverGhosts(true, 0f);
        }
        if (palette != null) palette.RestoreFromSave(s);
        if (score != null) score.SetTotalAndCombo(s.scoreTotal, s.comboCurrent);
        reviveUsed = s.reviveUsed;
        _hsAtRunStart = (score != null) ? score.HighScore : 0;

        ui?.ShowHome(false);
        ui?.ShowHUD(true);
        ui?.ShowSettingPanel(false);
        ui?.ShowRevive(false);
        ui?.ShowGameOver(false);
        ui?.ShowBestScore(false);
        GameStarted?.Invoke();
    }

    void OnApplicationPause(bool pause) { if (pause) SaveSnapshotNow(); }
    void OnApplicationQuit() { SaveSnapshotNow(); }

    private bool ShouldOfferRevive()
    {
        if (reviveUsed) return false;
        if (score == null) return false;
        int hs = score.HighScore;
        int cur = score.TotalScore;
        return cur > (hs * 0.5f);
    }
}
