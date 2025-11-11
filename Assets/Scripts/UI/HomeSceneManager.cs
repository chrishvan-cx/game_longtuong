using UnityEngine;
using UnityEngine.UI;

public class HomeSceneManager : MonoBehaviour
{
    [Header("UI References")]
    public Button mapPortalButton;
    public GameObject mapPortalObject; // Visual portal (can be an image, sprite, etc.)

    void Start()
    {
        // Setup button listener
        if (mapPortalButton != null)
        {
            mapPortalButton.onClick.AddListener(OnMapPortalClicked);
        }
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

    // Can be called from other UI buttons
    public void OnQuitButtonClicked()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.QuitGame();
        }
    }
}
