using System.Text;
using UnityEngine;

/// <summary>
/// 1920s investigation model: police only progress if a report is filed, and can only link suspects via
/// witness testimony or HUMINT. No cameras/GPS/etc.
/// </summary>
public static class PoliceInvestigationSystem
{
    public static void ProcessOperationOutcome(OperationType op, OperationResolution res, PlayerCharacterProfile boss)
    {
        if (boss == null || PersonnelRegistry.Members == null || PersonnelRegistry.Members.Count == 0)
            return;

        // Skip if boss already incarcerated; MVP avoids stacking new cases while detained.
        if (CharacterStatusUtility.IsIncarcerated(PersonnelRegistry.Members[0].GetResolvedStatus()))
            return;

        // --- Stealth & gear modifiers ---
        float stealth = Mathf.Clamp01((boss.Agility > 0f ? boss.Agility : 0f) / 100f); // 0..1
        bool hasMask = HasMask(boss);
        bool hasGloves = HasGloves(boss);

        // --- Noise profile (1920s: bystanders + commotion drives reporting) ---
        float baseNoise = op switch
        {
            OperationType.Scout => 0.22f,
            OperationType.Surveillance => 0.16f,
            OperationType.Collect => 0.38f,
            _ => 0.25f
        };
        baseNoise += Mathf.Clamp(res.ConsequenceScore * 0.012f, 0f, 0.28f);
        if (!res.Success)
            baseNoise += 0.10f; // failures tend to get messy

        float stealthSuppression = 0.45f * stealth + (hasMask ? 0.12f : 0f);
        float effectiveNoise = Mathf.Clamp01(baseNoise * (1f - stealthSuppression));

        // Report gate: without a report, no real investigation starts.
        float reportChance = Mathf.Clamp01(0.15f + effectiveNoise * 0.85f);
        bool reportFiled = Random.value < reportChance;

        // Witness gate: needed to link to suspect (unless HUMINT).
        float witnessChance = reportFiled
            ? Mathf.Clamp01(0.10f + effectiveNoise * 0.65f) * (hasMask ? 0.35f : 1f) * (1f - 0.25f * stealth)
            : 0f;
        bool witnessStatement = reportFiled && (Random.value < witnessChance);

        // HUMINT can sometimes substitute witness linkage (rivals, informants, cops on take).
        float humintChance = reportFiled
            ? Mathf.Clamp01(0.05f + effectiveNoise * 0.22f) * (1f - 0.20f * stealth)
            : 0f;
        bool humintLink = reportFiled && !witnessStatement && (Random.value < humintChance);

        // Evidence generation (only if report exists).
        ArrestRecord file = EnsureBossPoliceFile();
        int day = GameSessionState.CurrentDay;

        int pressureDelta = Mathf.RoundToInt(2 + effectiveNoise * 6);
        if (res.Success && op == OperationType.Collect)
            pressureDelta += 2; // collection is visible
        GameSessionState.ApplyGameplayPolicePressureDelta(pressureDelta);

        StringBuilder sb = new StringBuilder();
        sb.Append("Police file update: ");
        sb.Append(OperationRegistry.GetName(op));
        sb.Append(res.Success ? " success. " : " failed. ");

        if (!reportFiled)
        {
            sb.Append("No report filed — the station has nothing actionable.");
            GameSessionState.LastPoliceInvestigationUpdate = sb.ToString();
            return;
        }

        // Always: station report exists.
        file.Evidence.Add(ArrestEvidenceItem.Create(
            ArrestEvidenceType.DocumentsRecords,
            strength: 18,
            admissibilityRisk: 8,
            summary: "Station report filed by a local caller; patrol logged the incident.",
            day: day,
            directness: ArrestEvidenceDirectness.Indirect,
            chain: ArrestEvidenceChainState.Clean));

        sb.Append("Report filed. ");

        if (witnessStatement)
        {
            int strength = hasMask ? 18 : 38;
            file.Evidence.Add(ArrestEvidenceItem.Create(
                ArrestEvidenceType.Testimony,
                strength: strength,
                admissibilityRisk: 28,
                summary: hasMask
                    ? "Witness statement: masked suspect; only rough build/clothing described."
                    : "Witness statement: suspect described clearly (face, voice, mannerisms).",
                day: day,
                directness: ArrestEvidenceDirectness.Direct,
                chain: ArrestEvidenceChainState.Questionable));
            sb.Append("Witness statement recorded. ");
        }
        else if (humintLink)
        {
            file.Evidence.Add(ArrestEvidenceItem.Create(
                ArrestEvidenceType.Circumstantial,
                strength: 22,
                admissibilityRisk: 55,
                summary: "Street talk / informant lead points toward your crew (hearsay).",
                day: day,
                directness: ArrestEvidenceDirectness.Indirect,
                chain: ArrestEvidenceChainState.Questionable));
            sb.Append("HUMINT lead surfaced. ");
        }
        else
        {
            sb.Append("No witness willing to talk — no suspect linkage.");
            GameSessionState.LastPoliceInvestigationUpdate = sb.ToString();
            return;
        }

        // Forensic/physical only if the scene yields something; gloves reduce fingerprints heavily.
        float physicalChance = Mathf.Clamp01(0.18f + effectiveNoise * 0.40f) * (hasGloves ? 0.35f : 1f);
        if (Random.value < physicalChance)
        {
            file.Evidence.Add(ArrestEvidenceItem.Create(
                ArrestEvidenceType.Forensic,
                strength: hasGloves ? 10 : 28,
                admissibilityRisk: 18,
                summary: hasGloves
                    ? "Partial prints recovered, but smudged; low confidence."
                    : "Fingerprints recovered from the scene; usable quality.",
                day: day,
                directness: ArrestEvidenceDirectness.Indirect,
                chain: ArrestEvidenceChainState.Clean));
            sb.Append("Forensic traces collected. ");
        }

        // MVP arrest trigger: if linkage exists and evidence pile is strong enough.
        int proof = ComputeProofScore(file);
        if (proof >= 70 && op == OperationType.Collect)
        {
            // Collection aligns with protection/extortion in our codex.
            DetainBoss(ArrestCause.ProtectionRacket,
                bonus: hasMask ? ArrestCause.Obstruction : ArrestCause.ArmedThreats,
                notes: "Booked after a reported collection incident. Evidence file opened.");
            sb.Append("Custody risk spiked: detectives moved to detain.");
        }
        else
        {
            sb.Append("Case pressure rising.");
        }

        GameSessionState.LastPoliceInvestigationUpdate = sb.ToString();
    }

