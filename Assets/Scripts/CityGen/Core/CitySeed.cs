using FamilyBusiness.CityGen.Utils;
using UnityEngine;

namespace FamilyBusiness.CityGen.Core
{
    /// <summary>
    /// Deterministic seed wrapper for the city generator pipeline.
    /// </summary>
    public sealed class CitySeed
    {
        readonly int _value;

        public int Value => _value;

        public CitySeed(int seed)
        {
            _value = seed;
        }

        public static CitySeed FromExplicit(int seed) => new CitySeed(seed);

        /// <summary>Random seed for a new run (non-deterministic across sessions).</summary>
        public static CitySeed FromRandom() =>
            new CitySeed(UnityEngine.Random.Range(1, int.MaxValue));

        /// <summary>Sub-seed for an isolated pipeline stage (deterministic).</summary>
        public CitySeed Fork(int salt) => new CitySeed(DeterministicHash.Mix(_value, salt));

        public CitySeed Fork(string stageName) => new CitySeed(DeterministicHash.Mix(_value, stageName));

        /// <summary>Fresh <see cref="System.Random"/> for this seed value.</summary>
        public System.Random CreateSystemRandom() => new System.Random(_value);

        /// <summary>Integer in [minInclusive, maxExclusive).</summary>
        public int NextInt(System.Random rng, int minInclusive, int maxExclusive) =>
            rng.Next(minInclusive, maxExclusive);

        /// <summary>Float in [0, 1).</summary>
        public float NextFloat01(System.Random rng) =>
            (float)rng.NextDouble();
    }
}
