using UnityEngine;

namespace FamilyBusiness.CityGen.Data
{
    public sealed class MacroAnchorPointData
    {
        public int Id { get; set; }
        public MacroAnchorKind Kind { get; set; }
        public Vector2 Position;
        public string Label { get; set; }
    }
}
