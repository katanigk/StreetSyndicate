using System;
using UnityEngine;

/// <summary>
/// Police-interview framing: answers feed boss core traits, crew accent color, and starting partner specialties.
/// </summary>
/// <remarks>
/// <para><b>Draft array reuse (the source of most confusion):</b></para>
/// <list type="bullet">
/// <item><b>Index 2</b> — Standard route: “who stabbed” (3 opts). Co-op route (last night = “three friends”): partner <b>names</b> flow (manual vs auto); values are <b>not</b> stabbing answers.</item>
/// <item><b>Indices 3–5</b> — Standard: three 6-option “philosophy” questions → <see cref="ChoiceTraitByIndex"/> for bar weights + interview potential ceiling.
/// Co-op: travel <b>reason</b> (4 opts), <b>car owner</b> (4 opts), <b>car color</b> (8 opts). <see cref="CountsTowardTraitPhilosophyPicks"/> must skip 3–5 on co-op.</item>
/// <item><b>Co-op manual interview</b> ends after index 5; indices 6–12 stay unset (-1) and are ignored until <see cref="BuildProfile"/> / session logic.</item>
/// </list>
/// </remarks>
public static class PersonalityQuestionnaire
{
    public const int NameQuestionIndex = 0;
    public const int LastNightQuestionIndex = 1;
    public const int StabbingWhoQuestionIndex = 2;

    /// <summary>First shared slot after stabbing/names: standard = philosophy Q1 (6 opts → trait); co-op = why the crew came (4 opts).</summary>
    public const int PhilosophyOrCoopSlotFirstIndex = 3;

    /// <summary>Last shared slot: standard = philosophy Q3; co-op = vehicle color (8 opts), also mirrored into <see cref="ColorQuestionIndex"/> when confirming.</summary>
    public const int PhilosophyOrCoopSlotLastIndex = 5;

    /// <summary>Alias: shared slot 3 — co-op travel reason UI.</summary>
    public const int CoopReasonQuestionIndex = PhilosophyOrCoopSlotFirstIndex;

    /// <summary>Shared slot 4 — standard: philosophy Q2 (6 opts → trait); co-op: which car brought the crew (4 opts).</summary>
    public const int CoopCarOwnerQuestionIndex = 4;

    /// <summary>Shared slot 5 — standard: philosophy Q3; co-op: car color (8 opts).</summary>
    public const int CoopCarColorQuestionIndex = PhilosophyOrCoopSlotLastIndex;

    /// <summary>Last linear question index in the co-op-only interview (before post-car follow-up UI).</summary>
    public const int CooperativeInterviewLastLinearDraftIndex = PhilosophyOrCoopSlotLastIndex;

    public enum DetainedTarget
    {
        None = 0,
        Boss = 1,
        Partner1 = 2,
        Partner2 = 3,
        Partner3 = 4
    }

    public struct InterrogationOutcome
    {
        public int PolicePressure;
        public int CustodyRisk;
        public bool CulpritIdentified;
        public DetainedTarget Detained;
        public bool BossKnownToPolice;
        public bool StreetExposureIncreased;
        public bool AssociatesDisclosed;
        public int StartingBlackCash;
        public string CaseNote;
    }

    /// <summary>Number of draft slots before the formal color question (indices 0..TraitQuestionCount-1). Not all slots are “trait philosophy”: see class remarks.</summary>
    public const int TraitQuestionCount = 6;

    /// <summary>Single question — favorite crew accent color (8 options).</summary>
    public const int ColorQuestionIndex = TraitQuestionCount;

    /// <summary>Three questions — one per starting partner, maps to skill specialty index.</summary>
    public const int PartnerQuestionCount = 3;

    public const int FirstPartnerQuestionIndex = TraitQuestionCount + 1;

    public const int PolicePressureQuestionIndex = FirstPartnerQuestionIndex + PartnerQuestionCount;
    public const int StabbingStatementQuestionIndex = PolicePressureQuestionIndex + 1;
    public const int NamedAssociateQuestionIndex = StabbingStatementQuestionIndex + 1;

    /// <summary>Trait + color + partner specialty + police pressure + stabbing resolution questions.</summary>
    public const int QuestionCount = TraitQuestionCount + 1 + PartnerQuestionCount + 3;

    public static bool IsCooperativeInterviewRoute(int[] answers)
    {
        return answers != null && answers.Length > LastNightQuestionIndex && answers[LastNightQuestionIndex] == 0;
    }

