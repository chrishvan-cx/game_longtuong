using System.Collections;
using TMPro;
using UnityEngine;

public class DamagePopup : MonoBehaviour
{
    [Header("References")]
    public TMP_Text damageText;

    [Header("Animation Settings")]
    private float riseSpeed = 1f;
    private float lifetime = 2.5f;
    private float fadeInDuration = 0.15f;
    private float fadeOutStartTime = 1f;

    private float elapsedTime = 0f;
    private Color originalColor;
    private Vector3 velocity;
    private bool isInitialized = false;

    void Start()
    {
        // Only start animation if SetDamage was called (proper initialization)
        if (!isInitialized)
        {
            // This is likely a prefab or template in the scene, don't animate
            if (damageText != null)
                damageText.text = "";
            return;
        }
    }

    void Update()
    {
        if (!isInitialized) return;

        elapsedTime += Time.deltaTime;

        // Move upward
        transform.position += velocity * Time.deltaTime;

        // Update alpha based on time
        if (damageText != null)
        {
            Color newColor = originalColor;

            // Fade in at start
            if (elapsedTime < fadeInDuration)
            {
                float fadeInProgress = elapsedTime / fadeInDuration;
                newColor.a = Mathf.Lerp(0f, originalColor.a, fadeInProgress);
            }
            // Fade out near end
            else if (elapsedTime >= fadeOutStartTime)
            {
                float fadeOutProgress = (elapsedTime - fadeOutStartTime) / (lifetime - fadeOutStartTime);
                newColor.a = Mathf.Lerp(originalColor.a, 0f, fadeOutProgress);
            }
            else
            {
                // Full opacity in the middle
                newColor.a = originalColor.a;
            }

            damageText.color = newColor;
        }
    }

    public void SetDamage(int damage)
    {
        isInitialized = true;

        if (damageText != null)
        {
            damageText.text = damage.ToString();
            // Store original color (with full alpha)
            originalColor = damageText.color;
            originalColor.a = 1f; // Ensure full opacity is stored
            
            // Start with transparent for fade-in
            Color startColor = originalColor;
            startColor.a = 0f;
            damageText.color = startColor;
        }

        // Add slight random horizontal drift
        velocity = new Vector3(Random.Range(-0.2f, 0.2f), riseSpeed, 0f);
        
        // Auto-destroy after lifetime
        Destroy(gameObject, lifetime);
    }
}
