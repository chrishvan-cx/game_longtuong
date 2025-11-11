using UnityEngine;
using TMPro;

/// <summary>
/// Displays current player data: Level, Gold, Experience
/// Single source of truth: PlayerData
/// </summary>
public class PlayerPanelUI : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI levelText;
    public TextMeshProUGUI goldText;
    public ExpBar experienceBar;

    [Header("XP Requirements")]
    public int[] experienceRequiredPerLevel = new int[]
    {
        1000, 1500, 2000, 2500, 3000, 3500, 4000, 5000, 6000, 10000
    };

    void Start()
    {
        // Wait a frame to ensure ExpBar has initialized
        StartCoroutine(InitializeUI());
    }

    private System.Collections.IEnumerator InitializeUI()
    {
        yield return null; // Wait one frame
        UpdateUI(true); // First load - instant fill
        SubscribeToEvents();
    }

    void OnDestroy()
    {
        UnsubscribeFromEvents();
    }

    private void SubscribeToEvents()
    {
        if (PlayerData.Instance != null)
        {
            PlayerData.Instance.OnPlayerDataChanged -= OnDataChanged;
            PlayerData.Instance.OnPlayerDataChanged += OnDataChanged;
        }
    }

    private void OnDataChanged()
    {
        UpdateUI(false); // Animate on updates
    }

    private void UnsubscribeFromEvents()
    {
        if (PlayerData.Instance != null)
        {
            PlayerData.Instance.OnPlayerDataChanged -= OnDataChanged;
        }
    }

    private void UpdateUI(bool instant = false)
    {
        if (PlayerData.Instance == null)
            return;
        
        if (levelText != null)
            levelText.text = $"Level {PlayerData.Instance.playerLevel}";

        if (goldText != null)
            goldText.text = $"{PlayerData.Instance.playerGold:N0}";

        if (experienceBar != null)
        {
            int currentXP = PlayerData.Instance.playerExp;
            int requiredXP = GetRequiredXP(PlayerData.Instance.playerLevel);
            float progress = requiredXP > 0 ? (float)currentXP / requiredXP : 0f;
            
            if (instant)
                experienceBar.SetExperienceInstant(progress, currentXP, requiredXP);
            else
                experienceBar.SetExperience(progress, currentXP, requiredXP);
        }
    }

    private int GetRequiredXP(int level)
    {
        int levelIndex = level - 1;
        if (levelIndex >= experienceRequiredPerLevel.Length)
            return experienceRequiredPerLevel[experienceRequiredPerLevel.Length - 1];
        return experienceRequiredPerLevel[levelIndex];
    }
}
