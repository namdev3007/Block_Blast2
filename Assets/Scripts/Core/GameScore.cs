using UnityEngine;
using System;

[System.Serializable]
public struct ScoreResult
{
    public int moveIndex;
    public int blockCells;
    public int linesCleared;
    public int comboBefore;     // combo dùng để nhân điểm ở lượt này
    public int comboAfter;      // combo sau khi cập nhật
    public int linePointsBase;
    public int linePointsFinal;
    public int blockPoints;
    public int totalGain;
    public bool rescuedCombo;   // có rescue khi đang armed hay không
    public bool wasArmed;       // trước lượt này có đang armed (đủ 3 miss) hay không
}

public class GameScore : MonoBehaviour
{
    // NEW: thông báo mỗi lần điểm thay đổi
    public event Action<ScoreResult> Scored;

    [Header("Combo")]
    [Tooltip("Combo có thể tăng vô hạn.")]
    public bool unlimitedCombo = true;

    [Tooltip("Nhân điểm ô theo combo hay không (mặc định KHÔNG theo spec).")]
    public bool applyComboToBlockCells = false;

    [Header("Runtime (read-only)")]
    [SerializeField] private int totalScore;
    [SerializeField] private int moveCount;
    [SerializeField] private int comboLevel = 1; // 1..∞
    [SerializeField] private int dryStreak = 0;

    public int TotalScore => totalScore;
    public int MoveCount => moveCount;
    public int ComboLevel => comboLevel;
    public int DryStreak => dryStreak;

    public void ResetAll()
    {
        totalScore = 0;
        moveCount = 0;
        comboLevel = 1;
        dryStreak = 0;
        // Optionally phát một event điểm 0
        Scored?.Invoke(new ScoreResult
        {
            moveIndex = moveCount,
            comboBefore = comboLevel,
            comboAfter = comboLevel
        });
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
            if (linesCleared > 0) { rescued = true; comboUsed = comboLevel; }
            else { comboLevel = 1; comboUsed = comboLevel; }
        }

        int comboMult = Mathf.Max(1, comboUsed);
        int lineBase = GetLineClearBase(linesCleared);
        int lineFinal = lineBase * comboMult;
        int blockFinal = (applyComboToBlockCells && linesCleared > 0) ? blockCells * comboMult : blockCells;

        int gain = blockFinal + lineFinal;
        totalScore += gain;

        if (linesCleared > 0) { dryStreak = 0; comboLevel += 1; }
        else { dryStreak = prevDry + 1; }

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

        // NEW: bắn event
        Scored?.Invoke(result);
        return result;
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
