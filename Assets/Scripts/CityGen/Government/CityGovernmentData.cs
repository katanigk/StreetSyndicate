using System.Collections.Generic;
using FamilyBusiness.CityGen.Data;
using FamilyBusiness.CityGen.Discovery;

namespace FamilyBusiness.CityGen.Government
{
    /// <summary>Batch 12: city-scoped extracted view of government-relevant institutions.</summary>
    public sealed class CityGovernmentData
    {
        readonly List<GovernmentFacilityData> _all = new List<GovernmentFacilityData>();
        readonly Dictionary<int, GovernmentFacilityData> _byInstitutionId = new Dictionary<int, GovernmentFacilityData>();
        readonly Dictionary<GovernmentSystemKind, List<GovernmentFacilityData>> _bySystem =
            new Dictionary<GovernmentSystemKind, List<GovernmentFacilityData>>();

        public IReadOnlyList<GovernmentFacilityData> AllFacilities => _all;

        internal void ClearAndRebuildIndices()
        {
            _all.Clear();
            _byInstitutionId.Clear();
            foreach (GovernmentSystemKind k in System.Enum.GetValues(typeof(GovernmentSystemKind)))
            {
                if (!_bySystem.ContainsKey(k))
                    _bySystem[k] = new List<GovernmentFacilityData>();
                else
                    _bySystem[k].Clear();
            }
        }

        internal void AddFacility(GovernmentFacilityData f)
        {
            _all.Add(f);
            _byInstitutionId[f.SourceInstitutionId] = f;
            if (!_bySystem.TryGetValue(f.GovernmentSystem, out List<GovernmentFacilityData> list))
            {
                list = new List<GovernmentFacilityData>();
                _bySystem[f.GovernmentSystem] = list;
            }

            list.Add(f);
        }

        public GovernmentFacilityData GetFacilityByInstitutionId(int institutionId) =>
            _byInstitutionId.TryGetValue(institutionId, out GovernmentFacilityData f) ? f : null;

        public IReadOnlyList<GovernmentFacilityData> GetFacilities(GovernmentSystemKind kind) =>
            _bySystem.TryGetValue(kind, out List<GovernmentFacilityData> list) ? list : System.Array.Empty<GovernmentFacilityData>();

        public IReadOnlyList<GovernmentFacilityData> GetVisibleFacilities(GovernmentSystemKind kind)
        {
            if (!_bySystem.TryGetValue(kind, out List<GovernmentFacilityData> list) || list.Count == 0)
                return System.Array.Empty<GovernmentFacilityData>();

            var tmp = new List<GovernmentFacilityData>(list.Count);
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].IsVisibleOnMapNow)
                    tmp.Add(list[i]);
            }

            return tmp;
        }

        public IReadOnlyList<GovernmentFacilityData> GetKnownFacilities(GovernmentSystemKind kind)
        {
            if (!_bySystem.TryGetValue(kind, out List<GovernmentFacilityData> list) || list.Count == 0)
                return System.Array.Empty<GovernmentFacilityData>();

            var tmp = new List<GovernmentFacilityData>(list.Count);
            for (int i = 0; i < list.Count; i++)
            {
                if (DiscoveryStateOrdering.IsAtLeast(list[i].DiscoveryStateSnapshot, DiscoveryState.Known))
                    tmp.Add(list[i]);
            }

            return tmp;
        }

        public IReadOnlyList<GovernmentFacilityData> GetFacilitiesInDistrict(int districtId)
        {
            if (_all.Count == 0)
                return System.Array.Empty<GovernmentFacilityData>();

            var tmp = new List<GovernmentFacilityData>();
            for (int i = 0; i < _all.Count; i++)
            {
                if (_all[i].DistrictId == districtId)
                    tmp.Add(_all[i]);
            }

            return tmp;
        }

        public IReadOnlyList<GovernmentFacilityData> GetIntelEligibleFacilities(GovernmentSystemKind kind)
        {
            if (!_bySystem.TryGetValue(kind, out List<GovernmentFacilityData> list) || list.Count == 0)
                return System.Array.Empty<GovernmentFacilityData>();

            var tmp = new List<GovernmentFacilityData>(list.Count);
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].PlayerHasIntelAtLeastRumored)
                    tmp.Add(list[i]);
            }

            return tmp;
        }

        public IReadOnlyList<GovernmentFacilityData> GetAllVisibleFacilities()
        {
            if (_all.Count == 0)
                return System.Array.Empty<GovernmentFacilityData>();

            var tmp = new List<GovernmentFacilityData>();
            for (int i = 0; i < _all.Count; i++)
            {
                if (_all[i].IsVisibleOnMapNow)
                    tmp.Add(_all[i]);
            }

            return tmp;
        }

        public IReadOnlyList<GovernmentFacilityData> GetAllKnownFacilities()
        {
            if (_all.Count == 0)
                return System.Array.Empty<GovernmentFacilityData>();

            var tmp = new List<GovernmentFacilityData>();
            for (int i = 0; i < _all.Count; i++)
            {
                if (DiscoveryStateOrdering.IsAtLeast(_all[i].DiscoveryStateSnapshot, DiscoveryState.Known))
                    tmp.Add(_all[i]);
            }

            return tmp;
        }
    }
}
