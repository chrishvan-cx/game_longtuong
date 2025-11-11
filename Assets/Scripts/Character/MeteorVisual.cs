using UnityEngine;

// Simple visual component for meteor effect (2D Game)
// Attach this to your meteor prefab for easy control
public class MeteorVisual : MonoBehaviour
{
    [Header("Visual Settings")]
    public bool rotateWhileFalling = true;
    public float rotationSpeed = 360f; // Degrees per second
    public bool scaleWhileFalling = true;
    public AnimationCurve scaleCurve = AnimationCurve.EaseInOut(0, 0.5f, 1, 1f);
    
    [Header("Trail Settings (Optional)")]
    public TrailRenderer trail;
    
    private Vector3 initialScale;
    private float fallProgress = 0f;
    
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
    }
    
    void Update()
    {
        // Rotate meteor around Z axis (2D rotation)
        if (rotateWhileFalling)
        {
            transform.Rotate(0, 0, rotationSpeed * Time.deltaTime);
        }
        
        // Scale meteor based on fall progress (set by external script)
        if (scaleWhileFalling && fallProgress > 0f)
        {
            float scaleMultiplier = scaleCurve.Evaluate(fallProgress);
            transform.localScale = initialScale * scaleMultiplier;
        }
    }
    
    // Called by HeroUnit to update fall animation progress
    public void SetFallProgress(float progress)
    {
        fallProgress = Mathf.Clamp01(progress);
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
