using UnityEngine;
using UnityEngine.UI;

public class MapSceneManager : MonoBehaviour
{
    [Header("UI References")]
    public Button backButton; // Return to home
    public Transform npcContainer; // Parent object holding all NPCs

    void Start()
    {
        // Setup back button
        if (backButton != null)
        {
            backButton.onClick.AddListener(OnBackButtonClicked);
        }
        
        // If returning from battle, reopen the last area
        ReopenLastArea();
    }
    
    private void ReopenLastArea()
    {
        if (GameManager.Instance == null || string.IsNullOrEmpty(GameManager.Instance.lastOpenedAreaName))
            return;
        
        // Find the area that was opened
        MapArea[] allAreas = FindObjectsOfType<MapArea>();
        foreach (var area in allAreas)
        {
            if (area.areaName == GameManager.Instance.lastOpenedAreaName)
            {
                // Trigger the panel to open
                area.OnPointerClick(null);
                break;
            }
        }
    }

    private void OnBackButtonClicked()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.LoadHomeScene();
        }
        else
        {
            Debug.LogError("GameManager not found!");
        }
    }
}