    public static bool IsPhilosophyOrCoopSharedSlot(int questionIndex)
    {
        return questionIndex >= PhilosophyOrCoopSlotFirstIndex && questionIndex <= PhilosophyOrCoopSlotLastIndex;
    }

    /// <summary>Standard route: slots 3–5 are philosophy picks. Co-op: same indices hold reason/car/ color — they must <b>not</b> count toward trait bars or interview ceiling.</summary>
    public static bool CountsTowardTraitPhilosophyPicks(int[] answers, int questionIndex)
    {
        if (!IsPhilosophyOrCoopSharedSlot(questionIndex))
            return false;
        if (answers == null || answers.Length <= LastNightQuestionIndex)
            return true;
        return !IsCooperativeInterviewRoute(answers);
    }

    /// <summary>
    /// Shown under the session header on the interview screen (story setup).
    /// </summary>
    public const string InterrogationSessionIntro =
        "They cuffed you and put you in the back of the car. The city rolled past the window; the radio muttered; nobody asked how you felt. " +
        "Now you are in a small room with a recorder on the table. They want answers. From here on, what you say is who you are on the file.";

    /// <summary>Story beat before each question (interrogation reference).</summary>
    public static string[] QuestionIntroRefs =>
        new[]
        {
            "The lead officer taps the recorder and opens the file. First line is identity: name on record.",
            "The officer leans back. 'Tell us where you were last night.'",
            "The detective flips to the assault page. 'Who started the stabbing?'",
            "They ask about territory like it is a rumor. How do you secure a foothold before rivals can react?",
            "They lean in when trust breaks. Someone panics. What, in your story, keeps the group from splintering?",
            "They ask what you want the crew to believe about you by dawn — the line nobody is supposed to test.",
            "Paperwork needs a mark for the file — a color your people will wear so your crews can tell each other from rivals later.",
            "They read the first name from your statement — an associate traveling with you. What do they bring when things go loud?",
            "Second associate. Same file, different body. What is their edge when the night turns ugly?",
            "Third associate. Last line on this page. What do they contribute when the plan starts to crack?",
            "The officers compare notes. They decide whether your family is just noise or a real target under a magnifying glass.",
            "They return to the stabbing report. Recorder is still on. They want one official statement.",
            "If you point at an associate, they ask which one goes into the file."
        };

    public static string[] QuestionTitles =>
        new[]
        {
            "Name. What is your name?",
            "Well, {NAME}, tell us what you did last night.",
            "So, {NAME}, who did the stabbing?",
            "You get one opening in the district. How do you secure it before rivals react?",
            "Pressure rises, trust is fragile, and one friend panics. What keeps your group together?",
            "By dawn, what should your crew believe about you?",
            "Favorite color — if your organization had to wear one signal color for the long run, which would it be?",
            "Your first associate — what do they do best when the situation turns hot?",
            "Your second associate — what is their strongest contribution under pressure?",
            "Your third associate — what do they bring that the others do not?",
            "How hot should police interest in your family start?",
            "On the stabbing case, what statement do you give?",
            "If the file requires one associate name, who do you name?"
        };

    /// <summary>Per question: choice labels.</summary>
    public static string[][] ChoiceLabels =>
        new[]
        {
            new[]
            {
                "Write my real name in the file.",
                "Make up an alias for me.",
                "Silence."
            },
            new[]
            {
                "I went out with my three friends.",
                "I don't remember.",
                "Silence."
            },
            new[]
            {
                "I did it.",
                "One of my friends did it.",
                "No comment."
            },
            new[]
            {
                "Take the territory by direct presence.",
                "Hit quickly, relocate, and deny retaliation.",
                "Control information and shape the next move early.",
                "Build local alliances and lock a quiet agreement.",
                "Hold steady while everyone else overreacts.",
                "Push through setbacks until the district is ours."
            },
            new[]
            {
                "Force and intimidation drills.",
                "Mobility, timing, and escape lines.",
                "Planning sessions and tactical review.",
                "Negotiation, leverage, and influence practice.",
                "Stress tolerance and emotional control.",
                "Execution routines and endurance discipline."
            },
            new[]
            {
                "When I speak, nobody tests the line.",
                "Nobody can pin us down or predict our path.",
                "We win before the fight even begins.",
                "Even enemies prefer making terms with us.",
                "We do not crack, no matter who leans on us.",
                "If we start it, we finish it."
            },
            new[]
            {
                "Red",
                "Green",
                "Blue",
                "Turquoise",
                "Yellow",
                "Orange",
                "Purple",
                "Lilac"
            },
            PartnerSpecialtyChoiceLabels,
            PartnerSpecialtyChoiceLabels,
            PartnerSpecialtyChoiceLabels,
            new[]
            {
                "Low profile — they mark us as minor noise for now.",
                "Noticeable — they track us, but not full priority.",
                "High watch — patrols and detectives keep eyes on us.",
                "Maximum scrutiny — active pressure from multiple units."
            },
            new[]
            {
                "I did it. Put my name in the report.",
                "One associate did it. I will give you a name.",
                "No statement. I stay silent."
            },
            new[]
            {
                "First associate.",
                "Second associate.",
                "Third associate."
            }
        };

