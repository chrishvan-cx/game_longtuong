using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using UnityEngine.Events;

/// <summary>
/// Core HeroUnit component - manages hero state, HP, and coordinates other systems
/// </summary>
public class HeroUnit : MonoBehaviour
{
    [Header("References")]
    public Animator animator;
    public Transform modelRoot;
    public Image hpFill;
    public TMP_Text nameText;
    public TMP_Text skillText;
    public SpriteRenderer heroStationRenderer;
    public GameObject heroBody;
    public GameObject damagePopupPrefab;
    public Transform damagePopupSpawnPoint;

    [Header("UI Views")]
    public GameObject faceIconUI;
    public SpriteRenderer faceIconRenderer;
    public GameObject interfaceUI;

    [HideInInspector]
    public HeroData data;

    [Header("Stats")]
    public int currentHP;
    public bool isAlive = true;
    public int teamId; // 0 = left, 1 = right
    public bool IsPerformingAction { get; set; } = false;

    [Header("Round Tracking")]
    public int h_round = 0;
    public bool is_turn = false;

    [SerializeField] private HealthBar healthBar;
    [SerializeField] private EnergyBar energyBar;

    // HP Change Event System
    [System.Serializable]
    public class HPChangeEvent : UnityEvent<int, int> { }
    public HPChangeEvent OnHPChanged = new HPChangeEvent();

    // Component references (set automatically)
    private HeroCombat combat;
    private HeroSkillSystem skillSystem;
    private HeroAnimator heroAnimator;

    void Awake()
    {
        // Default view: Interface visible, Face Icon hidden
        SetViewMode(HeroViewMode.Interface);

        // Get or add component references
        combat = GetComponent<HeroCombat>();
        if (combat == null) combat = gameObject.AddComponent<HeroCombat>();

        skillSystem = GetComponent<HeroSkillSystem>();
        if (skillSystem == null) skillSystem = gameObject.AddComponent<HeroSkillSystem>();

        heroAnimator = GetComponent<HeroAnimator>();
        if (heroAnimator == null) heroAnimator = gameObject.AddComponent<HeroAnimator>();

        // Initialize components
        combat.Initialize(this);
        skillSystem.Initialize(this);
        heroAnimator.Initialize(this);

        // Pass healthBar reference to combat
        if (healthBar != null)
            combat.SetHealthBar(healthBar);
    }

    void Start()
    {
        if (data != null)
        {
            Setup(data, teamId);
        }
    }

    public void Setup(HeroData d, int team)
    {
        data = d;
        teamId = team;
        currentHP = data.maxHP;
        isAlive = true;
        h_round = 0;
        is_turn = false;

        // Hide skill text initially
        if (skillText != null)
            skillText.gameObject.SetActive(false);

        // Initialize skill system
        skillSystem.SetupEnergy(data.energy);

        // Initialize UI
        if (energyBar != null)
            energyBar.SetEnergy(skillSystem.CurrentEnergy);

        if (healthBar != null)
            healthBar.SetHealth(currentHP, data.maxHP);

        // Setup animator
        if (animator != null && data.animatorController != null)
        {
            animator.runtimeAnimatorController = data.animatorController;
            animator.Play("Idle", 0, 0f);
        }

        if (nameText != null)
            nameText.text = data.heroName;

        // Setup face icon
        if (faceIconRenderer != null && data.sprite != null)
        {
            faceIconRenderer.sprite = data.sprite;
        }

        SetHeroStationVisible(false);
        UpdateHPUI();
    }

    public void UpdateHPUI()
    {
        if (hpFill != null)
        {
            float fillAmount = (float)currentHP / data.maxHP;
            hpFill.fillAmount = fillAmount;
            hpFill.transform.localScale = new Vector3(fillAmount, 1, 1);
        }
    }

