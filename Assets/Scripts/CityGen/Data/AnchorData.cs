using UnityEngine;

namespace FamilyBusiness.CityGen.Data
{
    public enum AnchorKind
    {
        Unknown = 0,
        Gameplay = 1,
        Narrative = 2,
        Traffic = 3
    }

    /// <summary>
    /// Semantic point for POIs, missions, etc. (Batch 2+).
    /// </summary>
    public sealed class AnchorData
    {
        public int Id { get; set; }
        public AnchorKind Kind { get; set; } = AnchorKind.Unknown;
        public Vector2 Position;
        public string Label { get; set; }
    }
}
