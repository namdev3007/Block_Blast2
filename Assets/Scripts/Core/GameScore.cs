using UnityEngine;
using System;

[Serializable]
public struct ScoreResult
{
    public int moveIndex;
    public int blockCells;
    public int linesCleared;
    public int comboBefore;
    public int comboAfter;
    public int linePointsBase;
    public int linePointsFinal;
    public int blockPoints;
    public int totalGain;
    public bool rescuedCombo;
    public bool wasArmed;
}

public class GameScore : MonoBehaviour
{
    public event Action<ScoreResult> Scored;
    public event Action<int> HighScoreChanged;

    [Header("Options")]
    public bool unlimitedCombo = true;
    public bool applyComboToBlockCells = false;
    public int boardClearBonus = 240;

    [Header("Persistence")]
    [SerializeField] private string highScoreKey = "HighScore";
    [SerializeField] private int highScore;

    [SerializeField] private int totalScore;
    [SerializeField] private int moveCount;
    [SerializeField] private int comboLevel = 1;
    [SerializeField] private int dryStreak = 0;

    public int TotalScore => totalScore;
    public int MoveCount => moveCount;
    public int ComboLevel => comboLevel;
    public int DryStreak => dryStreak;
    public int HighScore => highScore;

    public int Total => TotalScore;
    public int CurrentCombo => ComboLevel;

    private void Awake()
    {
        highScore = PlayerPrefs.GetInt(highScoreKey, 0);
    }

    public void SetTotalAndCombo(int total, int combo)
    {
        totalScore = Mathf.Max(0, total);
        comboLevel = Mathf.Max(1, combo);

        Scored?.Invoke(new ScoreResult
        {
            moveIndex = moveCount,
            blockCells = 0,
            linesCleared = 0,
            comboBefore = comboLevel,
            comboAfter = comboLevel,
            linePointsBase = 0,
            linePointsFinal = 0,
            blockPoints = 0,
            totalGain = 0,
            rescuedCombo = false,
            wasArmed = false
        });
    }

    public void ResetAll()
    {
        totalScore = 0;
        moveCount = 0;
        comboLevel = 1;
        dryStreak = 0;

        Scored?.Invoke(new ScoreResult
        {
            moveIndex = moveCount,
            comboBefore = comboLevel,
            comboAfter = comboLevel
        });
    }

    public void ResetHighScore()
    {
        highScore = 0;
        PlayerPrefs.SetInt(highScoreKey, 0);
        PlayerPrefs.Save();
        HighScoreChanged?.Invoke(highScore);
    }

    public ScoreResult OnPiecePlaced(int blockCells, int linesCleared)
    {
        moveCount++;

        int prevDry = dryStreak;
        bool armed = prevDry >= 3;
        bool rescued = false;

        int comboUsed = comboLevel;

        if (armed)
        {
            if (linesCleared > 0)
            {
                rescued = true;
            }
            else
            {
                comboLevel = 1;
                comboUsed = comboLevel;
            }
        }

        float comboMult = ComboMultiplier(comboUsed);

        int lineBase = GetLineClearBase(linesCleared);
        int lineFinal = Mathf.RoundToInt(lineBase * comboMult);

        int blockFinal = blockCells;
        if (applyComboToBlockCells && linesCleared > 0)
            blockFinal = Mathf.RoundToInt(blockCells * comboMult);

        int gain = blockFinal + lineFinal;
        totalScore += gain;

        int oldCombo = comboLevel;

        if (linesCleared > 0)
        {
            dryStreak = 0;
            comboLevel = unlimitedCombo ? (comboLevel + 1) : Mathf.Min(comboLevel + 1, 9999);
        }
        else
        {
            dryStreak = prevDry + 1;
        }

        if (comboLevel != oldCombo && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayComboTier(comboLevel);
        }

        var result = new ScoreResult
        {
            moveIndex = moveCount,
            blockCells = blockCells,
            linesCleared = linesCleared,
            comboBefore = comboUsed,
            comboAfter = comboLevel,
            linePointsBase = lineBase,
            linePointsFinal = lineFinal,
            blockPoints = blockFinal,
            totalGain = gain,
            rescuedCombo = rescued,
            wasArmed = armed
        };

        Scored?.Invoke(result);
        TryUpdateHighScore();
        return result;
    }

    public ScoreResult AwardBoardClearBonus(int? custom = null)
    {
        int bonus = custom ?? boardClearBonus;
        totalScore += bonus;

        var result = new ScoreResult
        {
            moveIndex = moveCount,
            blockCells = 0,
            linesCleared = 0,
            comboBefore = comboLevel,
            comboAfter = comboLevel,
            linePointsBase = 0,
            linePointsFinal = 0,
            blockPoints = 0,
            totalGain = bonus,
            rescuedCombo = false,
            wasArmed = false
        };

        Scored?.Invoke(result);
        TryUpdateHighScore();
        return result;
    }

    private void TryUpdateHighScore()
    {
        if (totalScore > highScore)
        {
            highScore = totalScore;
            PlayerPrefs.SetInt(highScoreKey, highScore);
            PlayerPrefs.Save();
            HighScoreChanged?.Invoke(highScore);
        }
    }

    private float ComboMultiplier(int c)
    {
        if (c <= 1) return 1f;
        if (c <= 4) return c + 1f;
        if (c <= 9) return 9f + 1.5f * (c - 5);
        return 22f + 2f * (c - 10);
    }

    private int GetLineClearBase(int lines)
    {
        switch (lines)
        {
            case 1: return 10;
            case 2: return 20;
            case 3: return 60;
            case 4: return 140;
            case 5: return 220;
            default: return (lines <= 0) ? 0 : 300;
        }
    }
}
