using UnityEngine;

public class SkinProvider : MonoBehaviour
{
    public BlockSkin current;
    private System.Random _rng;

    void Awake() { _rng = new System.Random(); }

    public int RollVariant() => current ? current.RollVariant(_rng) : 0;
    public Sprite GetTileSprite(int variantIndex) =>
        current ? current.GetVariant(variantIndex) : null;

    public Sprite GetBoardCellNormal() =>
        current ? current.boardCellNormal : null;
}
