using System.Collections.Generic;

public static class PersonalityTriggerTable
{
    static readonly Dictionary<PersonalityObservedEventType, PersonalityTriggerDelta[]> Map = BuildMap();

    public static PersonalityTriggerDelta[] GetDeltas(PersonalityObservedEventType evt)
    {
        if (Map.TryGetValue(evt, out var d)) return d;
        return System.Array.Empty<PersonalityTriggerDelta>();
    }

    static Dictionary<PersonalityObservedEventType, PersonalityTriggerDelta[]> BuildMap()
    {
        return new Dictionary<PersonalityObservedEventType, PersonalityTriggerDelta[]>
        {
            { PersonalityObservedEventType.FollowedOrdersUnderPressure, new[] {
                new PersonalityTriggerDelta(PersonalityTraitType.Disciplined, +2),
                new PersonalityTriggerDelta(PersonalityTraitType.Calm, +1)
            }},
            { PersonalityObservedEventType.BrokeProcedure, new[] {
                new PersonalityTriggerDelta(PersonalityTraitType.Disciplined, -2),
                new PersonalityTriggerDelta(PersonalityTraitType.Impulsive, +1)
            }},
            { PersonalityObservedEventType.ProtectedTeammateAtCost, new[] {
                new PersonalityTriggerDelta(PersonalityTraitType.Protective, +2),
                new PersonalityTriggerDelta(PersonalityTraitType.Loyal, +1),
                new PersonalityTriggerDelta(PersonalityTraitType.Brave, +1)
            }},
            { PersonalityObservedEventType.SoldInformationForGain, new[] {
                new PersonalityTriggerDelta(PersonalityTraitType.Treacherous, +2),
                new PersonalityTriggerDelta(PersonalityTraitType.MoneyGreedy, +2),
                new PersonalityTriggerDelta(PersonalityTraitType.Loyal, -2)
            }},
            { PersonalityObservedEventType.EnteredHighRiskFight, new[] {
                new PersonalityTriggerDelta(PersonalityTraitType.Brave, +2),
                new PersonalityTriggerDelta(PersonalityTraitType.Cowardly, -1)
            }},
            { PersonalityObservedEventType.FledFromThreat, new[] {
                new PersonalityTriggerDelta(PersonalityTraitType.Cowardly, +2),
                new PersonalityTriggerDelta(PersonalityTraitType.Brave, -1)
            }},
            { PersonalityObservedEventType.RefusedBribeForPrinciple, new[] {
                new PersonalityTriggerDelta(PersonalityTraitType.Ideological, +2),
                new PersonalityTriggerDelta(PersonalityTraitType.MoneyGreedy, -2)
            }},
            { PersonalityObservedEventType.TookBribe, new[] {
                new PersonalityTriggerDelta(PersonalityTraitType.MoneyGreedy, +2),
                new PersonalityTriggerDelta(PersonalityTraitType.Ideological, -2)
            }},
            { PersonalityObservedEventType.LongSurveillanceCompleted, new[] {
                new PersonalityTriggerDelta(PersonalityTraitType.Patient, +2),
                new PersonalityTriggerDelta(PersonalityTraitType.Methodical, +1)
            }},
            { PersonalityObservedEventType.PrematureActionTriggered, new[] {
                new PersonalityTriggerDelta(PersonalityTraitType.Impulsive, +2),
                new PersonalityTriggerDelta(PersonalityTraitType.Patient, -1)
            }},
            { PersonalityObservedEventType.ControlledResponseInCrisis, new[] {
                new PersonalityTriggerDelta(PersonalityTraitType.Calm, +2),
                new PersonalityTriggerDelta(PersonalityTraitType.Calculated, +1)
            }},
            { PersonalityObservedEventType.PanicResponseInCrisis, new[] {
                new PersonalityTriggerDelta(PersonalityTraitType.Cowardly, +1),
                new PersonalityTriggerDelta(PersonalityTraitType.Calm, -2)
            }},
            { PersonalityObservedEventType.DiscoveredHiddenLink, new[] {
                new PersonalityTriggerDelta(PersonalityTraitType.Curious, +2),
                new PersonalityTriggerDelta(PersonalityTraitType.Calculated, +1)
            }},
            { PersonalityObservedEventType.IgnoredCriticalLead, new[] {
                new PersonalityTriggerDelta(PersonalityTraitType.Curious, -2)
            }},
            { PersonalityObservedEventType.ImprovisedSuccessfulPlan, new[] {
                new PersonalityTriggerDelta(PersonalityTraitType.Creative, +2),
                new PersonalityTriggerDelta(PersonalityTraitType.Calculated, +1)
            }},
            { PersonalityObservedEventType.RepeatedTemplateAction, new[] {
                new PersonalityTriggerDelta(PersonalityTraitType.Conventional, +2),
                new PersonalityTriggerDelta(PersonalityTraitType.Creative, -1)
            }},
            { PersonalityObservedEventType.AdvancedCareerAtAnyCost, new[] {
                new PersonalityTriggerDelta(PersonalityTraitType.Ambitious, +2),
                new PersonalityTriggerDelta(PersonalityTraitType.Treacherous, +1)
            }},
            { PersonalityObservedEventType.AvoidedGrowthOpportunity, new[] {
                new PersonalityTriggerDelta(PersonalityTraitType.Complacent, +2),
                new PersonalityTriggerDelta(PersonalityTraitType.Ambitious, -1)
            }},
            { PersonalityObservedEventType.InfluencedCrowd, new[] {
                new PersonalityTriggerDelta(PersonalityTraitType.Charismatic, +2)
            }},
            { PersonalityObservedEventType.PublicTrustCollapse, new[] {
                new PersonalityTriggerDelta(PersonalityTraitType.Alienating, +2),
                new PersonalityTriggerDelta(PersonalityTraitType.Charismatic, -1)
            }},
            { PersonalityObservedEventType.DefendedInnerCircle, new[] {
                new PersonalityTriggerDelta(PersonalityTraitType.Protective, +2),
                new PersonalityTriggerDelta(PersonalityTraitType.Loyal, +1)
            }},
            { PersonalityObservedEventType.ExploitedWeakTarget, new[] {
                new PersonalityTriggerDelta(PersonalityTraitType.Predatory, +2),
                new PersonalityTriggerDelta(PersonalityTraitType.Protective, -1)
            }},
            { PersonalityObservedEventType.EgoEscalation, new[] {
                new PersonalityTriggerDelta(PersonalityTraitType.Proud, +2),
                new PersonalityTriggerDelta(PersonalityTraitType.Humble, -1)
            }},
            { PersonalityObservedEventType.AcceptedAccountability, new[] {
                new PersonalityTriggerDelta(PersonalityTraitType.Humble, +2),
                new PersonalityTriggerDelta(PersonalityTraitType.Proud, -1)
            }},
            { PersonalityObservedEventType.RanCounterIntelChecks, new[] {
                new PersonalityTriggerDelta(PersonalityTraitType.Suspicious, +2),
                new PersonalityTriggerDelta(PersonalityTraitType.Calculated, +1)
            }},
            { PersonalityObservedEventType.TrustedWithoutVerification, new[] {
                new PersonalityTriggerDelta(PersonalityTraitType.Trusting, +2),
                new PersonalityTriggerDelta(PersonalityTraitType.Suspicious, -1)
            }},
            { PersonalityObservedEventType.RevengeAction, new[] {
                new PersonalityTriggerDelta(PersonalityTraitType.Vengeful, +2),
                new PersonalityTriggerDelta(PersonalityTraitType.Forgiving, -1)
            }},
            { PersonalityObservedEventType.ChoseDeEscalation, new[] {
                new PersonalityTriggerDelta(PersonalityTraitType.Forgiving, +2),
                new PersonalityTriggerDelta(PersonalityTraitType.Vengeful, -1)
            }},
            { PersonalityObservedEventType.AppliedCruelPunishment, new[] {
                new PersonalityTriggerDelta(PersonalityTraitType.Cruel, +2),
                new PersonalityTriggerDelta(PersonalityTraitType.Compassionate, -1)
            }},
            { PersonalityObservedEventType.ShowedCompassion, new[] {
                new PersonalityTriggerDelta(PersonalityTraitType.Compassionate, +2),
                new PersonalityTriggerDelta(PersonalityTraitType.Cruel, -1)
            }},
            { PersonalityObservedEventType.TortureForInformation, new[] {
                new PersonalityTriggerDelta(PersonalityTraitType.Sadistic, +2, true),
                new PersonalityTriggerDelta(PersonalityTraitType.Cruel, +1, true),
                new PersonalityTriggerDelta(PersonalityTraitType.Merciful, -2, true)
            }},
            { PersonalityObservedEventType.ShowedMercyAfterControl, new[] {
                new PersonalityTriggerDelta(PersonalityTraitType.Merciful, +2),
                new PersonalityTriggerDelta(PersonalityTraitType.Sadistic, -1)
            }},
            { PersonalityObservedEventType.MultiStepStrategicPlan, new[] {
                new PersonalityTriggerDelta(PersonalityTraitType.Calculated, +2),
                new PersonalityTriggerDelta(PersonalityTraitType.Methodical, +1)
            }},
            { PersonalityObservedEventType.InstinctOnlyDecision, new[] {
                new PersonalityTriggerDelta(PersonalityTraitType.Instinctive, +2),
                new PersonalityTriggerDelta(PersonalityTraitType.Calculated, -1)
            }},
            { PersonalityObservedEventType.BuiltReliableProcess, new[] {
                new PersonalityTriggerDelta(PersonalityTraitType.Methodical, +2),
                new PersonalityTriggerDelta(PersonalityTraitType.Disciplined, +1)
            }},
            { PersonalityObservedEventType.ChaoticExecution, new[] {
                new PersonalityTriggerDelta(PersonalityTraitType.Chaotic, +2),
                new PersonalityTriggerDelta(PersonalityTraitType.Methodical, -1)
            }},
            { PersonalityObservedEventType.MajorBetrayalExperienced, new[] {
                new PersonalityTriggerDelta(PersonalityTraitType.Suspicious, +2, true),
                new PersonalityTriggerDelta(PersonalityTraitType.Paranoid, +2, true),
                new PersonalityTriggerDelta(PersonalityTraitType.Vengeful, +1, true),
                new PersonalityTriggerDelta(PersonalityTraitType.Trusting, -2, true)
            }},
            { PersonalityObservedEventType.TeammateKilledInOperation, new[] {
                new PersonalityTriggerDelta(PersonalityTraitType.Protective, +2, true),
                new PersonalityTriggerDelta(PersonalityTraitType.Vengeful, +2, true),
                new PersonalityTriggerDelta(PersonalityTraitType.Calm, -1, true)
            }},
            { PersonalityObservedEventType.SourceFamilyThreat, new[] {
                new PersonalityTriggerDelta(PersonalityTraitType.Cowardly, +2, true),
                new PersonalityTriggerDelta(PersonalityTraitType.Treacherous, +1, true),
                new PersonalityTriggerDelta(PersonalityTraitType.Loyal, -1, true)
            }},
        };
    }
}

