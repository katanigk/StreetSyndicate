using System;
using UnityEngine;

/// <summary>
/// One place in the micro-block. <see cref="SurfacePublicName"/> is what the crew knows without digging.
/// </summary>
[Serializable]
public sealed class MicroBlockSpotRuntime
{
    public string StableId;
    public MicroBlockSpotKind Kind;

    [Tooltip("Sign / what neighbors say — may be wrong about who owns it.")]
    public string SurfacePublicName = string.Empty;

    [Tooltip("One line: exists nearby, no inside knowledge.")]
    public string SurfaceShortBlurb = string.Empty;

    /// <summary>CityGen lot id inside <see cref="MicroBlockWorldState.CrewHomeBlockId"/>; -1 if unassigned or city has fewer lots than spots.</summary>
    public int AnchorLotId = -1;

    /// <summary>
    /// Optional roof visual group: spots that share a key use the same sprite from <see cref="BlockRoofVisualConfig"/>
    /// (e.g. two adjacent façades = one continuous roof look).
    /// </summary>
    public string RoofClusterKey = string.Empty;

    /// <summary>Block map shows a name; false shows "?" (unknown footprint to the crew).</summary>
    public bool KnownOnBlockMap;

    public MicroBlockConcealedFacts Truth = new MicroBlockConcealedFacts();

    public MicroBlockSpotRuntime Clone()
    {
        return new MicroBlockSpotRuntime
        {
            StableId = StableId,
            Kind = Kind,
            SurfacePublicName = SurfacePublicName,
            SurfaceShortBlurb = SurfaceShortBlurb,
            AnchorLotId = AnchorLotId,
            RoofClusterKey = RoofClusterKey,
            KnownOnBlockMap = KnownOnBlockMap,
            Truth = new MicroBlockConcealedFacts
            {
                OwnerDisplayName = Truth.OwnerDisplayName,
                WorkerDisplayNames = Truth.WorkerDisplayNames != null ? (string[])Truth.WorkerDisplayNames.Clone() : Array.Empty<string>(),
                WeeklyNetIncomeEstimateUsd = Truth.WeeklyNetIncomeEstimateUsd,
                PaysProtectionRacket = Truth.PaysProtectionRacket,
                PoliceLookoutOrInformantTie = Truth.PoliceLookoutOrInformantTie,
                PolicePatrolInterest = Truth.PolicePatrolInterest,
                IntrusionDifficulty = Truth.IntrusionDifficulty,
                SellsAlcoholIllicitly = Truth.SellsAlcoholIllicitly,
                SellsNarcoticsIllicitly = Truth.SellsNarcoticsIllicitly,
                DesignerNote = Truth.DesignerNote
            }
        };
    }
}
