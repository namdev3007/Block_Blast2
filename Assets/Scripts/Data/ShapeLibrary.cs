using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ShapeLibrary", menuName = "Data/Shape Library")]
public class ShapeLibrary : ScriptableObject
{
    public List<ShapeData> shapes = new List<ShapeData>();

    // cache id -> shape
    private Dictionary<int, ShapeData> _byId;

    private void OnEnable() => RebuildIndex();
#if UNITY_EDITOR
    private void OnValidate() => RebuildIndex();
#endif

    public ShapeData GetRandom()
    {
        if (shapes == null || shapes.Count == 0) return null;
        return shapes[Random.Range(0, shapes.Count)];
    }

    public ShapeData GetById(int id)
    {
        if (_byId == null) RebuildIndex();
        if (_byId != null && _byId.TryGetValue(id, out var s)) return s;
        return null;
    }

    public void RebuildIndex()
    {
        _byId = new Dictionary<int, ShapeData>();
        if (shapes == null) return;

        // Đảm bảo mỗi shape có ID hợp lệ & duy nhất.
        // Quy ước: nếu shape.shapeId < 0, auto-assign theo index.
        var used = new HashSet<int>();
        for (int i = 0; i < shapes.Count; i++)
        {
            var s = shapes[i];
            if (!s) continue;

            if (s.shapeId < 0) s.shapeId = i;              // auto-assign
            if (used.Contains(s.shapeId))                  // nếu trùng, đẩy lên id mới
            {
                int newId = i;
                while (used.Contains(newId)) newId++;
                s.shapeId = newId;
            }
            used.Add(s.shapeId);
            _byId[s.shapeId] = s;
        }
    }
}
