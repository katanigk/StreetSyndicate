using UnityEngine;

/// <summary>
/// Roof sprite + optional Z rotation for uGUI block cells (corner autotile).
/// </summary>
public readonly struct RoofUiSpec
{
    public readonly Sprite Sprite;
    public readonly float ZRotationDegrees;

    public RoofUiSpec(Sprite sprite, float zRotationDegrees)
    {
        Sprite = sprite;
        ZRotationDegrees = zRotationDegrees;
    }

    public bool HasSprite => Sprite != null;
}
