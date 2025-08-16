using UnityEngine;

[CreateAssetMenu(fileName = "BlockSkin", menuName = "Skins/Block Skin")]
public class BlockSkin : ScriptableObject
{
    [Header("Tile variants (per-piece)")]
    public Sprite[] tileVariants;       

    [Header("Board cell background (optional)")]
    public Sprite boardCellNormal;       // nếu muốn ô nền dùng sprite riêng

    public Sprite GetVariant(int index)
    {
        if (tileVariants == null || tileVariants.Length == 0) return null;
        index = Mathf.Abs(index) % tileVariants.Length;
        return tileVariants[index];
    }

    public int RollVariant(System.Random rng)
    {
        if (tileVariants == null || tileVariants.Length == 0) return 0;
        return rng.Next(tileVariants.Length);
    }
}