    /// <summary>Maps 1:1 to MainMenuFlowController._partnerSkillOptions (six specialties).</summary>
    private static string[] PartnerSpecialtyChoiceLabels =>
        new[]
        {
            "Wheels and heat — driving under pressure, firearms when it goes loud.",
            "Ghost work — stealth, locks, quiet entry and exit.",
            "Muscle and face — intimidation, raw confrontation.",
            "Eyes and cover — surveillance, blending, reading the street.",
            "Talk and terms — negotiation, bribes, leverage.",
            "Papers and alibis — documents, forgery, clean covers."
        };

    /// <summary>choice index maps directly to one core trait (trait questions only).</summary>
    private static readonly CoreTrait[] ChoiceTraitByIndex =
    {
        CoreTrait.Strength,
        CoreTrait.Agility,
        CoreTrait.Intelligence,
        CoreTrait.Charisma,
        CoreTrait.MentalResilience,
        CoreTrait.Determination
    };

    /// <summary>Returns pick counts per trait from questionnaire answers (for UI display).</summary>
    public static void GetTraitPicksFromAnswers(int[] answers,
        out int strengthPicks, out int agilityPicks, out int intelligencePicks,
        out int charismaPicks, out int mentalPicks, out int determinationPicks)
    {
        strengthPicks = agilityPicks = intelligencePicks = charismaPicks = mentalPicks = determinationPicks = 0;
        if (answers == null || answers.Length < TraitQuestionCount) return;
        for (int q = 1; q < TraitQuestionCount; q++)
        {
            if (answers[q] < 0)
                continue;
            if (q == LastNightQuestionIndex || q == StabbingWhoQuestionIndex || !CountsTowardTraitPhilosophyPicks(answers, q))
                continue;
            int n = ChoiceLabels[q].Length;
            int choice = Mathf.Clamp(answers[q], 0, n - 1);
            switch (ChoiceTraitByIndex[choice])
            {
                case CoreTrait.Strength: strengthPicks++; break;
                case CoreTrait.Agility: agilityPicks++; break;
                case CoreTrait.Intelligence: intelligencePicks++; break;
                case CoreTrait.Charisma: charismaPicks++; break;
                case CoreTrait.MentalResilience: mentalPicks++; break;
                case CoreTrait.Determination: determinationPicks++; break;
            }
        }
    }

    /// <summary>Partner answer index maps to _partnerSkillOptions index (0..5).</summary>
    public static int GetPartnerSkillIndexFromAnswer(int choiceIndex)
    {
        int n = PartnerSpecialtyChoiceLabels.Length;
        return Mathf.Clamp(choiceIndex, 0, n - 1);
    }

