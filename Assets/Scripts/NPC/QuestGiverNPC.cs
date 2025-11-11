using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class QuestGiverNPC : MonoBehaviour
{
    [Header("UI References")]
    public Button npcButton; // Button to click the NPC
    public GameObject questDialogPanel; // Panel showing quest info
    public TMPro.TMP_Text questTitleText;
    public TMPro.TMP_Text questDescriptionText;
    public TMPro.TMP_Text questProgressText;
    public TMPro.TMP_Text questRewardText; // Shows gold and XP rewards
    public TMPro.TMP_Text questStatusText; // Shows pending/ready status
    public Button acceptButton;
    public Button completeButton;
    public Button closeButton;

    [Header("Quest Indicator")]
    public GameObject questAvailableIndicator; // "!" icon - Quest available/in progress
    public GameObject questCompleteIndicator; // "?" icon - Quest ready to turn in (pending)
    public GameObject noQuestIndicator; // No icon or different indicator for no quest

    [Header("Interaction")]
    [Tooltip("If true, player must be near (trigger) to interact")]
    public bool requireProximity = true;
    [Tooltip("Auto-open dialog when player enters trigger")]
    public bool autoOpenDialogOnEnter = true;
    private bool playerNearby = false;
    private bool pendingDialogOpen = false;

    void Start()
    {
        EnsureEventSystem();

        // Fallbacks in case references are missing in build
        if (npcButton == null)
        {
            npcButton = GetComponent<Button>();
            if (npcButton == null)
            {
                npcButton = GetComponentInChildren<Button>(true);
            }
        }

        if (npcButton != null)
        {
            npcButton.onClick.RemoveListener(OnNPCClicked);
            npcButton.onClick.AddListener(OnNPCClicked);
            // Hide button initially if proximity required
            if (requireProximity)
                npcButton.gameObject.SetActive(false);
        }

        if (acceptButton != null)
            acceptButton.onClick.AddListener(OnAcceptQuest);

        if (completeButton != null)
            completeButton.onClick.AddListener(OnCompleteQuest);

        if (closeButton != null)
            closeButton.onClick.AddListener(OnCloseDialog);

        if (questDialogPanel != null)
            questDialogPanel.SetActive(false);

        UpdateQuestIndicators();

        // Subscribe to quest events
        if (QuestManager.Instance != null)
        {
            QuestManager.Instance.OnQuestAccepted += OnQuestStateChanged;
            QuestManager.Instance.OnQuestCompleted += OnQuestStateChanged;
            QuestManager.Instance.OnQuestProgressUpdated += OnQuestStateChanged;
        }
    }

    void OnDestroy()
    {
        if (QuestManager.Instance != null)
        {
            QuestManager.Instance.OnQuestAccepted -= OnQuestStateChanged;
            QuestManager.Instance.OnQuestCompleted -= OnQuestStateChanged;
            QuestManager.Instance.OnQuestProgressUpdated -= OnQuestStateChanged;
        }
    }

    private void OnQuestStateChanged(QuestData quest)
    {
        UpdateQuestIndicators();
    }

    private void UpdateQuestIndicators()
    {
        if (QuestManager.Instance == null)
        {
            // No quest manager - hide all indicators
            HideAllIndicators();
            return;
        }

        // Priority 1: Check if quest is ready to turn in (pending completion)
        bool canComplete = QuestManager.Instance.CanCompleteQuest();
        if (canComplete)
        {
            // Quest complete, ready to turn in - show "?"
            ShowIndicator(IndicatorType.Pending);
            return;
        }

        // Priority 2: Check if there are quests available for player's level
        QuestData currentQuest = QuestManager.Instance.GetCurrentQuest();
        if (currentQuest != null && QuestManager.Instance.CanAcceptCurrentQuest())
        {
            // Quest exists and player can accept it - show "!"
            ShowIndicator(IndicatorType.Active);
            return;
        }

        // Priority 3: No quests available or player level too low
        ShowIndicator(IndicatorType.None);
    }

    private enum IndicatorType { None, Active, Pending }

    private void ShowIndicator(IndicatorType type)
    {
        if (questAvailableIndicator != null)
            questAvailableIndicator.SetActive(type == IndicatorType.Active);

        if (questCompleteIndicator != null)
            questCompleteIndicator.SetActive(type == IndicatorType.Pending);

        if (noQuestIndicator != null)
            noQuestIndicator.SetActive(type == IndicatorType.None);
    }

    private void HideAllIndicators()
    {
        if (questAvailableIndicator != null)
            questAvailableIndicator.SetActive(false);

        if (questCompleteIndicator != null)
            questCompleteIndicator.SetActive(false);

        if (noQuestIndicator != null)
            noQuestIndicator.SetActive(false);
    }

    // Alternative: Detect clicks directly on the NPC GameObject (works with Collider2D)
    // Disabled - PlayerMovement now handles all click detection centrally
    /*
    void OnMouseDown()
    {
        OnNPCClicked();
    }
    */

    public void OnNPCClicked()
    {
        // If player is already nearby, open dialog immediately
        if (playerNearby)
        {
            ShowQuestDialog();
            pendingDialogOpen = false;
            return;
        }

        // Player is far away: move to NPC, open dialog on arrival via trigger
        MovePlayerToNPC();
        pendingDialogOpen = true;
    }

    private void MovePlayerToNPC()
    {
        // Find player and tell them to move here
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            return;
        }

        PlayerMovement movement = player.GetComponent<PlayerMovement>();
        if (movement == null)
        {
            return;
        }

        // Move to NPC position (trigger will detect when player arrives)
        Vector3 targetPos = transform.position;
        movement.MoveToPosition(targetPos);
    }

    // Trigger events - called when player enters/exits collider
    void OnTriggerEnter2D(Collider2D other)
    {
        // Check if it's the player
        if (other.CompareTag("Player"))
        {
            playerNearby = true;

            // Show NPC button (if using proximity-based button)
            if (npcButton != null && requireProximity)
                npcButton.gameObject.SetActive(true);

            // ONLY open dialog if player clicked the NPC (pendingDialogOpen = true)
            // Don't open if player just walked into the trigger
            if (pendingDialogOpen)
            {
                ShowQuestDialog();
                pendingDialogOpen = false;
            }
            else
            {
            }
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        // Check if it's the player
        if (other.CompareTag("Player"))
        {
            playerNearby = false;

            // Clear pending dialog flag when leaving
            pendingDialogOpen = false;

            // Hide NPC button (if using proximity-based button)
            if (npcButton != null && requireProximity)
                npcButton.gameObject.SetActive(false);

            // Don't hide quest indicators - they should stay visible based on quest status
            // HideAllIndicators(); // REMOVED - indicators show quest status, not proximity

            // Close dialog if open
            if (questDialogPanel != null && questDialogPanel.activeSelf)
                questDialogPanel.SetActive(false);
        }
    }

    private void EnsureEventSystem()
    {
        if (EventSystem.current != null) return;

        // Create EventSystem at runtime if missing
        GameObject es = new GameObject("EventSystem");
        es.AddComponent<EventSystem>();
        es.AddComponent<StandaloneInputModule>();
    }

    private void ShowQuestDialog()
    {

        QuestData currentQuest = QuestManager.Instance.GetCurrentQuest();

        // Check if player level is high enough
        if (PlayerProgress.Instance != null)
        {
            int playerLevel = PlayerProgress.Instance.GetLevel();
        }

        // Show dialog
        if (questDialogPanel != null)
        {
            questDialogPanel.SetActive(true);
        }
        else
        {
            return;
        }

        // Update UI
        if (questTitleText != null)
            questTitleText.text = currentQuest.questName;

        if (questDescriptionText != null)
        {
            string description = currentQuest.questDescription;
            // Add level requirement to description
            description += $"\n\nRequired Level: {currentQuest.requiredPlayerLevel}";
            questDescriptionText.text = description;
        }

        // Update progress text
        int progress = QuestManager.Instance.GetCurrentQuestProgress();
        if (questProgressText != null)
            questProgressText.text = $"Progress: {progress}/{currentQuest.requiredBattles}";

        // Update reward text
        if (questRewardText != null)
        {
            questRewardText.text = $"Rewards: {currentQuest.goldReward} Gold, {currentQuest.experienceReward} XP";
        }

        // Update status text
        bool canComplete = QuestManager.Instance.CanCompleteQuest();
        if (questStatusText != null)
        {
            if (canComplete)
            {
                questStatusText.text = "Status: Ready to Turn In!";
                questStatusText.color = Color.green; // Optional: color based on status
            }
            else
            {
                questStatusText.text = "Status: In Progress";
                questStatusText.color = Color.yellow; // Optional: color based on status
            }
        }

        // Show appropriate buttons

        if (acceptButton != null)
            acceptButton.gameObject.SetActive(!canComplete);

        if (completeButton != null)
            completeButton.gameObject.SetActive(canComplete);
    }

    private void OnAcceptQuest()
    {
        if (QuestManager.Instance == null) return;

        QuestManager.Instance.AcceptQuest();
        OnCloseDialog();

        // Update indicators after accepting quest
        UpdateQuestIndicators();
    }

    private void OnCompleteQuest()
    {
        if (QuestManager.Instance == null) return;

        QuestManager.Instance.CompleteQuest();
        OnCloseDialog();

        // Update indicators after completing quest (should show next quest or none)
        UpdateQuestIndicators();
    }

    private void OnCloseDialog()
    {
        if (questDialogPanel != null)
            questDialogPanel.SetActive(false);
    }
}
