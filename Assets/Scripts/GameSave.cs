using System;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Minimal JSON save to persistent data path. Expand fields as gameplay systems grow.
/// </summary>
[Serializable]
public class SaveGameData
{
    public int FormatVersion = 12;
    public int CurrentDay;
    public int CrewCash;
    public bool ScoutMissionOrdered;
    public int LastDayMissionsCompleted;
    public int LastDayMissionsFailed;
    public int LastDaySoldiersReleased;
    public int ExecutionDayDurationSeconds;
    public bool BossIsPoliceInformant;
    public bool BossSnitchKnownToRivalGangs;
    public int BossSnitchStreetRevealAtDay = -1;
    public bool UnderworldWarDeclaredOnPlayerFamily;
    public bool PoliceInformantStreetFalloutApplied;
    public bool BossCustodyTrialCompleted;
    public bool BossStartsInPrison;
    public string InitialDetainedCharacterName;
    public int CityMapSeed;
    public int PlayerOrganizationStage;
    public int PlayerThreatScore;
    public int FederalBureauState;
    public int PlayerIntelNetworkRating;
    public int GameMode;
    public string ActiveSceneName;

    /// <summary>JsonUtility payload for PlayerCharacterProfile, or empty.</summary>
    public string PlayerProfileJson;

    /// <summary>Micro-block rent / landlord delta; spots rebuilt from seed on load.</summary>
    public string MicroBlockStateJson;

    /// <summary>Sandbox Ops map: block ids revealed by travel (macro fog cleared).</summary>
    public int[] SandboxRevealedBlockIds;

    public string PoliceStateJson;
    public string BureauStateJson;
    public string PersonalityStateJson;

    public static SaveGameData CaptureFromSession()
    {
        Scene scene = SceneManager.GetActiveScene();
        int mode = 0;
        if (GameModeManager.Instance != null)
            mode = (int)GameModeManager.Instance.CurrentMode;

        string profileJson = "";
        if (PlayerRunState.HasCharacter && PlayerRunState.Character != null)
            profileJson = JsonUtility.ToJson(PlayerRunState.Character);

        int day = GameSessionState.CurrentDay >= 1 ? GameSessionState.CurrentDay : 1;
        PoliceWorldState.EnsureBootstrappedForSession(GameSessionState.CityMapSeed, day);
        BureauWorldState.EnsureBootstrappedForSession(GameSessionState.CityMapSeed, day);
        return new SaveGameData
        {
            FormatVersion = 12,
            CurrentDay = GameSessionState.CurrentDay,
            CrewCash = GameSessionState.CrewCash,
            ScoutMissionOrdered = GameSessionState.ScoutMissionOrdered,
            LastDayMissionsCompleted = GameSessionState.LastDayMissionsCompleted,
            LastDayMissionsFailed = GameSessionState.LastDayMissionsFailed,
            LastDaySoldiersReleased = GameSessionState.LastDaySoldiersReleased,
            ExecutionDayDurationSeconds = GameSessionState.ExecutionDayDurationSeconds,
            BossIsPoliceInformant = GameSessionState.BossIsPoliceInformant,
            BossSnitchKnownToRivalGangs = GameSessionState.BossSnitchKnownToRivalGangs,
            BossSnitchStreetRevealAtDay = GameSessionState.BossSnitchStreetRevealAtDay,
            UnderworldWarDeclaredOnPlayerFamily = GameSessionState.UnderworldWarDeclaredOnPlayerFamily,
            PoliceInformantStreetFalloutApplied = GameSessionState.PoliceInformantStreetFalloutApplied,
            BossCustodyTrialCompleted = GameSessionState.BossCustodyTrialCompleted,
            BossStartsInPrison = GameSessionState.BossStartsInPrison,
            InitialDetainedCharacterName = GameSessionState.InitialDetainedCharacterName ?? string.Empty,
            CityMapSeed = GameSessionState.CityMapSeed,
            PlayerOrganizationStage = (int)GameSessionState.PlayerOrganizationStage,
            PlayerThreatScore = GameSessionState.PlayerThreatScore,
            FederalBureauState = (int)GameSessionState.FederalBureauState,
            PlayerIntelNetworkRating = GameSessionState.PlayerIntelNetworkRating,
            GameMode = mode,
            ActiveSceneName = scene.IsValid() ? scene.name : "PlanningScene",
            PlayerProfileJson = profileJson,
            MicroBlockStateJson = MicroBlockPersistence.CaptureJson(),
            SandboxRevealedBlockIds = GameSessionState.GetSandboxRevealedBlockIdsSnapshot(),
            PoliceStateJson = PoliceWorldState.CaptureJson(),
            BureauStateJson = BureauWorldState.CaptureJson(),
            PersonalityStateJson = PersonalityWorldState.CaptureJson()
        };
    }
}

