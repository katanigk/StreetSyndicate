using System;
using UnityEngine;

/// <summary>
/// Runtime state for a revenue / tax case (Capone path: books → assessment → liens → criminal referral).
/// Expand as gameplay hooks land; persistence can be added to <see cref="SaveGameData"/> later.
/// </summary>
[Serializable]
public sealed class TaxAuthorityCaseState
{
    public int OpenedAtDay = -1;

    /// <summary>0 = none, 1 = informal notice, 2 = formal audit, 3 = assessment issued, 4 = enforcement, 5 = criminal referral.</summary>
    public int Stage;

    public int AssessedBackTaxesUsd;
    public int AccruedInterestAndPenaltiesUsd;
    public int FrozenAssetsUsd;
    public bool CriminalReferralPending;

    public void Reset()
    {
        OpenedAtDay = -1;
        Stage = 0;
        AssessedBackTaxesUsd = 0;
        AccruedInterestAndPenaltiesUsd = 0;
        FrozenAssetsUsd = 0;
        CriminalReferralPending = false;
    }

    public int TotalLiabilityUsd => Mathf.Max(0, AssessedBackTaxesUsd + AccruedInterestAndPenaltiesUsd);
}
