using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

public static class PersonnelRegistry
{
    /// <summary>
    /// Align roster slot 0 with the player boss profile and session custody flags (fixes template crew / load desync).
    /// </summary>
    public static void SyncBossSlotFromProfileAndCustody(PlayerCharacterProfile profile)
    {
        if (profile == null || Members == null || Members.Count <= 0)
            return;

        string rosterName = (string.IsNullOrWhiteSpace(profile.DisplayName) ? "Boss" : profile.DisplayName.Trim()) + " (Boss)";
        CrewMember boss = Members[0];
        boss.Name = rosterName;
        boss.Role = "Boss";

        string initial = (GameSessionState.InitialDetainedCharacterName ?? string.Empty).Trim();
        string display = string.IsNullOrWhiteSpace(profile.DisplayName) ? string.Empty : profile.DisplayName.Trim();

        bool nameMatchesDetainedList =
            !string.IsNullOrEmpty(initial) &&
            (string.Equals(initial, rosterName, StringComparison.OrdinalIgnoreCase) ||
             (!string.IsNullOrEmpty(display) && string.Equals(initial, display, StringComparison.OrdinalIgnoreCase)));

        bool applyBossCustody = GameSessionState.BossStartsInPrison || nameMatchesDetainedList;

        if (applyBossCustody)
        {
            // If session says the boss is detained but the detained-name string is missing,
            // populate it so all UI surfaces (overview quick status, boss profile) stay consistent.
            if (string.IsNullOrWhiteSpace(GameSessionState.InitialDetainedCharacterName))
                GameSessionState.InitialDetainedCharacterName = rosterName;

            if (GameSessionState.BossCustodyTrialCompleted)
                boss.SetStatus(CharacterStatus.Imprisoned);
            else
                boss.SetStatus(CharacterStatus.Detained);

            // Ensure a non-empty detention cause exists when the boss is placed into custody by session flags.
            if (!GameSessionState.BossCustodyTrialCompleted)
            {
                if (boss.Arrest == null || boss.Arrest.GetEffectivePrimary() == ArrestCause.Unknown)
                {
                    boss.Arrest = ArrestRecord.CreateDefault(
                        ArrestCause.RacketeeringConspiracy,
                        GameSessionState.AgencyId.Police,
                        GameSessionState.CurrentDay,
                        "Custody initiated by session route / save.");
                }
            }
        }
    }

    public static readonly List<CrewMember> Members = new List<CrewMember>
    {
        new CrewMember { Name = "Vinnie \"The Bull\" Russo", Role = "Capo", Status = "Available", Loyalty = "Loyal", Skills = "Combat, Intimidation", PersonalReputation = 28, Satisfaction = CrewSatisfactionLevel.Satisfied },
        new CrewMember { Name = "Ricky \"Two Guns\" DeLuca", Role = "Soldier", Status = "On Mission", Loyalty = "Reliable", Skills = "Firearms, Driving", PersonalReputation = 12, Satisfaction = CrewSatisfactionLevel.Neutral },
        new CrewMember { Name = "Salvatore \"Sally\" Moretti", Role = "Underboss", Status = "Available", Loyalty = "Loyal", Skills = "Negotiation, Finance", PersonalReputation = 35, Satisfaction = CrewSatisfactionLevel.Satisfied },
        new CrewMember { Name = "Frank \"Fixer\" Moretti", Role = "Associate", Status = "Training", Loyalty = "Shaky", Skills = "Lockpicking, Stealth", PersonalReputation = -6, Satisfaction = CrewSatisfactionLevel.Unsatisfied },
        new CrewMember { Name = "Tony \"Ears\" Greco", Role = "Enforcer", Status = "Injured", Loyalty = "Loyal", Skills = "Intimidation, Surveillance", PersonalReputation = 22, Satisfaction = CrewSatisfactionLevel.Neutral },
        new CrewMember { Name = "Marco \"Silk\" Vitale", Role = "Associate", Status = "Available", Loyalty = "Reliable", Skills = "Forgery, Documents", PersonalReputation = 5, Satisfaction = CrewSatisfactionLevel.Neutral },
        new CrewMember { Name = "Rosa \"Nails\" Conti", Role = "Soldier", Status = "On Mission", Loyalty = "Loyal", Skills = "Surveillance, Disguise", PersonalReputation = 18, Satisfaction = CrewSatisfactionLevel.Satisfied },
        new CrewMember { Name = "Paulie \"Ice\" Lombardi", Role = "Capo", Status = "Available", Loyalty = "Shaky", Skills = "Negotiation, Bribes", PersonalReputation = 30, Satisfaction = CrewSatisfactionLevel.Unsatisfied },
        new CrewMember { Name = "Mike \"Fuse\" Brennan", Role = "Demolitions", Status = "Available", Loyalty = "Reliable", Skills = "Sabotage, explosives, bomb fabrication", PersonalReputation = 14, Satisfaction = CrewSatisfactionLevel.Neutral }
    };

