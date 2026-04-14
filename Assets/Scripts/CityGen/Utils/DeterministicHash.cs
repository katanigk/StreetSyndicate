namespace FamilyBusiness.CityGen.Utils
{
    /// <summary>
    /// Small deterministic mixing for sub-seeds (pipeline stages). Keeps generation independent of System.Random version quirks where possible.
    /// </summary>
    public static class DeterministicHash
    {
        public static int Mix(int seed, int salt)
        {
            unchecked
            {
                uint x = (uint)(seed ^ salt);
                x ^= x >> 16;
                x *= 0x7FEB_352D;
                x ^= x >> 15;
                x *= 0x846C_84B7;
                x ^= x >> 16;
                return (int)x;
            }
        }

        public static int Mix(int seed, string salt)
        {
            if (string.IsNullOrEmpty(salt))
                return Mix(seed, 0);
            int h = salt.GetHashCode();
            return Mix(seed, h);
        }
    }
}
