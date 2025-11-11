using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BattleResultPanel : MonoBehaviour
{
    [Header("UI References")]
    public GameObject panelRoot;
    public TMP_Text resultText;          // "Victory!" or "Defeat!"
    public TMP_Text expGainedText;       // "EXP Gained: 150"
    public TMP_Text goldGainedText;      // "Gold: 50"
    public Button continueButton;

    [Header("Item Display")]
    public GameObject itemIconPrefab;     // Prefab with Image component for item icon

    public GameObject itemsListContainer;

    [Header("Animation")]
    public float fadeInDuration = 0.5f;
    public AnimationCurve fadeInCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    private CanvasGroup canvasGroup;

    void Awake()
    {
        // Ensure the script GameObject stays active (only hide the panel root)
        if (panelRoot != null)
        {
            // Get or add CanvasGroup for fade animation
            canvasGroup = panelRoot.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = panelRoot.AddComponent<CanvasGroup>();
            }

            // Hide panel initially
            Hide();
        }

        // Setup button
        if (continueButton != null)
        {
            continueButton.onClick.AddListener(OnContinueClicked);
        }
    }

    public void ShowVictory(int expGained, int goldGained, List<ItemReward> itemRewards = null)
    {
        StartCoroutine(ShowResult(true, expGained, goldGained, itemRewards));
    }

    public void ShowDefeat()
    {
        StartCoroutine(ShowResult(false, 0, 0, null));
    }

    private IEnumerator ShowResult(bool isVictory, int exp, int gold, List<ItemReward> items)
    {
        // Wait a bit before showing panel
        yield return new WaitForSeconds(1f);

        // Award rewards to player if victory
        if (isVictory)
        {
            // Add to PlayerData (single source of truth)
            if (PlayerData.Instance != null)
            {
                PlayerData.Instance.AddRewards(exp, gold);
            }
            // Also add to PlayerProgress if it exists (for backwards compatibility)
            else if (PlayerProgress.Instance != null)
            {
                PlayerProgress.Instance.AddExperience(exp);
                PlayerProgress.Instance.AddGold(gold);
            }
        }

        // Set text content
        if (resultText != null)
        {
            resultText.text = isVictory ? "Victory!" : "Defeat!";
            resultText.color = isVictory ? Color.green : Color.red;
        }

        if (expGainedText != null)
        {
            expGainedText.text = isVictory ? $"EXP: {exp}" : "";
        }

        if (goldGainedText != null)
        {
            goldGainedText.text = isVictory ? $"Gold: {gold}" : "";
        }

        // Display item rewards
        ClearItemsContainer();
        if (items != null && items.Count > 0 && isVictory)
        {
            DisplayItems(items);
        }

        // Show panel with fade-in
        panelRoot.SetActive(true);
        yield return StartCoroutine(FadeIn());
    }

    private IEnumerator FadeIn()
    {
        float elapsed = 0f;
        canvasGroup.alpha = 0f;

        while (elapsed < fadeInDuration)
        {
            elapsed += Time.deltaTime;
            float t = fadeInCurve.Evaluate(elapsed / fadeInDuration);
            canvasGroup.alpha = t;
            yield return null;
        }

        canvasGroup.alpha = 1f;
    }

    public void Hide()
    {
        if (panelRoot != null)
        {
            panelRoot.SetActive(false);
        }
        ClearItemsContainer();
    }

    private void ClearItemsContainer()
    {
        if (itemsListContainer == null) return;

        // Destroy all children
        foreach (Transform child in itemsListContainer.transform)
        {
            Destroy(child.gameObject);
        }
    }

    private void DisplayItems(List<ItemReward> items)
    {
        if (itemsListContainer == null || itemIconPrefab == null)
        {
            return;
        }

        foreach (var item in items)
        {
            // Instantiate item icon
            GameObject itemObj = Instantiate(itemIconPrefab, itemsListContainer.transform);

            // Set sprite
            Image itemImage = itemObj.GetComponentInChildren<Image>();
            if (itemImage != null && item.itemSprite != null)
            {
                itemImage.sprite = item.itemSprite;
            }
            // Optional: Show quantity if > 1
            TMP_Text quantityText = itemObj.GetComponentInChildren<TMP_Text>();
            if (quantityText != null && item.quantity > 1)
            {
                quantityText.text = $"x{item.quantity}";
            }
        }
    }

    private void OnContinueClicked()
    {
        Hide();

        // Return to home scene
        if (GameManager.Instance != null)
        {
            GameManager.Instance.LoadHomeScene();
        }
    }
}
