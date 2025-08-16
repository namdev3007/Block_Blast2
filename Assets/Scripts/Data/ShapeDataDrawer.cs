using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(ShapeData), false)]
[CanEditMultipleObjects]
public class ShapeDataDrawer : Editor
{
    private ShapeData D => (ShapeData)target;

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.LabelField("Grid", EditorStyles.boldLabel);

        Undo.RecordObject(D, "Edit ShapeData");

        int oldCols = D.columns;
        int oldRows = D.rows;

        D.columns = Mathf.Max(1, EditorGUILayout.IntField("Columns", D.columns));
        D.rows = Mathf.Max(1, EditorGUILayout.IntField("Rows", D.rows));

        // Luôn đảm bảo ma trận đúng kích thước & giữ dữ liệu cũ
        if (D.board == null || D.board.Length != D.rows || D.columns != oldCols || D.rows != oldRows)
        {
            EnsureBoardShape(D);
        }

        if (GUILayout.Button("Clear Board")) { D.Clear(); }

        EditorGUILayout.Space();
        if (D.board != null && D.columns > 0 && D.rows > 0)
            DrawBoardTable(D);

        if (GUI.changed) EditorUtility.SetDirty(D);
        serializedObject.ApplyModifiedProperties();
    }

    /// Tạo ma trận mới đúng kích thước và copy dữ liệu cũ vào (nếu có)
    private void EnsureBoardShape(ShapeData data)
    {
        if (data.columns <= 0 || data.rows <= 0)
        {
            data.board = null;
            return;
        }

        var oldBoard = data.board;
        var newBoard = new ShapeData.Row[data.rows];

        for (int r = 0; r < data.rows; r++)
        {
            newBoard[r] = new ShapeData.Row(data.columns);
        }

        if (oldBoard != null)
        {
            int copyRows = Mathf.Min(data.rows, oldBoard.Length);
            for (int r = 0; r < copyRows; r++)
            {
                var oldRow = oldBoard[r];
                if (oldRow?.column == null) continue;

                int copyCols = Mathf.Min(data.columns, oldRow.column.Length);
                for (int c = 0; c < copyCols; c++)
                {
                    newBoard[r].column[c] = oldRow.column[c];
                }
            }
        }

        data.board = newBoard;
    }

    private void DrawBoardTable(ShapeData data)
    {
        // Phòng thủ thêm để không vỡ nếu ai đó sửa rows/cols trong lúc vẽ
        if (data.board == null || data.board.Length != data.rows)
            EnsureBoardShape(data);

        const int cell = 22;

        var cellStyle = new GUIStyle(EditorStyles.miniButtonMid)
        {
            fixedWidth = cell,
            fixedHeight = cell,
            margin = new RectOffset(0, 0, 0, 0),
            padding = new RectOffset(0, 0, 0, 0),
            // Dùng trạng thái on/off sẵn có để thấy toggle
        };
        cellStyle.normal.background = Texture2D.grayTexture;
        cellStyle.onNormal.background = Texture2D.whiteTexture;

        int gap = CmToPoints(0.1f);
        gap = Mathf.Max(1, gap);

        int oldIndent = EditorGUI.indentLevel;
        EditorGUI.indentLevel = 0;

        EditorGUILayout.BeginVertical("box");
        for (int r = 0; r < data.rows; r++)
        {
            // Đảm bảo hàng hợp lệ & đúng chiều dài cột
            if (data.board[r] == null) data.board[r] = new ShapeData.Row(data.columns);
            if (data.board[r].column == null || data.board[r].column.Length != data.columns)
                data.board[r].CreateRow(data.columns);

            EditorGUILayout.BeginHorizontal(GUILayout.ExpandWidth(false));

            for (int c = 0; c < data.columns; c++)
            {
                bool v = GUILayout.Toggle(
                    data.board[r].column[c],
                    GUIContent.none,
                    cellStyle,
                    GUILayout.Width(cell),
                    GUILayout.Height(cell)
                );
                data.board[r].column[c] = v;

                if (c < data.columns - 1) GUILayout.Space(gap);
            }

            EditorGUILayout.EndHorizontal();

            if (r < data.rows - 1) GUILayout.Space(gap);
        }
        EditorGUILayout.EndVertical();

        EditorGUI.indentLevel = oldIndent;
    }

    private int CmToPoints(float cm)
    {
        const float inchPerCm = 0.393700787f;
        const float DPI = 96f;
        float pixels = cm * inchPerCm * DPI;
        float points = pixels / EditorGUIUtility.pixelsPerPoint;
        return Mathf.RoundToInt(points);
    }
}
