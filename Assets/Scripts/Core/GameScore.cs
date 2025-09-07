using UnityEngine;
using System;

[System.Serializable]
public struct ScoreResult
{
    public int moveIndex;
    public int blockCells;
    public int linesCleared;
    public int comboBefore;     // cấp combo dùng để nhân điểm ở lượt này (không phải hệ số)
    public int comboAfter;      // cấp combo sau khi cập nhật
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

    [Header("Combo")]
    [Tooltip("Combo có thể tăng vô hạn.")]
    public bool unlimitedCombo = true;

    [Tooltip("Nhân điểm ô theo combo hay không.")]
    public bool applyComboToBlockCells = false;

    [Header("Runtime (read-only)")]
    [SerializeField] private int totalScore;
    [SerializeField] private int moveCount;
    [SerializeField] private int comboLevel = 1; // 1..∞ (cấp combo)
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
        bool armed = prevDry >= 3;   // đang trong trạng thái “cảnh báo” combo
        bool rescued = false;

        int comboUsed = comboLevel;

        if (armed)
        {
            if (linesCleared > 0) { rescued = true; /* giữ comboUsed như cũ */ }
            else { comboLevel = 1; comboUsed = comboLevel; }
        }

        float comboMult = ComboMultiplier(comboUsed);

        int lineBase = GetLineClearBase(linesCleared);
        int lineFinal = Mathf.RoundToInt(lineBase * comboMult);

        int blockFinal = blockCells;
        if (applyComboToBlockCells && linesCleared > 0)
            blockFinal = Mathf.RoundToInt(blockCells * comboMult);

        int gain = blockFinal + lineFinal;
        totalScore += gain;

        if (linesCleared > 0) { dryStreak = 0; comboLevel += 1; }
        else { dryStreak = prevDry + 1; }

        var result = new ScoreResult
        {
            moveIndex = moveCount,
            blockCells = blockCells,
            linesCleared = linesCleared,
            comboBefore = comboUsed,   // vẫn trả cấp combo
            comboAfter = comboLevel,   // cấp mới sau lượt này
            linePointsBase = lineBase,
            linePointsFinal = lineFinal,
            blockPoints = blockFinal,
            totalGain = gain,
            rescuedCombo = rescued,
            wasArmed = armed
        };

        Scored?.Invoke(result);
        return result;
    }

    private float ComboMultiplier(int c)
    {
        if (c <= 1) return 1f;
        if (c <= 4) return c + 1f;                  // 2→3×, 3→4×, 4→5×
        if (c <= 9) return 9f + 1.5f * (c - 5);     // 5→9×, 6→10.5×, ..., 9→15×
        return 22f + 2f * (c - 10);                 // 10→22×, 11→24×, ...
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
