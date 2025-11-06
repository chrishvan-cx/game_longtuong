using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.ComponentModel;

/// <summary>
/// Experience bar component - displays and animates XP progress
/// Similar to HealthBar but with smooth animation
/// </summary>
public class ExpBar : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Optional text to display XP progress")]
    public TextMeshProUGUI expText;

    [Tooltip("Optional percentage experience text")]
    public TextMeshProUGUI expPercentText;

    [Header("Display Options")]
    [Tooltip("Show XP as numbers (e.g. 1000/5000) or percentage (e.g. 20%)")]
    public bool showNumbers = true;

    [Header("Animation")]
    [Tooltip("Speed of XP bar fill animation")]
    public float fillSpeed = 2f;

    private Image fillImage;
    private Transform fill;
    private float currentFillAmount = 0f;
    private float targetFillAmount = 0f;
    private Coroutine fillCoroutine;

    // Store current/required XP for display
    private int currentXP = 0;
    private int requiredXP = 1;

    void Start()
    {
        // Find Fill child - could be UI Image or SpriteRenderer
        Transform fillTransform = transform.Find("Fill");
        
        if (fillTransform != null)
        {
            fillImage = fillTransform.GetComponent<Image>();
            
            // If no Image component, use Transform directly (for SpriteRenderer)
            if (fillImage == null)
            {
                fill = fillTransform;
            }
            
            // Initialize to 0
            UpdateFill(0f);
        }
    }
    /// <summary>
    /// Set experience progress (0.0 to 1.0) with smooth animation
    /// </summary>
    public void SetExperience(float progress)
    {
        targetFillAmount = Mathf.Clamp01(progress);

        // Stop any existing animation
        if (fillCoroutine != null)
        {
            StopCoroutine(fillCoroutine);
        }

        // Start new animation
        fillCoroutine = StartCoroutine(AnimateExp());
    }

    /// <summary>
    /// Set experience with XP numbers for text display
    /// </summary>
    public void SetExperience(float progress, int current, int required)
    {
        currentXP = current;
        requiredXP = required;
        targetFillAmount = Mathf.Clamp01(progress);

        // Stop any existing animation
        if (fillCoroutine != null)
        {
            StopCoroutine(fillCoroutine);
        }

        // Update text immediately
        UpdateText();

        // Start animation
        fillCoroutine = StartCoroutine(AnimateExp());
    }

    /// <summary>
    /// Set experience instantly without animation
    /// </summary>
    public void SetExperienceInstant(float progress)
    {
        progress = Mathf.Clamp01(progress);
        currentFillAmount = progress;
        targetFillAmount = progress;

        // Stop any existing animation
        if (fillCoroutine != null)
        {
            StopCoroutine(fillCoroutine);
        }

        UpdateFill(progress);
        UpdateText();
    }

    /// <summary>
    /// Set experience instantly with XP numbers
    /// </summary>
    public void SetExperienceInstant(float progress, int current, int required)
    {
        currentXP = current;
        requiredXP = required;
        
        // Set fill immediately
        progress = Mathf.Clamp01(progress);
        currentFillAmount = progress;
        targetFillAmount = progress;
        
        UpdateFill(progress);
        UpdateText();
    }

    /// <summary>
    /// Get current experience fill amount
    /// </summary>
    public float GetExperience()
    {
        return currentFillAmount;
    }

    private IEnumerator AnimateExp()
    {
        // Smoothly animate from current to target fill amount
        while (Mathf.Abs(currentFillAmount - targetFillAmount) > 0.01f)
        {
            currentFillAmount = Mathf.Lerp(currentFillAmount, targetFillAmount, Time.deltaTime * fillSpeed);
            UpdateFill(currentFillAmount);
            yield return null;
        }

        // Snap to final value
        currentFillAmount = targetFillAmount;
        UpdateFill(currentFillAmount);
    }

    private void UpdateFill(float amount)
    {
        if (fillImage != null)
        {
            // UI Image - try both fillAmount and scale
            fillImage.fillAmount = amount;
            fillImage.transform.localScale = new Vector3(amount, 1, 1);
        }
        else if (fill != null)
        {
            // Scale the Fill GameObject (parent of FillSprite)
            fill.localScale = new Vector3(amount, 1, 1);
        }
    }

    private void UpdateText()
    {
        if (expText != null)
        {
            if (showNumbers)
            {
                // Show as "current / required"
                expText.text = $"{currentXP:N0} / {requiredXP:N0}";
            }
            else
            {
                // Show as percentage
                float percent = (requiredXP > 0) ? ((float)currentXP / requiredXP * 100f) : 0f;
                expText.text = $"{percent:F0}%";
            }
        }

        if (expPercentText != null)
        {
            // Always show percentage in expPercentText
            float percent = (requiredXP > 0) ? ((float)currentXP / requiredXP * 100f) : 0f;
            expPercentText.text = $"{percent:F0}%";
        }
    }
}