    public static string BuildContextStripSummary()
    {
        int total = Members.Count;
        int available = Members.Count(m => m.Status == "Available");
        int onMission = Members.Count(m => m.Status == "On Mission");
        int injured = Members.Count(m => m.Status == "Injured");
        int training = Members.Count(m => m.Status == "Training");
        float medMorale = CrewMoraleSystem.GetOrgMoraleMedian();
        return "Total Crew: " + total + "  |  Available: " + available + "  |  On Mission: " + onMission +
               "  |  Injured: " + injured + "  |  Training: " + training +
               "  |  Morale (median): " + Mathf.RoundToInt(medMorale);
    }

    /// <summary>Left column: display name (includes quoted nickname in <see cref="CrewMember.Name"/>), then role/rank line.</summary>
    public static string BuildRosterNamesColumn()
    {
        var sb = new StringBuilder();
        sb.AppendLine("<b>Members</b>");
        for (int i = 0; i < Members.Count; i++)
        {
            CrewMember m = Members[i];
            if (m == null)
                continue;
            sb.AppendLine("<size=15><b>" + m.Name + "</b></size>");
            if (!string.IsNullOrWhiteSpace(m.Role))
                sb.AppendLine("<size=13><color=#C8C2B8>" + m.Role + "</color></size>");
            if (i < Members.Count - 1)
                sb.AppendLine();
        }

        return sb.ToString();
    }

    /// <summary>Center: category stub + roster rows without identity (status, loyalty, skills).</summary>
    public static string BuildRosterTable()
    {
        var sb = new StringBuilder();
        sb.AppendLine("<b>Categories</b>");
        sb.AppendLine("• All members\n• Available\n• On mission\n• Leadership\n• Training");
        sb.AppendLine();
        sb.AppendLine("<b>Filters</b> <size=12><i>(coming soon)</i></size>");
        sb.AppendLine();
        sb.AppendLine("<b>Roster</b>");
        sb.AppendLine("<size=13>Status  |  Loyalty  |  Skills</size>");
        foreach (CrewMember m in Members)
        {
            if (m == null)
                continue;
            sb.AppendLine(m.Status + "  |  " + m.Loyalty + "  |  " + m.Skills);
        }

        return sb.ToString();
    }

    public static string BuildRightPanelStub()
    {
        float med = CrewMoraleSystem.GetOrgMoraleMedian();
        float mean = CrewMoraleSystem.GetOrgMoraleMean();
        return "<b>Insights</b>\n• Org morale (median): " + Mathf.RoundToInt(med) +
               "\n• Org morale (mean): " + mean.ToString("F1") +
               "\n• Internal risk: <i>TBD</i> (axes not wired)" +
               "\n\n<i>Reputation channels + memory hooks — next pass.</i>";
    }

    public static string BuildProfileDetail(int index)
    {
        if (index < 0 || index >= Members.Count)
            return "<b>Profile</b>\n<i>No member at this index.</i>";

        CrewMember m = Members[index];
        string sat = CrewReputationSystem.GetSatisfactionLabel(m.Satisfaction);
        int personalRep = m.PersonalReputation;
        int effectiveRep = CrewReputationSystem.GetEffectiveReputation(m);
        return
            "<b>Profile</b>\n\n" +
            "<size=15>" + m.Name + "</size>\n\n" +
            "<b>ROLE</b>  " + m.Role + "\n" +
            "<b>STAT</b>  " + m.Status + "\n" +
            "<b>LOYL</b>  " + m.Loyalty + "\n" +
            "<b>SAT </b>  " + sat + "\n" +
            "<b>REP-P</b> " + personalRep + "\n" +
            "<b>REP-E</b> " + effectiveRep + "\n" +
            "<b>MOR </b>  " + m.PersonalMorale + "\n" +
            "<b>SKLS</b>  " + m.Skills + "\n\n" +
            "<size=12><i>Hook: equipment, pay, heat, assignments.</i></size>";
    }
}
