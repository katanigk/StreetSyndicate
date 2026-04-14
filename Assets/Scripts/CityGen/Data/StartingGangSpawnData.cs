using System.Collections.Generic;
using UnityEngine;

namespace FamilyBusiness.CityGen.Data
{
    /// <summary>Batch 11: structured result of starting gang spawn selection (plan + world space).</summary>
    public sealed class StartingGangSpawnData
    {
        public int StartDistrictId = -1;
        public DistrictKind StartDistrictKind = DistrictKind.Unknown;
        public int StartBlockId = -1;
        public int StartLotId = -1;

        public Vector2 StartPlanPosition;

        /// <summary>Primary spawn in world space (XZ from plan, Y supplied at build time).</summary>
        public Vector3 StartWorldPosition;

        public StartingGangSpawnProfile SpawnProfile = StartingGangSpawnProfile.FallbackStart;

        public bool UsesBuildingBasedSpawn;
        public int StartBuildingId = -1;

        public readonly List<Vector2> GangMemberPlanPositions = new List<Vector2>(4);
        public readonly List<Vector3> GangMemberWorldPositions = new List<Vector3>(4);
    }
}