    private static int ComputeProofScore(ArrestRecord file)
    {
        if (file == null || file.Evidence == null || file.Evidence.Count == 0)
            return 0;
        int score = 0;
        for (int i = 0; i < file.Evidence.Count; i++)
        {
            ArrestEvidenceItem e = file.Evidence[i];
            if (e == null)
                continue;
            int v = Mathf.Clamp(e.Strength - Mathf.RoundToInt(e.AdmissibilityRisk * 0.35f), 0, 100);
            if (e.Type == ArrestEvidenceType.Testimony && e.Directness == ArrestEvidenceDirectness.Direct)
                v = Mathf.RoundToInt(v * 1.15f);
            score += v;
        }
        return score;
    }

    private static ArrestRecord EnsureBossPoliceFile()
    {
        CrewMember boss = PersonnelRegistry.Members[0];
        if (boss.Arrest == null)
            boss.Arrest = ArrestRecord.CreateDefault(ArrestCause.Unknown, GameSessionState.AgencyId.Police, GameSessionState.CurrentDay, "Police file opened.");
        if (boss.Arrest.Evidence == null)
            boss.Arrest.Evidence = new System.Collections.Generic.List<ArrestEvidenceItem>();
        return boss.Arrest;
    }

    private static void DetainBoss(ArrestCause primary, ArrestCause bonus, string notes)
    {
        if (PersonnelRegistry.Members == null || PersonnelRegistry.Members.Count == 0)
            return;
        CrewMember boss = PersonnelRegistry.Members[0];
        boss.SetStatus(CharacterStatus.Detained);
        boss.Arrest = ArrestRecord.CreateDefault(primary, GameSessionState.AgencyId.Police, GameSessionState.CurrentDay, notes, bonus);

        string dn = PlayerRunState.Character != null && !string.IsNullOrWhiteSpace(PlayerRunState.Character.DisplayName)
            ? PlayerRunState.Character.DisplayName.Trim()
            : "Boss";
        GameSessionState.InitialDetainedCharacterName = dn + " (Boss)";
        GameSessionState.BossStartsInPrison = true;
        GameSessionState.BossPrisonPhase = GameSessionState.PrisonLegalPhase.BeforeTrial;
    }

    private static bool HasMask(PlayerCharacterProfile boss)
    {
        if (boss == null || boss.Equipment == null)
            return false;
        string head = boss.Equipment.HeadSlot ?? "";
        string a1 = boss.Equipment.AccessorySlot1 ?? "";
        string a2 = boss.Equipment.AccessorySlot2 ?? "";
        string rh = boss.Equipment.RightHand != null ? (boss.Equipment.RightHand.UtilityItem ?? "") : "";
        string lh = boss.Equipment.LeftHand != null ? (boss.Equipment.LeftHand.UtilityItem ?? "") : "";
        return ContainsAny(head, "mask", "bandana", "veil") ||
               ContainsAny(a1, "mask", "bandana") ||
               ContainsAny(a2, "mask", "bandana") ||
               ContainsAny(rh, "mask", "bandana") ||
               ContainsAny(lh, "mask", "bandana");
    }

    private static bool HasGloves(PlayerCharacterProfile boss)
    {
        if (boss == null || boss.Equipment == null)
            return false;
        string rh = boss.Equipment.RightHand != null ? (boss.Equipment.RightHand.UtilityItem ?? "") : "";
        string lh = boss.Equipment.LeftHand != null ? (boss.Equipment.LeftHand.UtilityItem ?? "") : "";
        string a1 = boss.Equipment.AccessorySlot1 ?? "";
        string a2 = boss.Equipment.AccessorySlot2 ?? "";
        return ContainsAny(rh, "glove") || ContainsAny(lh, "glove") || ContainsAny(a1, "glove") || ContainsAny(a2, "glove");
    }

    private static bool ContainsAny(string haystack, params string[] needles)
    {
        if (string.IsNullOrWhiteSpace(haystack))
            return false;
        string s = haystack.ToLowerInvariant();
        for (int i = 0; i < needles.Length; i++)
        {
            if (s.Contains(needles[i]))
                return true;
        }
        return false;
    }
}

