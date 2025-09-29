// Assets/Data/Tutorial/TutorialSequence.cs
using System.Collections.Generic;
using UnityEngine;

public enum SeedPickMode { ForcedIndex, Random }
public enum AnchorConstraintMode { AnyOf, PickOne, ForcedIndex }

[CreateAssetMenu(fileName = "TutorialSequence", menuName = "BlockBlast/Tutorial Sequence")]
public class TutorialSequence : ScriptableObject
{
    public List<Stage> stages = new();

    [System.Serializable]
    public class Stage
    {
        [Header("Prefill boards (choose one)")]
        [Tooltip("Danh sách seed board. Director sẽ chọn 1 cái theo SeedPickMode.")]
        public List<ShapeData> prefillBoards = new();

        public SeedPickMode seedPickMode = SeedPickMode.Random;
        [Tooltip("Dùng khi seedPickMode = ForcedIndex. -1 = bỏ qua.")]
        public int forcedSeedIndex = -1;

        [Header("Hand (slots)")]
        [Tooltip("Định nghĩa cho từng slot của palette ở stage này (theo thứ tự slot).")]
        public List<SlotRule> slots = new();

        [Header("Yêu cầu / nhắc")]
        public bool requireLineClearThisStep = false;      
        [TextArea] public string tipText;
    }

    [System.Serializable]
    public class SlotRule
    {
        [Tooltip("Shape sẽ xuất hiện ở slot này. Có thể để null => slot rỗng (bỏ qua).")]
        public ShapeData handShape;

        [Header("Anchors & constraint")]
        [Tooltip("Danh sách anchor mục tiêu hợp lệ cho slot này (tối đa 3–4 cái tuỳ ý).")]
        public List<Vector2Int> anchorCandidates = new();

        public AnchorConstraintMode anchorMode = AnchorConstraintMode.AnyOf;

        [Tooltip("Dùng khi anchorMode = ForcedIndex. -1 = bỏ qua.")]
        public int forcedAnchorIndex = -1;

        [Header("Yêu cầu riêng slot (tuỳ chọn)")]
        [Tooltip("Nếu true thì riêng slot này bắt buộc bước đặt này phải clear line.")]
        public bool requireLineClear = false;
    }
}
