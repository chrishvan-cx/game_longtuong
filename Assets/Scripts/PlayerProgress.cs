using UnityEngine;

public class PlayerProgress : MonoBehaviour
{
    public static PlayerProgress Instance { get; private set; }

    [Header("Player Stats")]
    private int gold = 0;
    private int experience = 0;
    private int level = 1;

    [Header("Level Up Requirements")]
    [Tooltip("XP required for each level. Index 0 = XP needed for level 1->2, Index 1 = XP needed for level 2->3, etc.")]
    public int[] experienceRequiredPerLevel = new int[]
    {
        1000,  // Level 1 -> 2
        1500,  // Level 2 -> 3
        2000,  // Level 3 -> 4
        2500,  // Level 4 -> 5
        3000,  // Level 5 -> 6
        3500,  // Level 6 -> 7
        4000,  // Level 7 -> 8
        5000,  // Level 8 -> 9
        6000,  // Level 9 -> 10
        10000  // Level 10 -> 11 (and beyond)
    };

    // Events
    public System.Action<int> OnGoldChanged;
    public System.Action<int> OnExperienceChanged;
    public System.Action<int> OnLevelUp;
    public System.Action OnPlayerDataChanged; // General event for UI updates

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

#if UNITY_EDITOR
        // Auto-reset progress during development
        ResetProgressInternal();
#else
        LoadProgress();
#endif
    }

    // Getters
    public int GetGold() => gold;
    public int GetExperience() => experience;
    public int GetLevel() => level;

    /// <summary>
    /// Get the XP required to reach the next level
    /// </summary>
    public int GetExperienceRequiredForNextLevel()
    {
        int levelIndex = level - 1; // Array is 0-indexed

        // If we're beyond the array, use the last value
        if (levelIndex >= experienceRequiredPerLevel.Length)
        {
            return experienceRequiredPerLevel[experienceRequiredPerLevel.Length - 1];
        }

        return experienceRequiredPerLevel[levelIndex];
    }

    /// <summary>
    /// Get current progress towards next level as a percentage (0.0 to 1.0)
    /// </summary>
    public float GetLevelProgressPercent()
    {
        int xpRequired = GetExperienceRequiredForNextLevel();
        if (xpRequired <= 0) return 1f;
        return Mathf.Clamp01((float)experience / xpRequired);
    }

    // Gold management
    public void AddGold(int amount)
    {
        gold += amount;
        OnGoldChanged?.Invoke(gold);
        OnPlayerDataChanged?.Invoke();
        SaveProgress();
    }

    public bool SpendGold(int amount)
    {
        if (gold >= amount)
        {
            gold -= amount;
            OnGoldChanged?.Invoke(gold);
            OnPlayerDataChanged?.Invoke();
            SaveProgress();
            return true;
        }
        return false;
    }

    // Experience management
    public void AddExperience(int amount)
    {
        experience += amount;
        OnExperienceChanged?.Invoke(experience);
        OnPlayerDataChanged?.Invoke();

        // Check for level up using the XP array
        CheckForLevelUp();

        SaveProgress();
    }

    private void CheckForLevelUp()
    {
        int xpNeededForNextLevel = GetExperienceRequiredForNextLevel();

        while (experience >= xpNeededForNextLevel)
        {
            experience -= xpNeededForNextLevel;
            LevelUp();
            xpNeededForNextLevel = GetExperienceRequiredForNextLevel();
        }
    }

    private void LevelUp()
    {
        level++;
        OnLevelUp?.Invoke(level);
        OnPlayerDataChanged?.Invoke();
        SaveProgress();
    }

    // Save/Load
    private void SaveProgress()
    {
        PlayerPrefs.SetInt("PlayerGold", gold);
        PlayerPrefs.SetInt("PlayerExperience", experience);
        PlayerPrefs.SetInt("PlayerLevel", level);
        PlayerPrefs.Save();
    }

    private void LoadProgress()
    {
        gold = PlayerPrefs.GetInt("PlayerGold", 0);
        experience = PlayerPrefs.GetInt("PlayerExperience", 0);
        level = PlayerPrefs.GetInt("PlayerLevel", 1);
    }

    // Debug: Reset progress
    public void ResetProgress()
    {
        ResetProgressInternal();
    }

    private void ResetProgressInternal()
    {
        gold = 0;
        experience = 0;
        level = 1;
        SaveProgress();
    }
}
