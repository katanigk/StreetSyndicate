using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

/// <summary>
/// Facts the crew believes about spots — channel + credibility. Ground truth stays on <see cref="MicroBlockSpotRuntime.Truth"/> until discovery merges in later.
/// </summary>
[Serializable]
public sealed class MicroBlockKnowledgeFact
{
    public string SpotStableId = string.Empty;
    public string FactKey = string.Empty;
    public int Channel;
    public float Credibility01 = 1f;
    public string Text = string.Empty;
    public int AcquiredOnDay;
}

[Serializable]
sealed class MicroBlockKnowledgeSnapshotDto
{
    public MicroBlockKnowledgeFact[] Items = Array.Empty<MicroBlockKnowledgeFact>();
}

public static class MicroBlockKnowledgeStore
{
    public static readonly List<MicroBlockKnowledgeFact> Facts = new List<MicroBlockKnowledgeFact>(64);

    public static void Clear()
    {
        Facts.Clear();
    }

    /// <summary>Baseline “street face” knowledge: place exists + what the sign/neighbors call it (not verified ownership).</summary>
    public static void SeedAmbientFromSpots(IReadOnlyList<MicroBlockSpotRuntime> spots)
    {
        Clear();
        int day = Mathf.Max(1, GameSessionState.CurrentDay);
        for (int i = 0; i < spots.Count; i++)
        {
            MicroBlockSpotRuntime s = spots[i];
            if (s == null || string.IsNullOrEmpty(s.StableId))
                continue;

            float signCred = s.Kind == MicroBlockSpotKind.CrewSharedRoom ? 1f : 0.88f;
            Facts.Add(new MicroBlockKnowledgeFact
            {
                SpotStableId = s.StableId,
                FactKey = "place_present",
                Channel = (int)MicroBlockKnowledgeChannel.AmbientPresence,
                Credibility01 = 1f,
                Text = "We see the frontage on our walks — it’s part of the block.",
                AcquiredOnDay = day
            });
            Facts.Add(new MicroBlockKnowledgeFact
            {
                SpotStableId = s.StableId,
                FactKey = "facade_name",
                Channel = (int)MicroBlockKnowledgeChannel.Heard,
                Credibility01 = signCred,
                Text = "People call it <b>" + s.SurfacePublicName + "</b> — that doesn’t prove who owns the books.",
                AcquiredOnDay = day
            });
        }
    }

    public static string CaptureSnapshotJson()
    {
        var dto = new MicroBlockKnowledgeSnapshotDto { Items = Facts.ToArray() };
        return JsonUtility.ToJson(dto);
    }

