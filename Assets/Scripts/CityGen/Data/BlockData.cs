using UnityEngine;

namespace FamilyBusiness.CityGen.Data
{
    /// <summary>
    /// Axis-aligned city block cell (Batch 4: district ownership + type mirror for queries).
    /// </summary>
    public sealed class BlockData
    {
        public int Id { get; set; }
        public int DistrictId { get; set; } = -1;
        public DistrictKind DistrictKind { get; set; } = DistrictKind.Unknown;
        public Vector2 Min;
        public Vector2 Max;
    }
}
