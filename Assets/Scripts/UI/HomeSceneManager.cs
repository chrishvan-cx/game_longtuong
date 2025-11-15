using UnityEngine;
using UnityEngine.UI;

public class HomeSceneManager : MonoBehaviour
{
    [Header("UI References")]
    public Button mapPortalButton;
    public GameObject mapPortalObject; // Visual portal (can be an image, sprite, etc.)
    public Button formationButton;
    public FormationWindow formationWindow;

    void Start()
    {
        // Setup button listeners
        if (mapPortalButton != null)
        {
            mapPortalButton.onClick.AddListener(OnMapPortalClicked);
        }

        if (formationButton != null)
        {
            formationButton.onClick.AddListener(OnFormationButtonClicked);
        }

        // TEMPORARY: Auto-open formation window for testing
        // Remove this after you create the button
        if (formationWindow != null)
        {
            Debug.Log("[TEST] Auto-opening FormationWindow...");
            Invoke(nameof(TestOpenFormation), 0.5f); // Delay to let PlayerData load
        }
    }

    private void TestOpenFormation()
    {
        OnFormationButtonClicked();
    }

    private void OnMapPortalClicked()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.LoadMapScene();
        }
        else
        {
            Debug.LogError("GameManager not found! Make sure GameManager exists in the scene.");
        }
    }

    private void OnFormationButtonClicked()
    {
        Debug.Log("Formation button clicked!");
        
        if (formationWindow != null)
        {
            Debug.Log("Calling FormationWindow.Show()...");
            formationWindow.Show();
        }
        else
        {
            Debug.LogWarning("FormationWindow reference is not assigned!");
        }
    }

    // Can be called from other UI buttons
    public void OnQuitButtonClicked()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.QuitGame();
        }
    }
}