public static class GameSave
{
    private const string FileName = "streetsyndicate_save.json";
    private const string FilePattern = "streetsyndicate_save*.json";

    public static string SaveFilePath => Path.Combine(Application.persistentDataPath, FileName);

    public static bool HasSaveFile()
    {
        return File.Exists(SaveFilePath);
    }

    public static string[] GetSaveFilePaths()
    {
        try
        {
            string dir = Application.persistentDataPath;
            if (!Directory.Exists(dir))
                return Array.Empty<string>();

            string[] files = Directory.GetFiles(dir, FilePattern);
            Array.Sort(files, (a, b) =>
            {
                DateTime ta = File.GetLastWriteTimeUtc(a);
                DateTime tb = File.GetLastWriteTimeUtc(b);
                return tb.CompareTo(ta); // newest first
            });
            return files;
        }
        catch
        {
            return Array.Empty<string>();
        }
    }

    public static bool TrySave(out string errorMessage)
    {
        errorMessage = null;
        try
        {
            SaveGameData data = SaveGameData.CaptureFromSession();
            File.WriteAllText(SaveFilePath, JsonUtility.ToJson(data, true));
            return true;
        }
        catch (Exception e)
        {
            errorMessage = e.Message;
            return false;
        }
    }

    public static bool TryLoad(out string errorMessage)
    {
        return TryLoadFromPath(SaveFilePath, out errorMessage);
    }

    public static bool TryLoadFromPath(string path, out string errorMessage)
    {
        errorMessage = null;
        try
        {
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            {
                errorMessage = "No save file found.";
                return false;
            }

            string json = File.ReadAllText(path);
            SaveGameData data = JsonUtility.FromJson<SaveGameData>(json);
            if (data == null)
            {
                errorMessage = "Save file is invalid.";
                return false;
            }

            ApplyToSession(data);
            return true;
        }
        catch (Exception e)
        {
            errorMessage = e.Message;
            return false;
        }
    }

