using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Displays the player visual on screen: Name and Body (sprite/image)
/// Stats like level, gold, exp are shown in PlayerPanelUI
/// </summary>
public class PlayerUI : MonoBehaviour
{
    [Header("Player Visual")]
    public TextMeshProUGUI playerNameText;
    public Image playerBodyImage; // Player sprite/portrait
    
    [Header("Player Info")]
    [Tooltip("The player's display name")]
    public string playerName = "Player";
    
    [Tooltip("The player's body sprite/portrait")]
    public Sprite playerBodySprite;

    private void Start()
    {
        UpdatePlayerVisual();
    }

    /// <summary>
    /// Update the player name and body image
    /// </summary>
    public void UpdatePlayerVisual()
    {
        if (playerNameText != null)
        {
            playerNameText.text = playerName;
        }

        if (playerBodyImage != null && playerBodySprite != null)
        {
            playerBodyImage.sprite = playerBodySprite;
        }
    }

    /// <summary>
    /// Change the player's display name
    /// </summary>
    public void SetPlayerName(string newName)
    {
        playerName = newName;
        if (playerNameText != null)
        {
            playerNameText.text = playerName;
        }
    }

    /// <summary>
    /// Change the player's body sprite
    /// </summary>
    public void SetPlayerBody(Sprite newSprite)
    {
        playerBodySprite = newSprite;
        if (playerBodyImage != null)
        {
            playerBodyImage.sprite = playerBodySprite;
        }
    }

    // Optional: Force refresh for debugging
    [ContextMenu("Force Update Visual")]
    public void ForceUpdateVisual()
    {
        UpdatePlayerVisual();
    }
}
