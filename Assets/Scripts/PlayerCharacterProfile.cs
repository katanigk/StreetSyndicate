using System;
using UnityEngine;

/// <summary>
/// Player boss character: accent color, hidden core traits (revealed in-game), questionnaire answers for save/replay.
/// </summary>
[Serializable]
public class PlayerCharacterProfile
{
    public const int AccentColorCount = 8;

    /// <summary>Display labels matching <see cref="GetAccentPalette"/> order (family accent + vehicle color pick lists).</summary>
    public static readonly string[] AccentColorLabels =
    {
        "Red", "Steel", "Blue", "Turquoise", "Yellow", "Orange", "Purple", "Lilac"
    };

    public string DisplayName = "Boss";

    /// <summary>0..AccentColorCount-1 — red, green, blue, turquoise, yellow, orange, purple, lilac.</summary>
    public int AccentColorIndex;

    /// <summary>Per-question selected choice index (traits, color, three partner specialties — see PersonalityQuestionnaire.QuestionCount).</summary>
    public int[] QuestionnaireAnswers = Array.Empty<int>();

    /// <summary>Interrogation rubric XP for partners 0..2, flattened as 18 ints (3×6 <see cref="CoreTrait"/> order).</summary>
    public int[] CoopPartnerRubricBonusXp;

    /// <summary>Which core trait index (0..5) got the +50 restaurant roll per partner — for saves / display.</summary>
    public int[] CoopEveningRestaurantTraitRoll;

    /// <summary>
    /// Resource path for boss portrait texture (e.g. "BossPortrait", "BossPortraitF").
    /// </summary>
    public string PortraitResourcePath = "BossPortrait";

    /// <summary>0–100 internal; not shown to player until revealed by gameplay/UI.</summary>
    public float Physical;
    public float Agility;
    public float Intelligence;
    public float Charisma;
    public float MentalResilience;
    public float Determination;

    // Rubric XP (0..10 levels are derived from XP curve).
    public int StrengthXp;
    public int AgilityXp;
    public int IntelligenceXp;
    public int CharismaXp;
    public int MentalResilienceXp;
    public int DeterminationXp;

    /// <summary>
    /// Street reputation in public eyes, from -100 to +100.
    /// </summary>
    public int PublicReputation;

    /// <summary>Multi-channel reputation — design §8; each typically −50..+50.</summary>
    public int UnderworldRespect;
    public int StreetFear;
    public int PoliceAttentionChannel;
    public int BusinessCredibility;

    /// <summary>Per-skill XP (same order as <see cref="DerivedSkill"/> enum, length 13).</summary>
    public int[] DerivedSkillXp;

    /// <summary>0 = legacy trait XP; 1 = intermediate; 2 = potential derived from skills only.</summary>
    public int TraitXpRubricVersion;

    /// <summary>0 = legacy skill XP curve (100×L²); 1 = tiered ×3 skill rubric.</summary>
    public int SkillRubricVersion;

    /// <summary>
    /// Max potential tier (1..5) allowed by trait-question picks, <see cref="CoreTrait"/> order.
    /// Null or wrong length = no interview cap (legacy saves).
    /// </summary>
    public int[] TraitInterviewPotentialCeiling;

    /// <summary>
    /// Cumulative practice XP tagged to each <see cref="CoreTrait"/> (full amount per grant, before split into skills).
    /// Used with max gate-skill bank to unlock pillar potential tiers — length 6, same order as <see cref="CoreTrait"/>.
    /// </summary>
    public int[] TraitDirectedPracticeXp;

    /// <summary>Bitmask: bit i = trait i revealed to player.</summary>
    public int TraitRevealedMask;

    /// <summary>Co-op "try our luck" reason: rolled practice XP (0 if not that choice).</summary>
    public int CoopTryLuckGrantedXp;

    /// <summary>Co-op "try our luck": <see cref="CoreTrait"/> ordinal 0..5, or -1 if N/A.</summary>
    public int CoopTryLuckTraitIndex = -1;

    /// <summary>Co-op "try our luck" per partner 0..2: rolled practice XP (parallel to <see cref="CoopTryLuckPartnerTraitIndex"/>).</summary>
    public int[] CoopTryLuckPartnerGrantedXp;

    /// <summary>Co-op "try our luck" per partner: <see cref="CoreTrait"/> ordinal 0..5, or -1 if N/A.</summary>
    public int[] CoopTryLuckPartnerTraitIndex;

    /// <summary>Bar records "what is this place?" path: first trait ordinal per partner for split 100 rubric XP.</summary>
    public int[] CoopBarUnawarePartnerSplitTrait0;

    /// <summary>Second trait ordinal, or -1 if all 100 on <see cref="CoopBarUnawarePartnerSplitTrait0"/>.</summary>
    public int[] CoopBarUnawarePartnerSplitTrait1;

    public int[] CoopBarUnawarePartnerSplitXp0;
    public int[] CoopBarUnawarePartnerSplitXp1;

    [Serializable]
    public class HandLoadout
    {
        public string GripItem = "Empty";
        public string UtilityItem = "Empty";
        public string UseCondition = "Default";
        public int Priority = 1;
    }

