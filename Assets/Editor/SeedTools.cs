// Assets/Editor/SeedTools.cs
using UnityEditor;
using UnityEngine;

public static class SeedTools
{
    [MenuItem("Assets/Create/BlockBlast/Seed (8x8)", priority = 0)]
    public static void CreateSeed8x8()
    {
        var asset = ScriptableObject.CreateInstance<ShapeData>();
        asset.rows = 8;
        asset.columns = 8;
        asset.CreateNewBoard();

        string path = AssetDatabase.GenerateUniqueAssetPath("Assets/Seed_8x8.asset");
        AssetDatabase.CreateAsset(asset, path);
        AssetDatabase.SaveAssets();
        EditorUtility.FocusProjectWindow();
        Selection.activeObject = asset;
    }
}