    public static string GetImmediateConsequencePreview(int questionIndex, int answerIndex, int[] questionnaireAnswers = null)
    {
        if (questionIndex < 0 || questionIndex >= QuestionCount || answerIndex < 0)
            return string.Empty;

        if (questionIndex == 0)
        {
            if (answerIndex == 0)
                return "Consequence: your entered name becomes your identity. No trait bonus.";
            if (answerIndex == 1)
                return "Consequence: system generates alias and grants +100 Charisma XP.";
            if (answerIndex == 2)
                return "Consequence: police label you 'Mr. X' and grant +100 Mental Resilience XP.";
            return string.Empty;
        }

        if (questionIndex == LastNightQuestionIndex)
        {
            if (answerIndex == 0)
                return "Consequence: police now know you were with three associates; their follow-up path is unlocked.";
            if (answerIndex == 1)
                return "Consequence: +5 police pressure and +10 custody risk.";
            if (answerIndex == 2)
                return "Consequence: +100 Mental Resilience XP and +5 police pressure.";
            return string.Empty;
        }

        if (questionIndex == StabbingWhoQuestionIndex)
        {
            if (answerIndex == 0)
                return "Consequence: self-incrimination. +10 police pressure and +25 custody risk.";
            if (answerIndex == 1)
                return "Consequence: shifts blame to an associate. +5 police pressure and +10 custody risk.";
            if (answerIndex == 2)
                return "Consequence: silence. +5 police pressure and +100 Mental Resilience XP.";
            return string.Empty;
        }

        if (questionnaireAnswers != null && IsCooperativeInterviewRoute(questionnaireAnswers) &&
            IsPhilosophyOrCoopSharedSlot(questionIndex))
        {
            if (questionIndex == CoopReasonQuestionIndex)
            {
                switch (answerIndex)
                {
                    case 0:
                        return "Consequence: boss gains +50 Strength practice (split to skills) when the profile is built.";
                    case 1:
                        return "Consequence: boss gains +50 Determination practice (split to skills).";
                    case 2:
                        return "Consequence: no scripted boss trait bonus for this reason in the current build.";
                    case 3:
                        return "Consequence: boss gains +50 Intelligence practice (split to skills).";
                    default:
                        return "Consequence: records why the crew came to the city.";
                }
            }

            if (questionIndex == CoopCarOwnerQuestionIndex)
                return "Consequence: records who drove; narrative / session detail (no lump boss trait grant from this pick alone).";
            if (questionIndex == CoopCarColorQuestionIndex)
                return "Consequence: sets crew accent from the car color (also copied to the formal color slot in your answers).";
        }

        if (questionIndex < TraitQuestionCount && CountsTowardTraitPhilosophyPicks(questionnaireAnswers, questionIndex))
        {
            CoreTrait trait = ChoiceTraitByIndex[Mathf.Clamp(answerIndex, 0, ChoiceTraitByIndex.Length - 1)];
            return "Consequence: strengthens " + trait + " development and related skills.";
        }

        if (questionIndex == ColorQuestionIndex)
            return "Consequence: sets your family's identifying color across UI and identity.";

        if (questionIndex >= FirstPartnerQuestionIndex && questionIndex < PolicePressureQuestionIndex)
            return "Consequence: sets this associate's opening specialty.";

        if (questionIndex == PolicePressureQuestionIndex)
        {
            int pressure = 25 + (Mathf.Clamp(answerIndex, 0, 3) * 20);
            return "Consequence: starting police pressure becomes " + pressure + "/100.";
        }

        if (questionIndex == StabbingStatementQuestionIndex)
        {
            if (answerIndex == 0)
                return "Consequence: boss is identified and starts with a short prison term; no bail cash drain.";
            if (answerIndex == 1)
                return "Consequence: one associate is identified and jailed; cash preserved, exposure rises.";
            return "Consequence: culprit stays unknown; released on bail but you start low on money ($350 black cash).";
        }

        if (questionIndex == NamedAssociateQuestionIndex)
            return "Consequence: this associate will take the case if you choose to name an associate.";

        return string.Empty;
    }