    [Serializable]
    public class EquipmentLoadout
    {
        public string HeadSlot = "Flat cap";
        public HandLoadout RightHand = new HandLoadout();
        public HandLoadout LeftHand = new HandLoadout();
        public string BodyArmorSlot = "Light vest";
        public string AccessorySlot1 = "Signet ring (+charisma)";
        public string AccessorySlot2 = "Pocket watch (+focus)";
        public string BagSlot = "Messenger bag";
        public string BagCondition = "Carry only when needed";
        public int ExtraCapacity = 0;
        public string BagImageResourcePath = "BagPortrait";
        public string[] BagItems = Array.Empty<string>();
    }

    public EquipmentLoadout Equipment = new EquipmentLoadout();

    public static Color32[] GetAccentPalette()
    {
        return new[]
        {
            new Color32(200, 48, 48, 255),
            // Replaced pure green (too UI-dominant on HUD washes) with a cool steel.
            new Color32(92, 116, 148, 255),
            new Color32(48, 96, 200, 255),
            new Color32(48, 180, 190, 255),
            new Color32(220, 200, 48, 255),
            new Color32(220, 120, 40, 255),
            new Color32(140, 72, 200, 255),
            new Color32(190, 150, 210, 255)
        };
    }

    public static Color GetAccentColor(int index)
    {
        Color32[] p = GetAccentPalette();
        if (index < 0 || index >= p.Length)
            index = 0;
        return p[index];
    }

    /// <summary>
    /// Interview-derived cap on core potential tier. Legacy profiles (no array) return full <see cref="TraitPotentialRubric.MaxTraitLevel"/>.
    /// </summary>
    public static int GetInterviewPotentialCeilingTier(PlayerCharacterProfile profile, CoreTrait trait)
    {
        if (profile == null)
            return TraitPotentialRubric.MaxTraitLevel;
        int[] c = profile.TraitInterviewPotentialCeiling;
        if (c == null || c.Length != 6)
            return TraitPotentialRubric.MaxTraitLevel;
        int i = (int)trait;
        if (i < 0 || i >= c.Length)
            return TraitPotentialRubric.MaxTraitLevel;
        return Mathf.Clamp(c[i], 1, TraitPotentialRubric.MaxTraitLevel);
    }

    /// <summary>Total trait-tagged practice (pre-split) for pillar potential; 0 if unset.</summary>
    public static int GetDirectedTraitPracticeXp(PlayerCharacterProfile profile, CoreTrait trait)
    {
        if (profile?.TraitDirectedPracticeXp == null || profile.TraitDirectedPracticeXp.Length != 6)
            return 0;
        int i = (int)trait;
        if (i < 0 || i >= 6)
            return 0;
        return Mathf.Max(0, profile.TraitDirectedPracticeXp[i]);
    }

    public void NormalizeTraitsTo100()
    {
        float max = Mathf.Max(Physical, Agility, Intelligence, Charisma, MentalResilience, Determination, 1f);
        float s = 100f / max;
        Physical *= s;
        Agility *= s;
        Intelligence *= s;
        Charisma *= s;
        MentalResilience *= s;
        Determination *= s;
    }

    public void EnsureEquipmentDefaults()
    {
        if (Equipment == null)
            Equipment = new EquipmentLoadout();
        if (Equipment.RightHand == null)
            Equipment.RightHand = new HandLoadout();
        if (Equipment.LeftHand == null)
            Equipment.LeftHand = new HandLoadout();
        if (Equipment.BagItems == null)
            Equipment.BagItems = Array.Empty<string>();

        if (string.IsNullOrWhiteSpace(Equipment.RightHand.GripItem) || Equipment.RightHand.GripItem == "Empty")
            Equipment.RightHand.GripItem = "Handgun";
        if (string.IsNullOrWhiteSpace(Equipment.RightHand.UtilityItem) || Equipment.RightHand.UtilityItem == "Empty")
            Equipment.RightHand.UtilityItem = "Leather gloves";
        if (string.IsNullOrWhiteSpace(Equipment.RightHand.UseCondition) || Equipment.RightHand.UseCondition == "Default")
            Equipment.RightHand.UseCondition = "Use handgun only in hostile zones";
        if (Equipment.RightHand.Priority <= 0)
            Equipment.RightHand.Priority = 1;

        if (string.IsNullOrWhiteSpace(Equipment.LeftHand.GripItem) || Equipment.LeftHand.GripItem == "Empty")
            Equipment.LeftHand.GripItem = "Knife";
        if (string.IsNullOrWhiteSpace(Equipment.LeftHand.UtilityItem) || Equipment.LeftHand.UtilityItem == "Empty")
            Equipment.LeftHand.UtilityItem = "Brass knuckles";
        if (string.IsNullOrWhiteSpace(Equipment.LeftHand.UseCondition) || Equipment.LeftHand.UseCondition == "Default")
            Equipment.LeftHand.UseCondition = "Use for close-range or silent pressure";
        if (Equipment.LeftHand.Priority <= 0)
            Equipment.LeftHand.Priority = 2;

        if (string.IsNullOrWhiteSpace(Equipment.BagSlot))
            Equipment.BagSlot = "Messenger bag";
        if (string.IsNullOrWhiteSpace(Equipment.BagCondition))
            Equipment.BagCondition = "Carry only when needed";
        if (string.IsNullOrWhiteSpace(Equipment.BagImageResourcePath))
            Equipment.BagImageResourcePath = "BagPortrait";
    }
}
