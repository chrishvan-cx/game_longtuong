using UnityEngine;

// Visual component for blast projectile effect (2D Game)
// Attach this to your blast prefab for easy control
public class BlastVisual : MonoBehaviour
{
    [Header("Visual Settings")]
    public bool rotateWhileFlying = true;
    public float rotationSpeed = 360f; // Degrees per second

    [Tooltip("Enable size change during flight. Use minimal curve changes with Trail Renderer.")]
    public bool scaleWhileFlying = true;
    public AnimationCurve scaleCurve = AnimationCurve.EaseInOut(0, 0.9f, 1, 1.05f); // Subtle growth

    [Header("Trail Settings (Optional)")]
    public TrailRenderer trail;

    private Vector3 initialScale;
    private float flyProgress = 0f;

    void Awake()
    {
        initialScale = transform.localScale;

        // CRITICAL: Disable all physics on this object for 2D
        Rigidbody2D rb2d = GetComponent<Rigidbody2D>();
        if (rb2d != null)
        {
            rb2d.isKinematic = true;
            rb2d.gravityScale = 0f;
            rb2d.velocity = Vector2.zero;
            rb2d.angularVelocity = 0f;
        }

        // Also check for 3D rigidbody (shouldn't exist but just in case)
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            Destroy(rb);
        }

        // Disable any colliders to prevent physics interactions
        Collider2D[] colliders = GetComponents<Collider2D>();
        foreach (var col in colliders)
        {
            col.enabled = false;
        }
        
        // Initialize trail if present
        if (trail == null)
        {
            trail = GetComponent<TrailRenderer>();
        }
        
        if (trail != null)
        {
            trail.emitting = true;
        }
    }

    void Update()
    {
        // Rotate blast while flying
        if (rotateWhileFlying)
        {
            transform.Rotate(0, 0, rotationSpeed * Time.deltaTime);
        }

        // Scale blast based on fly progress (set by external script)
        if (scaleWhileFlying && flyProgress > 0f)
        {
            float scaleMultiplier = scaleCurve.Evaluate(flyProgress);
            transform.localScale = initialScale * scaleMultiplier;
        }
    }

    // Called by HeroUnit to update fly animation progress
    public void SetFlyProgress(float progress)
    {
        flyProgress = Mathf.Clamp01(progress);
    }

    // Stop rotation when blast hits target
    public void StopRotation()
    {
        rotateWhileFlying = false;
    }

    // Clear trail when destroyed
    void OnDestroy()
    {
        if (trail != null)
        {
            trail.Clear();
        }
    }
}
