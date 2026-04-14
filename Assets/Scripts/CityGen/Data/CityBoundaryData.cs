using System.Collections.Generic;
using UnityEngine;

namespace FamilyBusiness.CityGen.Data
{
    /// <summary>
    /// Closed polygon boundary for macro layout (Batch 2+). Vertices should be ordered (typically CCW).
    /// </summary>
    public sealed class CityBoundaryData
    {
        public List<Vector2> Vertices { get; } = new List<Vector2>();

        public void Clear() => Vertices.Clear();

        public void GetAxisAlignedBounds(out Vector2 min, out Vector2 max)
        {
            if (Vertices.Count == 0)
            {
                min = Vector2.zero;
                max = Vector2.zero;
                return;
            }

            min = Vertices[0];
            max = Vertices[0];
            for (int i = 1; i < Vertices.Count; i++)
            {
                Vector2 v = Vertices[i];
                min = Vector2.Min(min, v);
                max = Vector2.Max(max, v);
            }
        }

        public CityBoundary ToAxisAlignedBoundary()
        {
            GetAxisAlignedBounds(out Vector2 min, out Vector2 max);
            return new CityBoundary(min, max);
        }

        public Vector2 Centroid()
        {
            if (Vertices.Count == 0)
                return Vector2.zero;
            Vector2 s = Vector2.zero;
            for (int i = 0; i < Vertices.Count; i++)
                s += Vertices[i];
            return s / Vertices.Count;
        }
    }
}
