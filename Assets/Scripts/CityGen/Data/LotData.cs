using System.Collections.Generic;
using UnityEngine;

namespace FamilyBusiness.CityGen.Data
{
    /// <summary>
    /// Axis-aligned footprint inside a block (Batch 5: grid) + road/access metadata (Batch 5.5).
    /// </summary>
    public sealed class LotData
    {
        public int Id { get; set; }
        public int BlockId { get; set; }
        public int DistrictId { get; set; } = -1;
        public DistrictKind DistrictKind { get; set; } = DistrictKind.Unknown;

        public Vector2 Min;
        public Vector2 Max;

        /// <summary>CCW rectangle — same geometry as Min/Max; kept for systems that consume polygons.</summary>
        public List<Vector2> Outline { get; } = new List<Vector2>();

        public float AreaCells => Mathf.Max(0f, Max.x - Min.x) * Mathf.Max(0f, Max.y - Min.y);

        // --- Batch 5.5: road & placement metadata ---

        public bool TouchesRoad;
        public List<int> AdjacentRoadEdgeIds { get; } = new List<int>();
        public float NearestRoadDistance = float.MaxValue;

        public float FrontageLength;
        public RoadEdgeKind FrontageRoadKind = RoadEdgeKind.Unknown;
        public bool HasMajorRoadFrontage;
        public int FrontageSideIndex = -1;

        public float AccessibilityScore;
        public bool TouchesBlockEdge;

        public bool SupportsLargeBuilding;
        public bool SupportsStreetFacingBuilding;
        public bool SupportsBackLotUse;
        public LotSizeClass SizeClass = LotSizeClass.Unknown;

        // --- Batch 6: reserved for anchor institution ---

        public bool IsReserved;
        public int ReservedByInstitutionId = -1;
        public InstitutionKind ReservedForKind = InstitutionKind.Unknown;

        /// <summary>Batch 7: index into <see cref="CityData.Buildings"/> for this lot, or -1.</summary>
        public int RegularBuildingId = -1;
    }
}
