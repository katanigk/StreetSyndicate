using System.Collections.Generic;
using UnityEngine;

namespace FamilyBusiness.CityGen.Data
{
    /// <summary>
    /// Polyline (or ordered path) macro feature with nominal width for future clipping / road avoidance.
    /// </summary>
    public sealed class MacroFeatureData
    {
        public int Id { get; set; }
        public MacroFeatureKind Kind { get; set; }
        public string Label { get; set; }

        /// <summary>Centerline / coastline trace in plan space.</summary>
        public List<Vector2> Path { get; } = new List<Vector2>();

        /// <summary>Nominal half-width in plan cells (river bed, rail easement, coastal strip depth).</summary>
        public float WidthCells { get; set; } = 4f;
    }
}
