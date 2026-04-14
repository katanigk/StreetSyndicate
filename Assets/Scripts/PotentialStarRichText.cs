using System.Text;
using UnityEngine;

/// <summary>
/// Core trait <b>potential</b> row: only full gold (earned tiers) or gray — no three-way state.
/// </summary>
public static class PotentialStarRichText
{
    public static string Build(int potentialTier, int maxStars = 5)
    {
        potentialTier = Mathf.Clamp(potentialTier, 0, maxStars);
        var sb = new StringBuilder(maxStars * 24);
        for (int i = 1; i <= maxStars; i++)
        {
            if (i <= potentialTier)
                sb.Append("<color=#FFD65C>★</color>");
            else
                sb.Append("<color=#626A75>★</color>");
        }

        return sb.ToString();
    }
}
