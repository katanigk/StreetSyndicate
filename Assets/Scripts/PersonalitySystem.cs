using System;
using System.Collections.Generic;
using UnityEngine;

public enum PersonalityTraitType
{
    Disciplined = 0,
    Loyal = 1,
    Brave = 2,
    Ideological = 3,
    Patient = 4,
    Calm = 5,
    Curious = 6,
    Creative = 7,
    Ambitious = 8,
    Charismatic = 9,
    Protective = 10,
    Impulsive = 11,
    MoneyGreedy = 12,
    Proud = 13,
    Suspicious = 14,
    Paranoid = 15,
    Vengeful = 16,
    Cruel = 17,
    Cowardly = 18,
    Sadistic = 19,
    Treacherous = 20,
    Calculated = 21,
    Methodical = 22,
    Undisciplined = 23,
    Impatient = 24,
    Reactive = 25,
    Indifferent = 26,
    Conventional = 27,
    Complacent = 28,
    Alienating = 29,
    Predatory = 30,
    Humble = 31,
    Trusting = 32,
    Secure = 33,
    Forgiving = 34,
    Compassionate = 35,
    Merciful = 36,
    Instinctive = 37,
    Chaotic = 38
}

public enum PersonalityVisibility
{
    Unknown = 0,
    Suspected = 1,
    Known = 2,
    Confirmed = 3
}

public enum PersonalityTraitSource
{
    Birth = 0,
    Behavior = 1,
    Trauma = 2,
    Training = 3,
    Environment = 4
}

public enum PersonalityTrendDirection
{
    Rising = 0,
    Stable = 1,
    Falling = 2
}

public enum PersonalityProgressTrigger
{
    RepeatedBehavior = 0,
    MajorEvent = 1,
    Environment = 2,
    Training = 3
}

[Serializable]
public class PersonalityTraitInstance
{
    public int traitTypeInt;
    public int intensity; // 1..3
    public int stability; // 0..100
    public int visibilityInt;
    public int sourceInt;
    public int trendDirectionInt;
}

[Serializable]
public class PersonalityTraitProgress
{
    public string characterId;
    public int traitTypeInt;
    public int progressXp;
    public int decayXp;
    public int lastTriggerInt;
    public int repeatedBehaviorCount;
    public int majorEventCount;
    public int environmentPressure;
    public int trendDirectionInt;
}

[Serializable]
public class PersonalityProfile
{
    public string characterId;
    public bool isLeaderContext;
    public bool isSubordinateContext;
    public bool isCivilianContext;
    public bool isCriminalContext;
    public bool isPoliceContext;
    public bool isFederalContext;
    public List<PersonalityTraitInstance> traits = new List<PersonalityTraitInstance>();
}

[Serializable]
public class PersonalityWorldSnapshot
{
    public int formatVersion;
    public List<PersonalityProfile> profiles = new List<PersonalityProfile>();
    public List<PersonalityTraitProgress> progress = new List<PersonalityTraitProgress>();
}

public readonly struct PersonalityContext
{
    public readonly bool IsLeaderContext;
    public readonly bool IsSubordinateContext;
    public readonly bool IsCivilianContext;
    public readonly bool IsCriminalContext;
    public readonly bool IsPoliceContext;
    public readonly bool IsFederalContext;

    public PersonalityContext(
        bool isLeaderContext,
        bool isSubordinateContext,
        bool isCivilianContext,
        bool isCriminalContext,
        bool isPoliceContext,
        bool isFederalContext)
    {
        IsLeaderContext = isLeaderContext;
        IsSubordinateContext = isSubordinateContext;
        IsCivilianContext = isCivilianContext;
        IsCriminalContext = isCriminalContext;
        IsPoliceContext = isPoliceContext;
        IsFederalContext = isFederalContext;
    }
}

public readonly struct PersonalityTraitEffect
{
    public readonly int ComplianceDelta;
    public readonly int LeadershipStabilityDelta;
    public readonly int RiskTakingDelta;
    public readonly int DeceptionDelta;
    public readonly int IntelligenceQualityDelta;

    public PersonalityTraitEffect(
        int complianceDelta,
        int leadershipStabilityDelta,
        int riskTakingDelta,
        int deceptionDelta,
        int intelligenceQualityDelta)
    {
        ComplianceDelta = complianceDelta;
        LeadershipStabilityDelta = leadershipStabilityDelta;
        RiskTakingDelta = riskTakingDelta;
        DeceptionDelta = deceptionDelta;
        IntelligenceQualityDelta = intelligenceQualityDelta;
    }
}

public readonly struct PersonalityRuleTuning
{
    public readonly int RepeatedGain;
    public readonly int MajorEventGain;
    public readonly int OppositeDecay;
    public readonly int EnvironmentGainPerStep;

    public PersonalityRuleTuning(int repeatedGain, int majorEventGain, int oppositeDecay, int environmentGainPerStep)
    {
        RepeatedGain = repeatedGain;
        MajorEventGain = majorEventGain;
        OppositeDecay = oppositeDecay;
        EnvironmentGainPerStep = environmentGainPerStep;
    }
}

public enum PersonalityObservedEventType
{
    FollowedOrdersUnderPressure = 0,
    BrokeProcedure = 1,
    ProtectedTeammateAtCost = 2,
    SoldInformationForGain = 3,
    EnteredHighRiskFight = 4,
    FledFromThreat = 5,
    RefusedBribeForPrinciple = 6,
    TookBribe = 7,
    LongSurveillanceCompleted = 8,
    PrematureActionTriggered = 9,
    ControlledResponseInCrisis = 10,
    PanicResponseInCrisis = 11,
    DiscoveredHiddenLink = 12,
    IgnoredCriticalLead = 13,
    ImprovisedSuccessfulPlan = 14,
    RepeatedTemplateAction = 15,
    AdvancedCareerAtAnyCost = 16,
    AvoidedGrowthOpportunity = 17,
    InfluencedCrowd = 18,
    PublicTrustCollapse = 19,
    DefendedInnerCircle = 20,
    ExploitedWeakTarget = 21,
    EgoEscalation = 22,
    AcceptedAccountability = 23,
    RanCounterIntelChecks = 24,
    TrustedWithoutVerification = 25,
    RevengeAction = 26,
    ChoseDeEscalation = 27,
    AppliedCruelPunishment = 28,
    ShowedCompassion = 29,
    TortureForInformation = 30,
    ShowedMercyAfterControl = 31,
    MultiStepStrategicPlan = 32,
    InstinctOnlyDecision = 33,
    BuiltReliableProcess = 34,
    ChaoticExecution = 35,
    MajorBetrayalExperienced = 36,
    TeammateKilledInOperation = 37,
    SourceFamilyThreat = 38
}

public readonly struct PersonalityTriggerDelta
{
    public readonly PersonalityTraitType TraitType;
    public readonly int Weight;
    public readonly bool IsMajorEvent;

    public PersonalityTriggerDelta(PersonalityTraitType traitType, int weight, bool isMajorEvent = false)
    {
        TraitType = traitType;
        Weight = weight;
        IsMajorEvent = isMajorEvent;
    }
}

