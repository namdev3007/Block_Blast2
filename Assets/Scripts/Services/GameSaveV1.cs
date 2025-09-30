using System;
using UnityEngine;

[Serializable]
public class GameSaveV1
{
    public int version = 1;
    public int rows, cols;
    public bool[] occupied;
    public int[] variants;
    public int paletteSize;
    public int[] shapeIds;
    public int[] shapeVariants;
    public int scoreTotal;
    public int comboCurrent;
    public bool reviveUsed;
    public long unixSavedAt;
}

public static class SaveService
{
    const string KEY = "gamesave_v1";

    public static GameSaveV1 Capture(GameManager gm, BoardRuntime board, ShapePalette palette)
    {
        var s = new GameSaveV1();
        s.version = 1;
        s.unixSavedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        if (board != null && board.gridView != null)
        {
            s.cols = board.gridView.columns;
            s.rows = board.gridView.rows;

            if (board.State != null)
            {
                board.State.CopyTo(out s.occupied, out s.variants);
            }
            else
            {
                s.occupied = new bool[s.rows * s.cols];
                s.variants = new int[s.rows * s.cols];
            }
        }
        else
        {
            s.cols = s.rows = 0;
            s.occupied = new bool[0];
            s.variants = new int[0];
        }

        int n = (palette != null && palette.slots != null) ? palette.slots.Count : 0;
        s.paletteSize = n;
        s.shapeIds = new int[n];
        s.shapeVariants = new int[n];

        if (palette != null)
        {
            for (int i = 0; i < n; i++)
            {
                var d = palette.Peek(i);
                s.shapeIds[i] = (d != null) ? d.shapeId : -1;
                s.shapeVariants[i] = (d != null) ? palette.PeekVariant(i) : 0;
            }
        }

        if (gm != null && gm.score != null)
        {
            s.scoreTotal = gm.score.Total;
            s.comboCurrent = gm.score.CurrentCombo;
        }
        s.reviveUsed = (gm != null) && gm.ReviveUsed;

        return s;
    }

    public static void Save(GameSaveV1 data)
    {
        if (data == null) return;
        PlayerPrefs.SetString(KEY, JsonUtility.ToJson(data));
        PlayerPrefs.Save();
    }

    public static bool TryLoad(out GameSaveV1 data)
    {
        data = null;
        if (!PlayerPrefs.HasKey(KEY)) return false;

        string json = PlayerPrefs.GetString(KEY);
        if (string.IsNullOrEmpty(json)) return false;

        data = JsonUtility.FromJson<GameSaveV1>(json);
        return data != null;
    }

    public static void Clear()
    {
        PlayerPrefs.DeleteKey(KEY);
    }
}