    public static InterrogationOutcome ResolveInterrogationOutcome(int[] answers)
    {
        int[] safe = new int[QuestionCount];
        if (answers != null)
        {
            for (int i = 0; i < Mathf.Min(answers.Length, QuestionCount); i++)
                safe[i] = Mathf.Max(0, answers[i]);
        }

        int rawNameAns = (answers != null && answers.Length > NameQuestionIndex) ? answers[NameQuestionIndex] : -1;
        int rawPoliceAns = (answers != null && answers.Length > PolicePressureQuestionIndex) ? answers[PolicePressureQuestionIndex] : -1;
        int rawLastNightAns = (answers != null && answers.Length > LastNightQuestionIndex) ? answers[LastNightQuestionIndex] : -1;
        int rawStabbingWhoAns = (answers != null && answers.Length > StabbingWhoQuestionIndex) ? answers[StabbingWhoQuestionIndex] : -1;
        int rawStatementAns = (answers != null && answers.Length > StabbingStatementQuestionIndex) ? answers[StabbingStatementQuestionIndex] : -1;
        int rawNamedAssocAns = (answers != null && answers.Length > NamedAssociateQuestionIndex) ? answers[NamedAssociateQuestionIndex] : -1;

        bool coopRoute = rawLastNightAns == 0; // "with three friends" route
        bool policeAnswered = rawPoliceAns >= 0;
        bool statementAnswered = rawStatementAns >= 0;
        bool stabbingWhoAnswered = rawStabbingWhoAns >= 0;

        int policeAns = Mathf.Clamp(safe[PolicePressureQuestionIndex], 0, ChoiceLabels[PolicePressureQuestionIndex].Length - 1);
        int statementAns = Mathf.Clamp(safe[StabbingStatementQuestionIndex], 0, ChoiceLabels[StabbingStatementQuestionIndex].Length - 1);
        int namedAssocAns = Mathf.Clamp(safe[NamedAssociateQuestionIndex], 0, ChoiceLabels[NamedAssociateQuestionIndex].Length - 1);
        int nameAns = Mathf.Clamp(safe[NameQuestionIndex], 0, ChoiceLabels[NameQuestionIndex].Length - 1);
        int lastNightAns = Mathf.Clamp(safe[LastNightQuestionIndex], 0, ChoiceLabels[LastNightQuestionIndex].Length - 1);
        int stabbingWhoAns = Mathf.Clamp(safe[StabbingWhoQuestionIndex], 0, ChoiceLabels[StabbingWhoQuestionIndex].Length - 1);

        int pressure = 25 + ((policeAnswered ? policeAns : 0) * 20); // 25 / 45 / 65 / 85
        var outcome = new InterrogationOutcome
        {
            PolicePressure = Mathf.Clamp(pressure, 0, 100),
            CustodyRisk = 0,
            CulpritIdentified = false,
            Detained = DetainedTarget.None,
            BossKnownToPolice = false,
            StreetExposureIncreased = false,
            AssociatesDisclosed = rawLastNightAns == 0 || (!coopRoute && rawStabbingWhoAns == 1),
            StartingBlackCash = 350,
            CaseNote = "Culprit unknown. Released on bail; most liquid money burned in legal pressure."
        };

        if (nameAns == 2)
            outcome.PolicePressure = Mathf.Clamp(outcome.PolicePressure + 5, 0, 100);
        else if (nameAns == 1 && rawLastNightAns == 2)
            outcome.PolicePressure = Mathf.Clamp(outcome.PolicePressure + 5, 0, 100);

        if (rawLastNightAns == 1)
        {
            outcome.PolicePressure = Mathf.Clamp(outcome.PolicePressure + 5, 0, 100);
            outcome.CustodyRisk += 10;
        }
        else if (rawLastNightAns == 2)
        {
            outcome.PolicePressure = Mathf.Clamp(outcome.PolicePressure + 5, 0, 100);
            outcome.CustodyRisk += 5;
        }

        if (!coopRoute && stabbingWhoAnswered)
        {
            if (stabbingWhoAns == 0)
            {
                outcome.PolicePressure = Mathf.Clamp(outcome.PolicePressure + 10, 0, 100);
                outcome.CustodyRisk += 25;
            }
            else if (stabbingWhoAns == 1)
            {
                outcome.PolicePressure = Mathf.Clamp(outcome.PolicePressure + 5, 0, 100);
                outcome.CustodyRisk += 10;
            }
            else if (stabbingWhoAns == 2)
            {
                outcome.PolicePressure = Mathf.Clamp(outcome.PolicePressure + 5, 0, 100);
                outcome.CustodyRisk += 5;
            }
        }

        if (statementAnswered && statementAns == 0)
        {
            outcome.CulpritIdentified = true;
            outcome.Detained = DetainedTarget.Boss;
            outcome.BossKnownToPolice = true;
            outcome.StreetExposureIncreased = true;
            outcome.StartingBlackCash = 1200;
            outcome.PolicePressure = Mathf.Clamp(outcome.PolicePressure + 10, 0, 100);
            outcome.CustodyRisk += 25;
            outcome.CaseNote = "Boss confessed to the stabbing. Short prison term, but new prison contacts unlocked.";
        }
        else if (statementAnswered && statementAns == 1)
        {
            outcome.CulpritIdentified = true;
            outcome.Detained = (DetainedTarget)((int)DetainedTarget.Partner1 + Mathf.Clamp(namedAssocAns, 0, 2));
            outcome.BossKnownToPolice = false;
            outcome.StreetExposureIncreased = true;
            outcome.StartingBlackCash = 1200;
            outcome.CustodyRisk += 10;
            outcome.CaseNote = "An associate took the stabbing case. Crew keeps cash but gains street attention.";
        }

        return outcome;
    }

