using UnityEngine;

[CreateAssetMenu(fileName = "Quest", menuName = "Game/Quest Data")]
public class QuestData : ScriptableObject
{
    [Header("Quest Info")]
    public int questId;
    public string questName;
    [TextArea(3, 6)]
    public string questDescription;
    
    [Header("Requirements")]
    public int requiredPlayerLevel = 1; // Player level required to accept this quest
    public int requiredBattles = 1; // Number of battles to complete
    public string targetNPCId; // Which NPC to battle (optional - can be any if empty)
    
    [Header("Rewards")]
    public int goldReward = 100;
    public int experienceReward = 50;
}
