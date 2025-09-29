using System;
using UnityEngine;

[Serializable]
public class GameSaveV1
{
    public int version = 1;

    // Board
    public int rows, cols;
    public bool[] occupied;
    public int[] variants;      // song song với occupied

    // Palette
    public int paletteSize;
    public int[] shapeIds;       // -1 nếu slot trống
    public int[] shapeVariants;  // biến thể skin theo slot

    // Score & flow
    public int scoreTotal;
    public int comboCurrent;
    public bool reviveUsed;

    // Meta
    public long unixSavedAt;
}

public static class SaveService
{
    const string KEY = "gamesave_v1";

    public static GameSaveV1 Capture(GameManager gm, BoardRuntime board, ShapePalette palette)
    {
        var s = new GameSaveV1();
        s.unixSavedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        // Board
        int W = board.gridView.columns;
        int H = board.gridView.rows;
        s.cols = W; s.rows = H;
        board.State.CopyTo(out s.occupied, out s.variants);

        // Palette
        int n = (palette.slots != null) ? palette.slots.Count : 3;
        s.paletteSize = n;
        s.shapeIds = new int[n];
        s.shapeVariants = new int[n];
        for (int i = 0; i < n; i++)
        {
            var d = palette.Peek(i);
            s.shapeIds[i] = (d != null) ? d.shapeId : -1; // yêu cầu ShapeData có shapeId
            s.shapeVariants[i] = (d != null) ? palette.PeekVariant(i) : 0;
        }

        // Score & flow
        if (gm.score != null)
        {
            s.scoreTotal = gm.score.Total;
            s.comboCurrent = gm.score.CurrentCombo;
        }
        s.reviveUsed = gm.ReviveUsed;   // property ở GameManager mục (3)

        return s;
    }

    public static void Save(GameSaveV1 data)
    {
        PlayerPrefs.SetString(KEY, JsonUtility.ToJson(data));
        PlayerPrefs.Save();
    }

    public static bool TryLoad(out GameSaveV1 data)
    {
        data = null;
        if (!PlayerPrefs.HasKey(KEY)) return false;
        data = JsonUtility.FromJson<GameSaveV1>(PlayerPrefs.GetString(KEY));
        return data != null;
    }

    public static void Clear() { PlayerPrefs.DeleteKey(KEY); }
}
