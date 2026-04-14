using System.Collections.Generic;
using UnityEngine;

namespace FamilyBusiness.CityGen.Data
{
    /// <summary>
    /// Intersection / road graph vertex (Batch 3: adjacency by edge ids).
    /// </summary>
    public sealed class RoadNode
    {
        public int Id { get; set; }
        public Vector2 Position;

        /// <summary>Undirected adjacency — <see cref="RoadEdge"/> ids incident to this node.</summary>
        public List<int> ConnectedEdgeIds { get; } = new List<int>();
    }
}
