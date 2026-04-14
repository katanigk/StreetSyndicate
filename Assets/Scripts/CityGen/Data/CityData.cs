using System.Collections.Generic;
using FamilyBusiness.CityGen.Government;
using UnityEngine;

namespace FamilyBusiness.CityGen.Data
{
    /// <summary>
    /// Root container produced by the city generator pipeline (<see cref="FamilyBusiness.CityGen.Core.CityGenerator"/>).
    /// </summary>
    public sealed class CityData
    {
        public int Seed { get; }

        /// <summary>Axis-aligned bounds of the macro polygon (updated by <see cref="Generators.MacroLayoutGenerator"/>).</summary>
        public CityBoundary Boundary;

        /// <summary>Irregular city footprint for Ashkelton-scale macro layout.</summary>
        public CityBoundaryData MacroBoundary { get; } = new CityBoundaryData();

        public List<MacroFeatureData> MacroFeatures { get; } = new List<MacroFeatureData>();
        public List<MacroAnchorPointData> MacroAnchors { get; } = new List<MacroAnchorPointData>();

        public List<RoadNode> RoadNodes { get; } = new List<RoadNode>();
        public List<RoadEdge> RoadEdges { get; } = new List<RoadEdge>();
        public List<DistrictData> Districts { get; } = new List<DistrictData>();
        public List<BlockData> Blocks { get; } = new List<BlockData>();
        public List<LotData> Lots { get; } = new List<LotData>();
        public List<AnchorData> Anchors { get; } = new List<AnchorData>();
        public List<InstitutionData> Institutions { get; } = new List<InstitutionData>();
        public List<BuildingData> Buildings { get; } = new List<BuildingData>();

        /// <summary>Batch 11: chosen gang start in plan cells (after <see cref="FamilyBusiness.CityGen.Core.CityWorldEntryBuilder"/>).</summary>
        public Vector2 GangStartPlanPosition;

        public bool GangStartPlanValid;
        public bool StartingRevealAppliedAtWorldEntry;

        /// <summary>Batch 11: last structured spawn from <see cref="FamilyBusiness.CityGen.Core.CityWorldEntryBuilder"/> (null until world entry).</summary>
        public StartingGangSpawnData LastStartingGangSpawn;

        /// <summary>Batch 12: extracted government-facing facility view; refresh via <see cref="GovernmentDataExtractor.Refresh"/> after discovery changes.</summary>
        public CityGovernmentData GovernmentData;

        public CityData(int seed)
        {
            Seed = seed;
        }
    }
}
