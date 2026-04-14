using UnityEngine;

namespace FamilyBusiness.CityGen.Discovery
{
    /// <summary>Gizmo tint colors for <see cref="DiscoveryState"/> (Batch 9 debug only).</summary>
    public static class DiscoveryDebugPalette
    {
        public static Color StateColor(DiscoveryState s) =>
            s switch
            {
                DiscoveryState.Unknown => new Color(0.25f, 0.25f, 0.28f, 0.55f),
                DiscoveryState.Rumored => new Color(0.35f, 0.45f, 0.85f, 0.6f),
                DiscoveryState.Known => new Color(0.4f, 0.75f, 0.45f, 0.62f),
                DiscoveryState.PartiallyExposed => new Color(0.85f, 0.65f, 0.35f, 0.65f),
                DiscoveryState.DeeplyExposed => new Color(0.9f, 0.4f, 0.35f, 0.68f),
                DiscoveryState.Controlled => new Color(0.75f, 0.4f, 0.85f, 0.7f),
                DiscoveryState.Infiltrated => new Color(0.95f, 0.5f, 0.85f, 0.72f),
                _ => Color.gray
            };
    }
}
