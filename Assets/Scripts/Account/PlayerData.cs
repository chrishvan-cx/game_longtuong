using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Stores player's team and data. In online version, this will sync with server.
/// </summary>
public class PlayerData : MonoBehaviour
{
    public static PlayerData Instance { get; private set; }

    [Header("Player Team")]
    public List<HeroData> playerTeam = new List<HeroData>();

    [Header("Save Settings")]
    [Tooltip("Bump this to force-reset player data after a new build")]
    public int playerDataSaveVersion = 1;

    [Header("Player Stats (for future use)")]
    public int playerLevel = 1;
    public int playerExp = 100;
    public int playerGold = 500;

    [Header("Level Requirements")]
    public int[] experienceRequiredPerLevel = new int[]
    {
        1000, 1500, 2000, 2500, 3000, 3500, 4000, 5000, 6000, 10000
    };

    // Events
    public System.Action OnPlayerDataChanged;
    public System.Action<int> OnLevelUp;

    void Awake()
    {
        // Singleton pattern with scene persistence
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            // In builds, initialize with default values if no save exists
#if !UNITY_EDITOR
            int savedVersion = PlayerPrefs.GetInt("PlayerDataSaveVersion", 0);
            if (savedVersion != playerDataSaveVersion)
            {
                ClearPlayerDataPlayerPrefs();
                PlayerPrefs.SetInt("PlayerDataSaveVersion", playerDataSaveVersion);
                PlayerPrefs.Save();
            }
            LoadFromPlayerPrefs();
#endif
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void LoadFromPlayerPrefs()
    {
        // Load saved data, or use inspector defaults
        playerLevel = PlayerPrefs.GetInt("PlayerData_Level", playerLevel);
        playerExp = PlayerPrefs.GetInt("PlayerData_Exp", playerExp);
        playerGold = PlayerPrefs.GetInt("PlayerData_Gold", playerGold);
    }

    private void SaveToPlayerPrefs()
    {
        PlayerPrefs.SetInt("PlayerData_Level", playerLevel);
        PlayerPrefs.SetInt("PlayerData_Exp", playerExp);
        PlayerPrefs.SetInt("PlayerData_Gold", playerGold);
        PlayerPrefs.Save();
    }
    private void ClearPlayerDataPlayerPrefs()
    {
        PlayerPrefs.DeleteKey("PlayerData_Level");
        PlayerPrefs.DeleteKey("PlayerData_Exp");
        PlayerPrefs.DeleteKey("PlayerData_Gold");
    }
    /// <summary>
    /// Getters for UI or other systems
    /// </summary>
    public int GetLevel() => playerLevel;
    public int GetExperience() => playerExp;
    public int GetGold() => playerGold;

    /// <summary>
    /// Optionally set all stats at once and notify
    /// </summary>
    public void SetStats(int level, int exp, int gold)
    {
        playerLevel = level;
        playerExp = exp;
        playerGold = gold;
        OnPlayerDataChanged?.Invoke();
    }

    /// <summary>
    /// Set player's team (for now just assign, later this will come from server)
    /// </summary>
    public void SetPlayerTeam(List<HeroData> team)
    {
        playerTeam = team != null ? new List<HeroData>(team) : new List<HeroData>();
        // No stat change here
    }

    /// <summary>
    /// Add rewards after battle (for future server sync)
    /// </summary>
    public void AddRewards(int exp, int gold)
    {
        playerExp += exp;
        playerGold += gold;

        // Check for level up
        CheckForLevelUp();

        SaveToPlayerPrefs();
        OnPlayerDataChanged?.Invoke();

        // TODO: Sync with server when online
    }

    private void CheckForLevelUp()
    {
        int xpRequired = GetExperienceRequiredForNextLevel();

        while (playerExp >= xpRequired)
        {
            playerExp -= xpRequired;
            playerLevel++;
            OnLevelUp?.Invoke(playerLevel);
            xpRequired = GetExperienceRequiredForNextLevel();
        }
    }

    private int GetExperienceRequiredForNextLevel()
    {
        int levelIndex = playerLevel - 1;

        if (experienceRequiredPerLevel == null || experienceRequiredPerLevel.Length == 0)
            return 1000;

        if (levelIndex >= experienceRequiredPerLevel.Length)
            return experienceRequiredPerLevel[experienceRequiredPerLevel.Length - 1];

        return experienceRequiredPerLevel[levelIndex];
    }

    /// <summary>
    /// Load player data from server (now using mock data)
    /// </summary>
    public void LoadFromServer()
    {
        // Load mock data
        var mockData = MockServerData.LoadMockData();
        
        // Convert mock team to HeroData list
        playerTeam = MockServerData.ConvertToHeroDataList(mockData.playerTeam);
        
        // Load player stats
        if (mockData.playerStats != null)
        {
            playerLevel = mockData.playerStats.level;
            playerExp = mockData.playerStats.exp;
            playerGold = mockData.playerStats.gold;
        }
        
        OnPlayerDataChanged?.Invoke();
    }

    /// <summary>
    /// Save player data to server (placeholder for now)
    /// </summary>
    public void SaveToServer()
    {
        // TODO: Send to server API
    }
}