    private static void ApplyToSession(SaveGameData d)
    {
        GameSessionState.CurrentDay = d.CurrentDay < 1 ? 1 : d.CurrentDay;
        if (d.FormatVersion < 3)
            GameSessionState.CrewCash = GameSessionState.StartingCrewCash;
        else
            GameSessionState.CrewCash = Mathf.Max(0, d.CrewCash);
        GameSessionState.ScoutMissionOrdered = d.ScoutMissionOrdered;
        GameSessionState.LastDayMissionsCompleted = d.LastDayMissionsCompleted;
        GameSessionState.LastDayMissionsFailed = d.LastDayMissionsFailed;
        GameSessionState.LastDaySoldiersReleased = d.LastDaySoldiersReleased;
        GameSessionState.ExecutionDayDurationSeconds = d.ExecutionDayDurationSeconds > 0 ? d.ExecutionDayDurationSeconds : 10;
        GameSessionState.BossIsPoliceInformant = d.BossIsPoliceInformant;
        GameSessionState.BossSnitchKnownToRivalGangs = d.BossSnitchKnownToRivalGangs;
        GameSessionState.BossSnitchStreetRevealAtDay = d.BossSnitchStreetRevealAtDay;
        GameSessionState.UnderworldWarDeclaredOnPlayerFamily = d.UnderworldWarDeclaredOnPlayerFamily;
        GameSessionState.PoliceInformantStreetFalloutApplied = d.PoliceInformantStreetFalloutApplied;
        GameSessionState.BossCustodyTrialCompleted = d.BossCustodyTrialCompleted;
        GameSessionState.BossStartsInPrison = d.BossStartsInPrison;
        GameSessionState.InitialDetainedCharacterName = d.InitialDetainedCharacterName ?? string.Empty;
        GameSessionState.CityMapSeed = d.CityMapSeed != 0
            ? d.CityMapSeed
            : UnityEngine.Random.Range(1, int.MaxValue);
        GameSessionState.InvalidateActiveCityData();
        MicroBlockBootstrap.ResetAndBuildForNewGame(GameSessionState.CityMapSeed);
        if (d.FormatVersion >= 5 && !string.IsNullOrEmpty(d.MicroBlockStateJson))
            MicroBlockPersistence.ApplyJson(d.MicroBlockStateJson);
        GameSessionState.ClearSandboxRevealedBlocks();
        if (d.FormatVersion >= 6 && d.SandboxRevealedBlockIds != null)
        {
            for (int i = 0; i < d.SandboxRevealedBlockIds.Length; i++)
                GameSessionState.RevealSandboxBlock(d.SandboxRevealedBlockIds[i]);
        }
        if (d.FormatVersion >= 7 && !string.IsNullOrEmpty(d.PoliceStateJson))
            PoliceWorldState.ApplyJson(d.PoliceStateJson);
        else
        {
            PoliceWorldState.ClearAll();
            PoliceWorldState.ResetForNewGame(GameSessionState.CityMapSeed);
        }
        if (d.FormatVersion >= 8 && !string.IsNullOrEmpty(d.BureauStateJson))
            BureauWorldState.ApplyJson(d.BureauStateJson);
        else
        {
            BureauWorldState.ClearAll();
            BureauWorldState.ResetForNewGame(GameSessionState.CityMapSeed);
        }
        if (d.FormatVersion >= 12 && !string.IsNullOrEmpty(d.PersonalityStateJson))
            PersonalityWorldState.ApplyJson(d.PersonalityStateJson);
        else
            PersonalityWorldState.ClearAll();
        GameSessionState.PlayerOrganizationStage = (GameSessionState.OrganizationStage)Mathf.Clamp(
            d.PlayerOrganizationStage, 0, (int)GameSessionState.OrganizationStage.CrimeFamily);

        if (d.FormatVersion >= 4)
        {
            GameSessionState.PlayerThreatScore = Mathf.Max(0, d.PlayerThreatScore);
            GameSessionState.FederalBureauState = (GameSessionState.FederalBureauEngagement)Mathf.Clamp(
                d.FederalBureauState, 0, (int)GameSessionState.FederalBureauEngagement.Active);
            GameSessionState.PlayerIntelNetworkRating = Mathf.Clamp(d.PlayerIntelNetworkRating, 0, 100);
        }
        else
        {
            GameSessionState.PlayerThreatScore = 0;
            GameSessionState.FederalBureauState = GameSessionState.FederalBureauEngagement.Dormant;
            GameSessionState.PlayerIntelNetworkRating = 0;
        }
        GameSessionState.ResetPlanningSubmissionState();
        GameSessionState.IsDaySummaryShowing = false;

        GameModeManager.EnsureExists();
        if (GameModeManager.Instance != null)
        {
            GameModeManager.GameMode mode = d.GameMode == 1
                ? GameModeManager.GameMode.Action
                : GameModeManager.GameMode.Management;
            GameModeManager.Instance.SetMode(mode);
        }

        if (!string.IsNullOrEmpty(d.PlayerProfileJson))
        {
            PlayerCharacterProfile p = JsonUtility.FromJson<PlayerCharacterProfile>(d.PlayerProfileJson);
            if (p != null)
                PlayerRunState.SetCharacter(p);
            else
                PlayerRunState.ClearCharacter();
        }
        else
            PlayerRunState.ClearCharacter();

        string scene = string.IsNullOrEmpty(d.ActiveSceneName) ? "PlanningScene" : d.ActiveSceneName;
        if (scene != "PlanningScene" && scene != "MainScene")
            scene = "PlanningScene";

        SceneManager.LoadScene(scene);
    }
}