    public IEnumerator TakeTurn()
    {
        if (!isAlive || IsBattleEnded())
            yield break;

        SetHeroStationVisible(true);

        // Check if hero should use special skill
        if (skillSystem.CanUseSpecialSkill() && data.specialSkill != null)
        {
            yield return skillSystem.PerformSpecialSkill();
        }
        else
        {
            // Normal attack - find target
            var target = GetRandomAliveEnemy();
            if (target != null && !IsBattleEnded())
            {
                yield return combat.PerformAttack(target);
            }
        }

        SetHeroStationVisible(false);
    }

    public IEnumerator TakeDamage(int dmg)
    {
        yield return combat.TakeDamage(dmg);
    }

    public void SetHeroStationVisible(bool visible)
    {
        if (heroStationRenderer != null)
            heroStationRenderer.enabled = visible;
    }

    public void Flip(bool facingLeft)
    {
        if (heroAnimator != null)
            heroAnimator.Flip(facingLeft);
    }

    public void OnHeroDeath()
    {
        StartCoroutine(Die());
    }

    private IEnumerator Die()
    {
        isAlive = false;
        SetHeroStationVisible(false);

        // Fade out hero
        float fadeDuration = 1f;
        float elapsed = 0f;

        SpriteRenderer[] spriteRenderers = GetComponentsInChildren<SpriteRenderer>();
        MeshRenderer[] meshRenderers = GetComponentsInChildren<MeshRenderer>();
        SkinnedMeshRenderer[] skinnedRenderers = GetComponentsInChildren<SkinnedMeshRenderer>();

        Color[] originalSpriteColors = new Color[spriteRenderers.Length];
        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            originalSpriteColors[i] = spriteRenderers[i].color;
        }

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(1f, 0f, elapsed / fadeDuration);

            for (int i = 0; i < spriteRenderers.Length; i++)
            {
                Color newColor = originalSpriteColors[i];
                newColor.a = alpha;
                spriteRenderers[i].color = newColor;
            }

            foreach (var renderer in meshRenderers)
            {
                foreach (var mat in renderer.materials)
                {
                    Color color = mat.color;
                    color.a = alpha;
                    mat.color = color;
                }
            }

            foreach (var renderer in skinnedRenderers)
            {
                foreach (var mat in renderer.materials)
                {
                    Color color = mat.color;
                    color.a = alpha;
                    mat.color = color;
                }
            }

            yield return null;
        }

        // Fade out UI
        if (hpFill != null)
        {
            var hpColor = hpFill.color;
            hpColor.a = 0f;
            hpFill.color = hpColor;
        }
        if (nameText != null)
        {
            var textColor = nameText.color;
            textColor.a = 0f;
            nameText.color = textColor;
        }

        gameObject.SetActive(false);
    }

    private HeroUnit GetRandomAliveEnemy()
    {
        var bm = BattleManager.Instance;
        if (bm == null) return null;

        var enemies = teamId == 0 ? bm.teamB : bm.teamA;
        if (enemies == null) return null;

        var alive = new System.Collections.Generic.List<HeroUnit>();
        for (int i = 0; i < enemies.Count; i++)
        {
            var e = enemies[i];
            if (e != null && e.isAlive) alive.Add(e);
        }

        if (alive.Count == 0) return null;
        return alive[UnityEngine.Random.Range(0, alive.Count)];
    }

    private bool IsBattleEnded()
    {
        return BattleManager.Instance != null && BattleManager.Instance.IsBattleEnded();
    }

    public void SetViewMode(HeroViewMode mode)
    {
        switch (mode)
        {
            case HeroViewMode.FaceIcon:
                if (faceIconUI != null) faceIconUI.SetActive(true);
                if (interfaceUI != null) interfaceUI.SetActive(false);
                break;
            case HeroViewMode.Interface:
                if (faceIconUI != null) faceIconUI.SetActive(false);
                if (interfaceUI != null) interfaceUI.SetActive(true);
                break;
        }
    }
}

public enum HeroViewMode
{
    FaceIcon,
    Interface
}
