using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(ShapeData), false)]
[CanEditMultipleObjects]
public class ShapeDataDrawer : Editor
{
    private ShapeData D => (ShapeData)target;
    SerializedProperty spSprite;

    void OnEnable()
    {
        spSprite = serializedObject.FindProperty("blockSprite");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.LabelField("Visual", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(spSprite, new GUIContent("Block Sprite"));
        EditorGUILayout.Space();

        if (GUILayout.Button("Clear Board")) D.Clear();
        EditorGUILayout.Space();

        var c0 = D.columns; var r0 = D.rows;
        D.columns = EditorGUILayout.IntField("Columns", D.columns);
        D.rows = EditorGUILayout.IntField("Rows", D.rows);
        if ((D.columns != c0 || D.rows != r0) && D.columns > 0 && D.rows > 0)
            D.CreateNewBoard();

        EditorGUILayout.Space();
        if (D.board != null && D.columns > 0 && D.rows > 0)
            DrawBoardTable(D);

        serializedObject.ApplyModifiedProperties();
        if (GUI.changed) EditorUtility.SetDirty(D);
    }

    private void DrawBoardTable(ShapeData data)
    {
        const int cell = 22; 

        var cellStyle = new GUIStyle(EditorStyles.miniButtonMid);
        cellStyle.fixedWidth = cell;
        cellStyle.fixedHeight = cell;
        cellStyle.margin = new RectOffset(0, 0, 0, 0);
        cellStyle.padding = new RectOffset(0, 0, 0, 0);
        cellStyle.normal.background = Texture2D.grayTexture;
        cellStyle.onNormal.background = Texture2D.whiteTexture;

        int gap = CmToPoints(0.1f); 
        gap = Mathf.Max(1, gap);          

        int oldIndent = EditorGUI.indentLevel;
        EditorGUI.indentLevel = 0;

        EditorGUILayout.BeginVertical("box");
        for (int r = 0; r < data.rows; r++)
        {
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

                if (c < data.columns - 1)
                    GUILayout.Space(gap); 
            }

            EditorGUILayout.EndHorizontal();

            if (r < data.rows - 1)
                GUILayout.Space(gap);
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
    