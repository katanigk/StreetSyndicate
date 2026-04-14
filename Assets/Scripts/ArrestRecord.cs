using System;
using System.Collections.Generic;

[Serializable]
public class ArrestRecord
{
    /// <summary>
    /// Back-compat: old single-cause field. Prefer <see cref="PrimaryCause"/> + <see cref="BonusCauses"/>.
    /// </summary>
    public ArrestCause Cause = ArrestCause.Unknown;

    /// <summary>Main (primary) arrest cause shown to the player.</summary>
    public ArrestCause PrimaryCause = ArrestCause.Unknown;

    /// <summary>Secondary causes ("bonus" counts) shown as additions.</summary>
    public List<ArrestCause> BonusCauses = new List<ArrestCause>();

    /// <summary>
    /// If the primary charge is dropped, the case collapses (no detention basis remains).
    /// </summary>
    public bool PrimaryChargeDropped;

    /// <summary>
    /// Bonus charges that were dropped (mitigation / sentence reduction). Keep separate from <see cref="BonusCauses"/>.
    /// </summary>
    public List<ArrestCause> DroppedBonusCauses = new List<ArrestCause>();
    public GameSessionState.AgencyId ArrestingAgency = GameSessionState.AgencyId.Police;
    public int DetainedAtDay = -1;
    public string Notes = "";

    /// <summary>Evidence items accumulated for this case (1920s: analog-only sources).</summary>
    public List<EvidenceItem> Evidence = new List<EvidenceItem>();

    public ArrestCause GetEffectivePrimary()
    {
        if (PrimaryChargeDropped)
            return ArrestCause.Unknown;
        if (PrimaryCause != ArrestCause.Unknown)
            return PrimaryCause;
        if (Cause != ArrestCause.Unknown)
            return Cause;
        return ArrestCause.Unknown;
    }

    public bool IsCaseCollapsed()
    {
        return PrimaryChargeDropped;
    }

    public void DropPrimaryCharge()
    {
        PrimaryChargeDropped = true;
    }

    public void DropBonusCharge(ArrestCause cause)
    {
        if (cause == ArrestCause.Unknown)
            return;
        if (DroppedBonusCauses == null)
            DroppedBonusCauses = new List<ArrestCause>();
        if (!DroppedBonusCauses.Contains(cause))
            DroppedBonusCauses.Add(cause);
    }

    public bool IsBonusChargeActive(ArrestCause cause)
    {
        if (cause == ArrestCause.Unknown)
            return false;
        if (BonusCauses == null || !BonusCauses.Contains(cause))
            return false;
        if (DroppedBonusCauses != null && DroppedBonusCauses.Contains(cause))
            return false;
        return true;
    }

    public static ArrestRecord CreateDefault(
        ArrestCause primary,
        GameSessionState.AgencyId agency,
        int day,
        string notes = "",
        params ArrestCause[] bonus)
    {
        ArrestRecord r = new ArrestRecord
        {
            Cause = primary, // keep old field populated for existing call sites / saved JSON
            PrimaryCause = primary,
            PrimaryChargeDropped = false,
            ArrestingAgency = agency,
            DetainedAtDay = day,
            Notes = notes ?? ""
        };

        if (bonus != null && bonus.Length > 0)
        {
            for (int i = 0; i < bonus.Length; i++)
            {
                ArrestCause b = bonus[i];
                if (b == ArrestCause.Unknown || b == primary)
                    continue;
                if (!r.BonusCauses.Contains(b))
                    r.BonusCauses.Add(b);
            }
        }

        return r;
    }
}

