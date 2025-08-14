using System.Collections.Generic;
using UnityEngine;

public class ShapePalette : MonoBehaviour
{
    public ShapeLibrary library;
    public List<ShapeItemView> slots = new List<ShapeItemView>();

    private readonly List<ShapeData> _current = new List<ShapeData>();

    private void Start()
    {
        Refill();
    }

    public void Refill()
    {
        if (library == null || slots == null || slots.Count == 0) return;

        _current.Clear();
        for (int i = 0; i < slots.Count; i++)
        {
            var s = library.GetRandom();
            _current.Add(s);
            slots[i].Render(s);

            // đảm bảo slot hiện lại
            var cg = slots[i].GetComponentInParent<CanvasGroup>();
            if (cg) cg.alpha = 1f;
        }
    }


    public void OnAllUsed() => Refill();

    public ShapeData Peek(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= _current.Count) return null;
        return _current[slotIndex];
    }

    public void Consume(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= _current.Count) return;
        if (_current[slotIndex] == null) return;

        _current[slotIndex] = null;
        if (slots[slotIndex] != null) slots[slotIndex].Clear();

        if (AllConsumed()) Refill();
    }

    private bool AllConsumed()
    {
        for (int i = 0; i < _current.Count; i++)
            if (_current[i] != null) return false;
        return true;
    }
}
