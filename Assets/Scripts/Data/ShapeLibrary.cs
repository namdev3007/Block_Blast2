using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ShapeLibrary", menuName = "Data/Shape Library")]
public class ShapeLibrary : ScriptableObject
{
    public List<ShapeData> shapes = new List<ShapeData>();

    public ShapeData GetRandom()
    {
        if (shapes == null || shapes.Count == 0) return null;
        return shapes[Random.Range(0, shapes.Count)];
    }
}
