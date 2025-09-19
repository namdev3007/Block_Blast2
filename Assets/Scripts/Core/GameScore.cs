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

    [Header("Bonuses")]
    [Tooltip("Điểm thưởng khi sau lượt này bàn sạch hoàn toàn.")]
    public int boardClearBonus = 240;

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
        bool armed = prevDry >= 3;
        bool rescued = false;

        // combo dùng để tính điểm của lượt này (trước khi có thay đổi)
        int comboUsed = comboLevel;

        // Nếu đang armed:
        // - Clear được -> "rescue" (giữ combo hiện tại để nhân điểm, sau đó vẫn tăng như bình thường)
        // - Không clear -> mất combo về 1 (comboUsed cũng cập nhật về 1 để tính điểm lượt này)
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

        // Lưu lại combo cũ để kiểm tra thay đổi
        int oldCombo = comboLevel;

        // Cập nhật dry/combo sau khi chấm điểm
        if (linesCleared > 0)
        {
            dryStreak = 0;
            comboLevel += 1; // tăng combo khi clear thành công
        }
        else
        {
            dryStreak = prevDry + 1;
            // nếu không armed, combo giữ nguyên; nếu armed mà không clear thì đã reset ở trên
        }

        // CHỈ phát SFX khi combo thực sự thay đổi (tăng hoặc reset)
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
        return result;
    }

    public ScoreResult AwardBoardClearBonus(int? custom = null)
    {
        int bonus = custom ?? boardClearBonus;
        totalScore += bonus;

        var result = new ScoreResult
        {
            moveIndex = moveCount, // vẫn là lượt hiện tại
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
