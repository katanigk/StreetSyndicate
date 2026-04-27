using System;
using UnityEngine;

[Flags]
public enum PersonToOrgHiddenFlags
{
    None = 0,
    FeelsUsed = 1 << 0,
    OwesOrganization = 1 << 1,
    FamilyProtectedByOrg = 1 << 2,
    UnderInternalThreat = 1 << 3,
    WatchingExitOpportunity = 1 << 4,
    CandidateForPromotion = 1 << 5,
    BlackmailedByOrg = 1 << 6
}

[Flags]
public enum OrgToOrgHiddenFlags
{
    None = 0,
    TradeOpen = 1 << 0,
    FragilePeace = 1 << 1,
    ProxyConflict = 1 << 2,
    NegotiationOpen = 1 << 3,
    MutualThreat = 1 << 4,
    PreparingWar = 1 << 5,
    SharedMarketDependence = 1 << 6,
    UnderInfiltration = 1 << 7
}

public enum RelationOperationalStatus
{
    Active,
    Limited,
    Strained,
    Frozen,
    Broken
}

public enum PersonToPersonSpectrumLabel
{
    FriendsBest,
    FriendsClose,
    Friends,
    Like,
    Acquaintance,
    Dislike,
    Hostile,
    Enemy,
    BitterEnemy
}

public enum PersonToOrgSpectrumLabel
{
    Devoted,
    Loyal,
    Attached,
    Cooperative,
    Neutral,
    Detached,
    Resentful,
    Disloyal,
    ReadyToFlip
}

public enum OrgToOrgSpectrumLabel
{
    Allied,
    Friendly,
    Cooperative,
    Neutral,
    Suspicious,
    Hostile,
    AtWar
}

[Serializable]
public class PersonToPersonRelation
{
    public string PersonAId;
    public string PersonBId;

    public int Affinity;
    public int Fear;
    public int Respect;
    public int Interest;

    public PersonToPersonSpectrumLabel SpectrumLabel = PersonToPersonSpectrumLabel.Acquaintance;
    public RelationOperationalStatus OperationalStatus = RelationOperationalStatus.Active;
    public int HiddenFlags;
    public long LastUpdatedUnixSeconds;
    public string EventHistoryRef;

    public void ClampAxes()
    {
        Affinity = SocialRelationMath.ClampSignedAxis(Affinity);
        Fear = SocialRelationMath.ClampFearAxis(Fear);
        Respect = SocialRelationMath.ClampSignedAxis(Respect);
        Interest = SocialRelationMath.ClampSignedAxis(Interest);
    }
}

[Serializable]
public class PersonToOrgRelation
{
    public string PersonId;
    public string OrgId;

    public int Affinity;
    public int Fear;
    public int Respect;
    public int Interest;

    public PersonToOrgSpectrumLabel SpectrumLabel = PersonToOrgSpectrumLabel.Neutral;
    public RelationOperationalStatus OperationalStatus = RelationOperationalStatus.Active;
    public PersonToOrgHiddenFlags HiddenFlags = PersonToOrgHiddenFlags.None;
    public long LastUpdatedUnixSeconds;
    public string EventHistoryRef;

    public void ClampAxes()
    {
        Affinity = SocialRelationMath.ClampSignedAxis(Affinity);
        Fear = SocialRelationMath.ClampFearAxis(Fear);
        Respect = SocialRelationMath.ClampSignedAxis(Respect);
        Interest = SocialRelationMath.ClampSignedAxis(Interest);
    }
}

[Serializable]
public class OrgToOrgRelation
{
    public string SourceOrgId;
    public string TargetOrgId;

    public int Affinity;
    public int Fear;
    public int Respect;
    public int Interest;

    public OrgToOrgSpectrumLabel SpectrumLabel = OrgToOrgSpectrumLabel.Neutral;
    public RelationOperationalStatus OperationalStatus = RelationOperationalStatus.Active;
    public OrgToOrgHiddenFlags HiddenFlags = OrgToOrgHiddenFlags.None;
    public long LastUpdatedUnixSeconds;
    public string EventHistoryRef;

    public void ClampAxes()
    {
        Affinity = SocialRelationMath.ClampSignedAxis(Affinity);
        Fear = SocialRelationMath.ClampFearAxis(Fear);
        Respect = SocialRelationMath.ClampSignedAxis(Respect);
        Interest = SocialRelationMath.ClampSignedAxis(Interest);
    }
}

public static class SocialRelationThresholds
{
    // Person <-> Org spectrum thresholds based on normalized bond score.
    public const int PersonOrgDevotedMin = 80;
    public const int PersonOrgLoyalMin = 55;
    public const int PersonOrgAttachedMin = 30;
    public const int PersonOrgCooperativeMin = 10;
    public const int PersonOrgNeutralMin = -9;
    public const int PersonOrgDetachedMin = -29;
    public const int PersonOrgResentfulMin = -54;
    public const int PersonOrgDisloyalMin = -79;

    // Org <-> Org thresholds are intentionally hard-coded now but centralized for later balancing.
    public const int OrgOrgAlliedMin = 80;
    public const int OrgOrgFriendlyMin = 50;
    public const int OrgOrgCooperativeMin = 20;
    public const int OrgOrgNeutralMin = -19;
    public const int OrgOrgSuspiciousMin = -49;
    public const int OrgOrgHostileMin = -79;
}

public static class SocialRelationMath
{
    public static int ClampSignedAxis(int value) => Mathf.Clamp(value, -100, 100);
    public static int ClampFearAxis(int value) => Mathf.Clamp(value, 0, 100);

    /// <summary>
    /// Bond score excludes fear on purpose. Fear can hold compliance, not real loyalty.
    /// </summary>
    public static int ComputeBondScore(int affinity, int respect, int interest)
    {
        int a = ClampSignedAxis(affinity);
        int r = ClampSignedAxis(respect);
        int i = ClampSignedAxis(interest);
        return Mathf.Clamp(Mathf.RoundToInt((a + r + i) / 3f), -100, 100);
    }
}
