using UnityEngine;

/// <summary>
/// Assign the four zone sprites once (can reference textures under <c>Assets/Art/</c> — not limited to Resources).
/// Place at <c>Resources/OpsMap/BigMapLotZoneVisualSet</c> or leave empty to use per-file loads under <c>OpsMap/BigMapLotZones/</c>.
/// </summary>
[CreateAssetMenu(fileName = "BigMapLotZoneVisualSet", menuName = "Street Syndicate/Ops/Big Map Lot Zone Visual Set")]
public sealed class OpsBigMapLotZoneVisualSet : ScriptableObject
{
    public Sprite residential;
    public Sprite commercial;
    public Sprite warehouse;
    public Sprite police;

    public Sprite TryGet(OpsBigMapLotZoneResolver.ZoneKind k) =>
        k switch
        {
            OpsBigMapLotZoneResolver.ZoneKind.Residential => residential,
            OpsBigMapLotZoneResolver.ZoneKind.Commercial => commercial,
            OpsBigMapLotZoneResolver.ZoneKind.Warehouse => warehouse,
            OpsBigMapLotZoneResolver.ZoneKind.Police => police,
            _ => null
        };
}
