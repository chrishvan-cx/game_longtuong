using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class MapArea : MonoBehaviour, IPointerClickHandler
{
    [Header("Area Info")]
    public string areaName = "Forest";
    public int requiredQuestLevel = 0; // Quest level required to unlock this area
    public Sprite areaIcon;

    [Header("UI References")]
    public Image areaImage;
    public TMP_Text areaNameText;
    public TMP_Text levelRangeText;

    [Header("NPCs in this Area")]
    public MapNPC[] npcsInArea;

    [Header("Panel Reference")]
    public NPCSelectionPanel npcSelectionPanel;

    void Start()
    {
        // Check if this area should be visible
        CheckUnlockState();

        // Setup visual
        if (areaNameText != null)
            areaNameText.text = areaName;

        if (levelRangeText != null)
            levelRangeText.text = "";

        if (areaImage != null)
        {
            if (areaIcon != null)
                areaImage.sprite = areaIcon;
            areaImage.raycastTarget = true;
        }

        // Subscribe to quest completion to update visibility
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

        // Simple check: area unlocked if player's quest level >= required level
        int playerQuestLevel = QuestManager.Instance.GetQuestLevel();
        bool isUnlocked = playerQuestLevel >= requiredQuestLevel;
        gameObject.SetActive(isUnlocked);

    }

    public void OnPointerClick(PointerEventData eventData)
    {
        OpenNPCSelectionPanel();
    }

    private void OpenNPCSelectionPanel()
    {
        if (npcSelectionPanel != null)
        {
            // Store which area was opened in GameManager
            if (GameManager.Instance != null)
            {
                GameManager.Instance.lastOpenedAreaName = areaName;
            }

            npcSelectionPanel.ShowNPCs(areaName, npcsInArea);
        }
        else
        {
            Debug.LogError("NPCSelectionPanel not assigned to MapArea!");
        }
    }
}
