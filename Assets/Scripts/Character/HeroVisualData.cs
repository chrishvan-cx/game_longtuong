using UnityEngine;

/// <summary>
/// Visual-only hero data (safe to include in build)
/// Stats come from server to prevent hacking
/// </summary>
[CreateAssetMenu(fileName = "HeroVisual", menuName = "TurnBased/Hero Visual Data")]
public class HeroVisualData : ScriptableObject
{
    [Header("Identity")]
    public string heroId;           // Unique ID for server lookup
    public string heroName;         // Display name
    
    [Header("Visuals")]
    public Sprite sprite;           // Character portrait
    public GameObject modelPrefab;  // 3D model (optional)
    public RuntimeAnimatorController animatorController;
    
    [Header("VFX")]
    public GameObject hitVFX;
    public GameObject deathVFX;
    
    [Header("Audio")]
    public AudioClip[] attackSounds;
    public AudioClip[] hitSounds;
    public AudioClip deathSound;
    
    // NO STATS HERE!
    // Stats come from server to prevent cheating
}
