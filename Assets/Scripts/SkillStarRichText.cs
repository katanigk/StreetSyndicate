using System.Text;
using UnityEngine;

/// <summary>
/// Derived skills: (1) full gold = current level achieved;
/// (2) gold-outline hollow ☆ = within potential cap, not yet earned;
/// (3) gray ★ = beyond cap / not in play for this track.
/// </summary>
public static class SkillStarRichText
{
    public static string Build(int currentLevel, int capLevel, int displaySlots = 10)
    {
        currentLevel = Mathf.Clamp(currentLevel, 0, displaySlots);
        capLevel = Mathf.Clamp(capLevel, 0, displaySlots);
        var sb = new StringBuilder(displaySlots * 24);
        for (int i = 1; i <= displaySlots; i++)
        {
            if (i <= currentLevel)
                sb.Append("<color=#FFD65C>★</color>");
            else if (i <= capLevel)
                sb.Append("<color=#FFD65C>☆</color>");
            else
                sb.Append("<color=#626A75>★</color>");
        }

        return sb.ToString();
    }
}
