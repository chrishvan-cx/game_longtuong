using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class QuestManager : MonoBehaviour
{
    public static QuestManager Instance { get; private set; }

    [Header("Quest Configuration")]
    public QuestData[] allQuests; // All quests in order

    [Header("Save Settings")]
    [Tooltip("Bump this to force-reset quest progress after a new build")]
    public int questSaveVersion = 1;

    [Header("Current State")]
    private int currentQuestIndex = 0;
    private HashSet<int> completedQuestIds = new HashSet<int>();
    private int currentQuestProgress = 0; // Track battles completed for current quest
    private bool currentQuestAccepted = false; // Track if current quest is accepted

    // Two-level progression system
    private int mapLevel = 0; // Controls NPC/area unlocking (increases after any battle)
    private int questLevel = 0; // For quest rewards and features (increases on quest completion)

    // Events
    public System.Action<QuestData> OnQuestAccepted;
    public System.Action<QuestData> OnQuestCompleted;
    public System.Action<QuestData> OnQuestProgressUpdated;

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
                        ResetAllProgressInternal();
#else
        // Build: optionally reset progress based on version key
        int savedVersion = PlayerPrefs.GetInt("QuestSaveVersion", 0);
        if (savedVersion != questSaveVersion)
        {
            ClearQuestPlayerPrefs();
            PlayerPrefs.SetInt("QuestSaveVersion", questSaveVersion);
            PlayerPrefs.Save();
        }
        LoadProgress();
#endif
    }

    void Start()
    {
        // Don't auto-accept any quests - let player accept manually
        // This ensures player must interact with quest giver
    }

    public QuestData GetCurrentQuest()
    {
        if (currentQuestIndex >= allQuests.Length)
            return null;
        return allQuests[currentQuestIndex];
    }

    /// <summary>
    /// Check if there are any quests available for the player's current level
    /// </summary>
    public bool HasAvailableQuestsForLevel()
    {
        int playerLevel = GetPlayerLevel();
        if (playerLevel == 0) return false;

        // Check if there's any quest that:
        // 1. Is not completed
        // 2. Player level meets requirement
        // 3. Is not currently accepted
        for (int i = currentQuestIndex; i < allQuests.Length; i++)
        {
            QuestData quest = allQuests[i];
            if (!completedQuestIds.Contains(quest.questId) &&
                playerLevel >= quest.requiredPlayerLevel)
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Check if player can accept the current quest based on level requirement
    /// </summary>
    public bool CanAcceptCurrentQuest()
    {
        QuestData quest = GetCurrentQuest();
        if (quest == null) return false;
        if (currentQuestAccepted) return false; // Already accepted

        int playerLevel = GetPlayerLevel();
        return playerLevel >= quest.requiredPlayerLevel;
    }

    public bool IsQuestActive()
    {
        return GetCurrentQuest() != null;
    }

    public bool IsQuestCompleted(int questId)
    {
        return completedQuestIds.Contains(questId);
    }

    public int GetCurrentQuestProgress()
    {
        return currentQuestProgress;
    }

    public void AcceptQuest()
    {
        QuestData quest = GetCurrentQuest();

        currentQuestAccepted = true;
        currentQuestProgress = 0;
        OnQuestAccepted?.Invoke(quest);
        SaveProgress();
    }

    // Called after winning ANY battle (even without quest)
    public void OnBattleWon()
    {
        // Increase map level - this unlocks new NPCs/areas
        mapLevel++;
        SaveProgress();
    }

    // Called after winning battle IF player has active quest
    public void UpdateQuestProgress(string npcId = "")
    {
        QuestData quest = GetCurrentQuest();

        currentQuestProgress++;
        OnQuestProgressUpdated?.Invoke(quest);

        SaveProgress();
    }

    public bool CanCompleteQuest()
    {
        QuestData quest = GetCurrentQuest();
        if (quest == null) return false;
        if (!currentQuestAccepted) return false; // Must accept quest first
        return currentQuestProgress >= quest.requiredBattles;
    }

    public void CompleteQuest()
    {
        QuestData quest = GetCurrentQuest();
        if (quest == null || !CanCompleteQuest())
        {
            return;
        }

        // Mark as completed
        completedQuestIds.Add(quest.questId);

        // Grant rewards to PlayerData
        if (PlayerData.Instance != null)
        {
            PlayerData.Instance.AddRewards(quest.experienceReward, quest.goldReward);
        }
        // Fallback to PlayerProgress if PlayerData doesn't exist
        else if (PlayerProgress.Instance != null)
        {
            PlayerProgress.Instance.AddGold(quest.goldReward);
            PlayerProgress.Instance.AddExperience(quest.experienceReward);
        }

        // Increase quest level (for future features and map area unlocking)
        questLevel++;
        OnQuestCompleted?.Invoke(quest);

        // Move to next quest
        currentQuestIndex++;
        currentQuestProgress = 0;
        currentQuestAccepted = false; // Reset acceptance for next quest

        SaveProgress();

        // Don't auto-accept next quest - let player accept manually
    }


    // Getters for progression levels
    public int GetMapLevel()
    {
        return mapLevel;
    }

    public int GetQuestLevel()
    {
        return questLevel;
    }

    // Save/Load
    private void SaveProgress()
    {
        PlayerPrefs.SetInt("CurrentQuestIndex", currentQuestIndex);
        PlayerPrefs.SetInt("CurrentQuestProgress", currentQuestProgress);
        PlayerPrefs.SetInt("CurrentQuestAccepted", currentQuestAccepted ? 1 : 0);
        PlayerPrefs.SetInt("MapLevel", mapLevel);
        PlayerPrefs.SetInt("QuestLevel", questLevel);
        PlayerPrefs.SetString("CompletedQuests", string.Join(",", completedQuestIds));
        PlayerPrefs.Save();
    }

    private void ClearQuestPlayerPrefs()
    {
        PlayerPrefs.DeleteKey("CurrentQuestIndex");
        PlayerPrefs.DeleteKey("CurrentQuestProgress");
        PlayerPrefs.DeleteKey("CurrentQuestAccepted");
        PlayerPrefs.DeleteKey("MapLevel");
        PlayerPrefs.DeleteKey("QuestLevel");
        PlayerPrefs.DeleteKey("CompletedQuests");
    }

    private void LoadProgress()
    {
        currentQuestIndex = PlayerPrefs.GetInt("CurrentQuestIndex", 0);
        currentQuestProgress = PlayerPrefs.GetInt("CurrentQuestProgress", 0);
        currentQuestAccepted = PlayerPrefs.GetInt("CurrentQuestAccepted", 0) == 1;
        mapLevel = PlayerPrefs.GetInt("MapLevel", 0);
        questLevel = PlayerPrefs.GetInt("QuestLevel", 0);

        string completedStr = PlayerPrefs.GetString("CompletedQuests", "");
        if (!string.IsNullOrEmpty(completedStr))
        {
            completedQuestIds = new HashSet<int>(completedStr.Split(',').Select(int.Parse));
        }
    }

    // Helper to get player level from PlayerData or PlayerProgress
    private int GetPlayerLevel()
    {
        if (PlayerData.Instance != null)
            return PlayerData.Instance.GetLevel();
        if (PlayerProgress.Instance != null)
            return PlayerProgress.Instance.GetLevel();
        return 1; // Default
    }

    // Debug: Reset all progress
    public void ResetAllProgress()
    {
        ResetAllProgressInternal();
    }

    private void ResetAllProgressInternal()
    {
        currentQuestIndex = 0;
        currentQuestProgress = 0;
        currentQuestAccepted = false;
        mapLevel = 0;
        questLevel = 0;
        completedQuestIds.Clear();
        SaveProgress();
    }
}