    public static PlayerCharacterProfile BuildProfile(string displayName, int accentIndex, int[] answers, string portraitResourcePath = "BossPortrait")
    {
        if (answers == null)
            answers = new int[QuestionCount];
        else if (answers.Length != QuestionCount)
        {
            int[] padded = new int[QuestionCount];
            for (int i = 0; i < Mathf.Min(answers.Length, QuestionCount); i++)
                padded[i] = answers[i];
            if (answers.Length < QuestionCount)
            {
                for (int i = answers.Length; i < QuestionCount; i++)
                    padded[i] = 0;
                if (answers.Length <= TraitQuestionCount)
                    padded[TraitQuestionCount] = Mathf.Clamp(accentIndex, 0, PlayerCharacterProfile.AccentColorCount - 1);
            }

            answers = padded;
        }

        int strengthPicks = 0;
        int agilityPicks = 0;
        int intelligencePicks = 0;
        int charismaPicks = 0;
        int mentalPicks = 0;
        int determinationPicks = 0;
        for (int q = 1; q < TraitQuestionCount; q++)
        {
            if (answers[q] < 0)
                continue;
            if (q == LastNightQuestionIndex || q == StabbingWhoQuestionIndex || !CountsTowardTraitPhilosophyPicks(answers, q))
                continue;
            int n = ChoiceLabels[q].Length;
            int choice = Mathf.Clamp(answers[q], 0, n - 1);
            switch (ChoiceTraitByIndex[choice])
            {
                case CoreTrait.Strength: strengthPicks++; break;
                case CoreTrait.Agility: agilityPicks++; break;
                case CoreTrait.Intelligence: intelligencePicks++; break;
                case CoreTrait.Charisma: charismaPicks++; break;
                case CoreTrait.MentalResilience: mentalPicks++; break;
                case CoreTrait.Determination: determinationPicks++; break;
            }
        }

        var profile = new PlayerCharacterProfile
        {
            DisplayName = string.IsNullOrWhiteSpace(displayName) ? "Boss" : displayName.Trim(),
            AccentColorIndex = Mathf.Clamp(accentIndex, 0, PlayerCharacterProfile.AccentColorCount - 1),
            PortraitResourcePath = string.IsNullOrWhiteSpace(portraitResourcePath) ? "BossPortrait" : portraitResourcePath.Trim(),
            QuestionnaireAnswers = (int[])answers.Clone(),
            Physical = 40f + strengthPicks * 10f,
            Agility = 40f + agilityPicks * 10f,
            Intelligence = 40f + intelligencePicks * 10f,
            Charisma = 40f + charismaPicks * 10f,
            MentalResilience = 40f + mentalPicks * 10f,
            Determination = 40f + determinationPicks * 10f,
            PublicReputation = 0,
            TraitRevealedMask = 0,
            CoopTryLuckTraitIndex = -1,
            CoopTryLuckPartnerGrantedXp = new int[3],
            CoopTryLuckPartnerTraitIndex = new int[] { -1, -1, -1 },
            TraitInterviewPotentialCeiling = new[]
            {
                Mathf.Clamp(strengthPicks + 1, 1, TraitPotentialRubric.MaxTraitLevel),
                Mathf.Clamp(agilityPicks + 1, 1, TraitPotentialRubric.MaxTraitLevel),
                Mathf.Clamp(intelligencePicks + 1, 1, TraitPotentialRubric.MaxTraitLevel),
                Mathf.Clamp(charismaPicks + 1, 1, TraitPotentialRubric.MaxTraitLevel),
                Mathf.Clamp(mentalPicks + 1, 1, TraitPotentialRubric.MaxTraitLevel),
                Mathf.Clamp(determinationPicks + 1, 1, TraitPotentialRubric.MaxTraitLevel)
            },
            TraitDirectedPracticeXp = new int[6]
        };

        profile.NormalizeTraitsTo100();
        profile.TraitXpRubricVersion = 2;
        profile.EnsureEquipmentDefaults();
        return profile;
    }
}
