using UnityEngine;

/// <summary>
/// Use for map markers / future crew tint: reads active player accent from PlayerRunState.
/// </summary>
public static class PlayerAccentTint
{
    public static Color GetAccentColorOrNeutral()
    {
        if (PlayerRunState.HasCharacter && PlayerRunState.Character != null)
            return PlayerCharacterProfile.GetAccentColor(PlayerRunState.Character.AccentColorIndex);
        return new Color(0.55f, 0.55f, 0.58f, 1f);
    }
}
