using System.Collections.Generic;
using FamilyBusiness.CityGen.Discovery;
using UnityEngine;

namespace FamilyBusiness.CityGen.Data
{
    /// <summary>
    /// Gameplay region owning blocks and carrying baseline sim-facing metadata (Batch 4).
    /// </summary>
    public sealed class DistrictData
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public DistrictKind Kind { get; set; }

        /// <summary>Centroid of owned blocks (updated during generation).</summary>
        public Vector2 CenterPosition;

        public List<int> BlockIds { get; } = new List<int>();

        /// <summary>Closed polygon — convex hull of blocks or macro shell fallback for tooling.</summary>
        public List<Vector2> Outline { get; } = new List<Vector2>();

        public float WealthLevel;
        public float DensityLevel;
        public float CrimeBaseline;
        public float PoliceBaseline;
        public float CommercialValue;
        public float LogisticsValue;

        /// <summary>Batch 9: player knowledge / fog layer; sim stats in this object remain full truth.</summary>
        public DistrictDiscoveryData Discovery { get; set; }
    }
}
