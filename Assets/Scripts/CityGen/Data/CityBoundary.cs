using UnityEngine;

namespace FamilyBusiness.CityGen.Data
{
    /// <summary>
    /// Axis-aligned city extent in abstract cell / plan space (Batch 1: rectangle only).
    /// </summary>
    public struct CityBoundary
    {
        public Vector2 Min;
        public Vector2 Max;

        public CityBoundary(Vector2 min, Vector2 max)
        {
            Min = min;
            Max = max;
        }

        public static CityBoundary AxisAlignedRectangle(float widthCells, float heightCells) =>
            new CityBoundary(Vector2.zero, new Vector2(widthCells, heightCells));

        public Vector2 Center => (Min + Max) * 0.5f;
        public Vector2 Size => Max - Min;
    }
}
