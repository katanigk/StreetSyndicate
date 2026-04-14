/// <summary>
/// Active player boss profile for the current run (memory; also persisted in save).
/// </summary>
public static class PlayerRunState
{
    public static PlayerCharacterProfile Character;

    public static bool HasCharacter => Character != null && !string.IsNullOrWhiteSpace(Character.DisplayName);

    public static void SetCharacter(PlayerCharacterProfile profile)
    {
        Character = profile;
    }

    public static void ClearCharacter()
    {
        Character = null;
    }
}
