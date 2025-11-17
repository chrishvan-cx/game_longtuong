using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class MapNPC : MonoBehaviour, IPointerClickHandler
{
    [Header("NPC Data")]
    public string npcName = "Enemy";
    public int enemyLevel = 1;
    public int requiredMapLevel = 0; // Map level required to unlock this NPC
    public Sprite npcSprite;

    [Header("Battle Rewards")]
    public int expReward = 100;
    public int goldReward = 50;
    public List<ItemReward> itemRewards = new List<ItemReward>();

    [Header("Enemy Team")]
    public string enemyTeamId = "monster_map_1";  // ← Set this in Inspector
    public List<HeroData> enemyTeam = new List<HeroData>();

    [Header("UI References")]
    public TMP_Text textNameAndLevel;
    public Image npcImage;  // Must have Raycast Target enabled

    void Start()
    {
        // Load enemy team from JSON
        LoadEnemyTeamFromJson();

        // Check if this NPC should be visible
        CheckUnlockState();

        // Setup visual - combine name and level
        if (textNameAndLevel != null)
            textNameAndLevel.text = $"{npcName} Lv.{enemyLevel}";

        if (npcImage != null && npcSprite != null)
            npcImage.sprite = npcSprite;

        // Ensure image can receive clicks
        if (npcImage != null)
        {
            npcImage.raycastTarget = true;
        }

        // Subscribe to events to update visibility
        if (QuestManager.Instance != null)
        {
            QuestManager.Instance.OnQuestCompleted += OnQuestCompleted;
        }
    }

    void OnDestroy()
    {
        if (QuestManager.Instance != null)
        {
            QuestManager.Instance.OnQuestCompleted -= OnQuestCompleted;
        }
    }

    void OnEnable()
    {
        // Re-check unlock state when NPC becomes active (e.g., when opening map panel)
        CheckUnlockState();
    }

    private void OnQuestCompleted(QuestData quest)
    {
        // Re-check unlock state when a quest is completed
        CheckUnlockState();
    }

    private void CheckUnlockState()
    {
        if (QuestManager.Instance == null)
        {
            // If no QuestManager, hide by default
            gameObject.SetActive(false);
            return;
        }

        // Check if this NPC is unlocked based on player's map level
        int playerMapLevel = QuestManager.Instance.GetMapLevel();
        bool isUnlocked = playerMapLevel >= requiredMapLevel;
        gameObject.SetActive(isUnlocked);
    }

    /// <summary>
    /// Load enemy team from JSON based on enemyTeamId
    /// </summary>
    private void LoadEnemyTeamFromJson()
    {
        if (string.IsNullOrEmpty(enemyTeamId))
        {
            Debug.LogWarning($"[{npcName}] No enemyTeamId set");
            return;
        }

        // ✅ Load enemy team using the teamId
        enemyTeam = MockServerData.LoadEnemyTeam(enemyTeamId);

        if (enemyTeam.Count == 0)
        {
            Debug.LogWarning($"[{npcName}] Failed to load enemy team '{enemyTeamId}'");
        }
        else
        {
            Debug.Log($"[{npcName}] Loaded {enemyTeam.Count} enemies from '{enemyTeamId}'");
        }
    }


    // IPointerClickHandler implementation
    public void OnPointerClick(PointerEventData eventData)
    {
        if (GameManager.Instance == null)
        {
            Debug.LogError("GameManager not found! Make sure you start from Home Scene.");
            return;
        }

        // Notify that battle is starting (map level will increase after win)
        if (QuestManager.Instance != null)
        {
            // Update quest progress if player has active quest
            QuestManager.Instance.UpdateQuestProgress();
        }

        // Set rewards and enemy team in GameManager before loading battle
        GameManager.Instance.SetBattleRewards(expReward, goldReward, itemRewards);
        GameManager.Instance.SetEnemyTeam(enemyTeam);
        GameManager.Instance.LoadBattleScene(npcName, enemyLevel);
    }
}