    public static void ApplySnapshotJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return;
        MicroBlockKnowledgeSnapshotDto dto = JsonUtility.FromJson<MicroBlockKnowledgeSnapshotDto>(json);
        if (dto?.Items == null || dto.Items.Length == 0)
            return;
        Facts.Clear();
        Facts.AddRange(dto.Items);
    }

    public static void AddFact(MicroBlockKnowledgeFact fact)
    {
        if (fact == null || string.IsNullOrEmpty(fact.SpotStableId))
            return;
        Facts.Add(fact);
    }

    public static string BuildFactsUiTextForSpot(string spotStableId)
    {
        var sb = new StringBuilder(512);
        sb.AppendLine("<b>Crew knowledge</b> <size=90%>(channel · confidence)</size>");
        sb.AppendLine();

        bool any = false;
        for (int i = 0; i < Facts.Count; i++)
        {
            MicroBlockKnowledgeFact f = Facts[i];
            if (f == null || f.SpotStableId != spotStableId)
                continue;
            any = true;
            var ch = (MicroBlockKnowledgeChannel)f.Channel;
            int pct = Mathf.Clamp(Mathf.RoundToInt(f.Credibility01 * 100f), 0, 100);
            sb.Append("• <color=#c9b87a>");
            sb.Append(ChannelShortLabel(ch));
            sb.Append("</color> · <b>");
            sb.Append(pct);
            sb.Append("%</b> — ");
            sb.Append(f.Text);
            sb.AppendLine();
        }

        if (!any)
            sb.AppendLine("<i>No filed knowledge for this spot yet.</i>");

        sb.AppendLine();
        sb.AppendLine("<size=92%><i>Ledgers, protection, real income, and back rooms require scouting, talk, or breaking the threshold — rumor quality depends on the source.</i></size>");
        return sb.ToString().TrimEnd();
    }

    /// <summary>Ambient / seen — treated as “certain” for the Ops left panel.</summary>
    public static string BuildCertainKnowledgeTextForSpot(string spotStableId)
    {
        var sb = new StringBuilder(384);
        bool any = false;
        for (int i = 0; i < Facts.Count; i++)
        {
            MicroBlockKnowledgeFact f = Facts[i];
            if (f == null || f.SpotStableId != spotStableId)
                continue;
            var ch = (MicroBlockKnowledgeChannel)f.Channel;
            if (ch != MicroBlockKnowledgeChannel.AmbientPresence && ch != MicroBlockKnowledgeChannel.Seen)
                continue;
            any = true;
            int pct = Mathf.Clamp(Mathf.RoundToInt(f.Credibility01 * 100f), 0, 100);
            sb.Append("• <b>");
            sb.Append(pct);
            sb.Append("%</b> — ");
            sb.Append(f.Text);
            sb.AppendLine();
        }

        if (!any)
            return "<i>Nothing filed as certain for this place yet.</i>";
        return sb.ToString().TrimEnd();
    }

    /// <summary>Heard / read / intel — “rumors & uncertain” strip.</summary>
    public static string BuildRumorKnowledgeTextForSpot(string spotStableId)
    {
        var sb = new StringBuilder(384);
        bool any = false;
        for (int i = 0; i < Facts.Count; i++)
        {
            MicroBlockKnowledgeFact f = Facts[i];
            if (f == null || f.SpotStableId != spotStableId)
                continue;
            var ch = (MicroBlockKnowledgeChannel)f.Channel;
            if (ch != MicroBlockKnowledgeChannel.Heard && ch != MicroBlockKnowledgeChannel.Read &&
                ch != MicroBlockKnowledgeChannel.IntelGathering)
                continue;
            any = true;
            int pct = Mathf.Clamp(Mathf.RoundToInt(f.Credibility01 * 100f), 0, 100);
            sb.Append("• <color=#c9b87a>");
            sb.Append(ChannelShortLabel(ch));
            sb.Append("</color> · <b>");
            sb.Append(pct);
            sb.Append("%</b> — ");
            sb.Append(f.Text);
            sb.AppendLine();
        }

        if (!any)
            return "<i>No rumors or loose talk filed for this place.</i>";
        return sb.ToString().TrimEnd();
    }

    /// <summary>At most two lines: newest rumor + day, then previous + day (plain text, for Ops strip).</summary>
    public static string BuildRumorDigestTwoLinesForSpot(string spotStableId)
    {
        var list = new List<MicroBlockKnowledgeFact>(8);
        for (int i = 0; i < Facts.Count; i++)
        {
            MicroBlockKnowledgeFact f = Facts[i];
            if (f == null || f.SpotStableId != spotStableId)
                continue;
            var ch = (MicroBlockKnowledgeChannel)f.Channel;
            if (ch != MicroBlockKnowledgeChannel.Heard && ch != MicroBlockKnowledgeChannel.Read &&
                ch != MicroBlockKnowledgeChannel.IntelGathering)
                continue;
            list.Add(f);
        }

        if (list.Count == 0)
            return "<i>No rumors on file.</i>";

        list.Sort((a, b) => b.AcquiredOnDay.CompareTo(a.AcquiredOnDay));

        var sb = new StringBuilder(256);
        MicroBlockKnowledgeFact newest = list[0];
        sb.Append("<b>Day ").Append(newest.AcquiredOnDay).Append(":</b> ").Append(StripRichTextForDigest(newest.Text));
        if (list.Count > 1)
        {
            MicroBlockKnowledgeFact prev = list[1];
            sb.Append("\n<b>Day ").Append(prev.AcquiredOnDay).Append(":</b> ").Append(StripRichTextForDigest(prev.Text));
        }

        return sb.ToString();
    }

    /// <summary>Generic bullets prepended to spot-specific “certain” knowledge in Ops.</summary>
    public static string BuildGenericCertainKnowledgeBullets()
    {
        return "• <b>Street certainty</b> — names and faces on the block are what people repeat, not court proof.\n" +
            "• <b>Ledgers & back rooms</b> — anything not seen or filed stays unknown until you work it.\n";
    }

    static string StripRichTextForDigest(string raw)
    {
        if (string.IsNullOrEmpty(raw))
            return string.Empty;
        string s = Regex.Replace(raw, "<[^>]+>", string.Empty);
        if (s.Length > 140)
            s = s.Substring(0, 137) + "…";
        return s.Trim();
    }

    static string ChannelShortLabel(MicroBlockKnowledgeChannel ch)
    {
        return ch switch
        {
            MicroBlockKnowledgeChannel.AmbientPresence => "street",
            MicroBlockKnowledgeChannel.Seen => "seen",
            MicroBlockKnowledgeChannel.Heard => "heard",
            MicroBlockKnowledgeChannel.Read => "read",
            MicroBlockKnowledgeChannel.IntelGathering => "intel",
            _ => "?"
        };
    }
}
