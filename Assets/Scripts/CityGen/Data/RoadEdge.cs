namespace FamilyBusiness.CityGen.Data
{
    public enum RoadEdgeKind
    {
        Unknown = 0,
        Major = 1,
        Secondary = 2,
        Alley = 3
    }

    /// <summary>
    /// Undirected road segment between two <see cref="RoadNode"/> ids (Batch 3: length + topology metadata).
    /// </summary>
    public sealed class RoadEdge
    {
        public int Id { get; set; }

        /// <summary>Same as <see cref="ToNodeId"/> naming — first endpoint.</summary>
        public int FromNodeId { get; set; }

        public int ToNodeId { get; set; }
        public RoadEdgeKind Kind { get; set; } = RoadEdgeKind.Unknown;

        public float Length { get; set; }

        /// <summary>True if segment passes through/near the macro rail corridor (for future bridges / crossings).</summary>
        public bool CrossesMacroRailCorridor { get; set; }

        /// <summary>Reserved for gameplay / lane metadata in later batches.</summary>
        public string TagsPlaceholder { get; set; }
    }
}
