using System;
using UnityEngine;

/// <summary>
/// Serializable delta for the micro-block session (rent, landlord, knowledge snapshot). Spots stay seed-driven until we add spot mutations.
/// </summary>
[Serializable]
public sealed class MicroBlockSavePayload
{
    public int CrewWeeklyRentUsd = 8;
    public int CrewRentPrepaidThroughDay = 1;
    public string LandlordDisplayName = string.Empty;

    /// <summary>Json from <see cref="MicroBlockKnowledgeStore.CaptureSnapshotJson"/>; empty = keep ambient seed from bootstrap.</summary>
    public string KnowledgeSnapshotJson = string.Empty;
}

public static class MicroBlockPersistence
{
    public static string CaptureJson()
    {
        var p = new MicroBlockSavePayload
        {
            CrewWeeklyRentUsd = MicroBlockWorldState.CrewWeeklyRentUsd,
            CrewRentPrepaidThroughDay = MicroBlockWorldState.CrewRentPrepaidThroughDay,
            LandlordDisplayName = MicroBlockWorldState.LandlordDisplayName ?? string.Empty,
            KnowledgeSnapshotJson = MicroBlockKnowledgeStore.CaptureSnapshotJson()
        };
        return JsonUtility.ToJson(p);
    }

    public static void ApplyJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return;
        MicroBlockSavePayload p = JsonUtility.FromJson<MicroBlockSavePayload>(json);
        if (p == null)
            return;
        MicroBlockWorldState.CrewWeeklyRentUsd = Mathf.Max(1, p.CrewWeeklyRentUsd);
        MicroBlockWorldState.CrewRentPrepaidThroughDay = Mathf.Max(1, p.CrewRentPrepaidThroughDay);
        if (!string.IsNullOrEmpty(p.LandlordDisplayName))
            MicroBlockWorldState.LandlordDisplayName = p.LandlordDisplayName;

        if (!string.IsNullOrEmpty(p.KnowledgeSnapshotJson))
            MicroBlockKnowledgeStore.ApplySnapshotJson(p.KnowledgeSnapshotJson);
    }
}
