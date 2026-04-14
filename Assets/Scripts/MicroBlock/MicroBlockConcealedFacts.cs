using System;
using UnityEngine;

/// <summary>
/// Ground truth for a spot. Shown to the player only through discovery / knowledge system later.
/// Serialized into save deltas in a future format version.
/// </summary>
[Serializable]
public sealed class MicroBlockConcealedFacts
{
    [Tooltip("Legal owner on paper; may differ from who runs the floor.")]
    public string OwnerDisplayName = string.Empty;

    [Tooltip("Who works the counter today — may be conflated with 'owner' in surface knowledge.")]
    public string[] WorkerDisplayNames = Array.Empty<string>();

    /// <summary>Estimated weekly net in dollars (1920s scale); discoverable.</summary>
    public int WeeklyNetIncomeEstimateUsd;

    public bool PaysProtectionRacket;
    public bool PoliceLookoutOrInformantTie;

    [Range(0, 100)]
    public int PolicePatrolInterest;

    /// <summary>How hard entry/break-in is abstractly; gameplay hook later.</summary>
    [Range(0, 100)]
    public int IntrusionDifficulty;

    public bool SellsAlcoholIllicitly;
    public bool SellsNarcoticsIllicitly;

    [TextArea(1, 4)]
    public string DesignerNote = string.Empty;
}
