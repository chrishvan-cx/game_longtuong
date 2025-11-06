using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System;
using UnityEngine.Events;

public class HeroUnit : MonoBehaviour
{
    [Header("References")]
    public Animator animator;              // PlayerAnimation Animator
    public Transform modelRoot;            // model (to flip left/right)
    public Image hpFill;                   // HeartBar/Image
    public TMP_Text nameText;              // Text (TMP)
    public SpriteRenderer heroStationRenderer; // Herostation indicator
    public GameObject damagePopupPrefab;   // Damage popup prefab
    public Transform damagePopupSpawnPoint; // Where to spawn damage popup (above hero)

    [Header("Stats")]
    public HeroData data;
    public int currentHP;
    public bool isAlive = true;
    public int teamId; // 0 = left, 1 = right
    public bool IsPerformingAction { get; private set; } = false;

    [Header("Round Tracking")]
    public int h_round = 0;  // Last round this hero acted in
    public bool is_turn = false;  // Currently taking their turn

    [Header("Animation Timing")]
    [SerializeField] private float attackAnimDuration = 0.35f;
    [SerializeField] private float hitAnimDuration = 0.5f;
    [SerializeField] private AnimationCurve moveCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f); // Smooth movement

    private HeroUnit currentTarget;
    private CodeHP codeHP;
    [SerializeField] private HealthBar healthBar;

    // Animation event flags
    private bool attackAnimationComplete = false;
    private bool hitAnimationComplete = false;

    // Public property to expose hit animation state for other heroes to wait on
    public bool IsHitSequenceDone => hitAnimationComplete;

    // Track if currently taking damage to prevent overlapping damage
    private bool isTakingDamage = false;
    public bool IsTakingDamage => isTakingDamage;

    // Damage Event System - triggers when TakeDamage is called (before HP loss)
    public UnityEvent OnTakeDamage = new UnityEvent();

    // HP Change Event System - triggers when HP actually decreases
    [System.Serializable]
    public class HPChangeEvent : UnityEvent<int, int> { } // oldHP, newHP
    public HPChangeEvent OnHPChanged = new HPChangeEvent();

    void Start()
    {
        if (data != null) Setup(data, teamId);
    }

    public void Setup(HeroData d, int team)
    {
        data = d;
        teamId = team;
        currentHP = data.maxHP;
        isAlive = true;
        h_round = 0;
        is_turn = false;

        if (animator != null && data.animatorController != null)
        {
            animator.runtimeAnimatorController = data.animatorController;
            animator.Play("Idle", 0, 0f);
        }

        if (nameText != null)
            nameText.text = data.heroName;

        // Hide hero station indicator initially
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

    // New: self-contained turn coroutine — notifies TurnManager by completion
    public IEnumerator TakeTurn()
    {
        if (!isAlive)
            yield break;

        // Show hero station indicator during turn
        SetHeroStationVisible(true);

        // Choose a random alive enemy
        var bm = BattleManager.Instance;
        if (bm == null)
            yield break;

        var enemies = teamId == 0 ? bm.teamB : bm.teamA;
        if (enemies == null)
            yield break;

        // Build a lightweight list of alive enemies
        var alive = new System.Collections.Generic.List<HeroUnit>();
        for (int i = 0; i < enemies.Count; i++)
        {
            var e = enemies[i];
            if (e != null && e.isAlive) alive.Add(e);
        }
        if (alive.Count == 0)
            yield break;

        var target = alive[UnityEngine.Random.Range(0, alive.Count)];
        if (target == null)
            yield break;

        // Execute the attack fully inside this hero
        yield return PerformAttack(target);

        // Hide hero station indicator after turn
        SetHeroStationVisible(false);
    }

    public IEnumerator TakeDamage(int dmg)
    {
        if (!isAlive)
        {
            yield break;
        }

        if (isTakingDamage)
        {
            // Wait for current damage to fully complete before processing a new one
            while (isTakingDamage)
            {
                yield return null;
            }
        }

        // Set damage state and reset hit animation flag
        isTakingDamage = true;
        hitAnimationComplete = false;

        // Calculate and apply HP loss immediately
        int previousHP = currentHP;
        int newHP = currentHP - dmg;
        if (newHP < 0) newHP = 0;
        currentHP = newHP;

        // Spawn damage popup
        SpawnDamagePopup(dmg);

        // Update UI immediately
        if (healthBar != null)
            healthBar.SetHealth((float)currentHP / data.maxHP);
        if (codeHP != null)
            codeHP.SetHealth((float)currentHP / data.maxHP);
        UpdateHPUI();

        // Fire HP change event
        OnHPChanged.Invoke(previousHP, currentHP);

        // Play hit animation (non-blocking - runs in parallel)
        StartCoroutine(PlayHitAnimationCoroutine());

        // Wait for hit animation to complete
        float timeWaited = 0f;
        while (!hitAnimationComplete && timeWaited < hitAnimDuration * 2f)
        {
            yield return null;
            timeWaited += Time.deltaTime;
        }

        // Check if hero died
        if (currentHP == 0)
        {
            yield return StartCoroutine(Die());
        }

        // Reset damage state
        isTakingDamage = false;
    }

    IEnumerator Die()
    {
        isAlive = false;

        // Hide hero station if visible
        SetHeroStationVisible(false);

        // Fade out hero over time
        float fadeDuration = 1f;
        float elapsed = 0f;

        // Get all renderers (sprite, mesh, etc.) on this hero
        SpriteRenderer[] spriteRenderers = GetComponentsInChildren<SpriteRenderer>();
        MeshRenderer[] meshRenderers = GetComponentsInChildren<MeshRenderer>();
        SkinnedMeshRenderer[] skinnedRenderers = GetComponentsInChildren<SkinnedMeshRenderer>();

        // Store original colors/alphas
        Color[] originalSpriteColors = new Color[spriteRenderers.Length];
        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            originalSpriteColors[i] = spriteRenderers[i].color;
        }

        // Fade out loop
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(1f, 0f, elapsed / fadeDuration);

            // Fade sprite renderers
            for (int i = 0; i < spriteRenderers.Length; i++)
            {
                Color newColor = originalSpriteColors[i];
                newColor.a = alpha;
                spriteRenderers[i].color = newColor;
            }

            // Fade mesh renderers
            foreach (var renderer in meshRenderers)
            {
                foreach (var mat in renderer.materials)
                {
                    Color color = mat.color;
                    color.a = alpha;
                    mat.color = color;
                }
            }

            // Fade skinned mesh renderers
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

        // Fade out UI elements
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

        // Remove from battlefield (deactivate instead of destroy to keep references valid)
        gameObject.SetActive(false);
    }

    public IEnumerator PerformAttack(HeroUnit target)
    {
        if (!isAlive || target == null)
        {
            yield break;
        }

        IsPerformingAction = true;
        currentTarget = target;

        // Reset animation flags
        attackAnimationComplete = false;

        // STEP 1: Move towards the target
        Vector3 originalPosition = transform.position;
        Vector3 targetPosition = target.transform.position;
        float moveOffset = 1.0f;
        Vector3 directionToTarget = (targetPosition - originalPosition).normalized;
        Vector3 attackPosition = targetPosition - directionToTarget * moveOffset;

        float moveDuration = 0.3f;
        float elapsed = 0f;
        while (elapsed < moveDuration)
        {
            float t = moveCurve.Evaluate(elapsed / moveDuration);
            transform.position = Vector3.Lerp(originalPosition, attackPosition, t);
            elapsed += Time.deltaTime;
            yield return null;
        }
        transform.position = attackPosition;

        // STEP 2: Start attack animation
        if (animator != null)
        {
            animator.SetTrigger("Attack");
        }

        // STEP 3: Wait for OnAttackHit() animation event to trigger damage
        // STEP 4: Wait for attack animation and damage to complete
        float timeWaited = 0f;
        float damageWaitTimeout = attackAnimDuration * 3f;
        while ((!attackAnimationComplete) && timeWaited < damageWaitTimeout)
        {
            yield return null;
            timeWaited += Time.deltaTime;
        }

        // FALLBACK: If animation event didn't trigger damage, apply it now
        yield return null; // Wait one frame for animation events to process

        // STEP 4.5: Wait for target's hit reaction to complete
        if (currentTarget != null && currentTarget.isTakingDamage)
        {
            timeWaited = 0f;
            while (currentTarget.isTakingDamage && timeWaited < hitAnimDuration * 3f)
            {
                yield return null;
                timeWaited += Time.deltaTime;
            }
        }

        // STEP 5: Move back to original position
        elapsed = 0f;
        while (elapsed < moveDuration)
        {
            float t = moveCurve.Evaluate(elapsed / moveDuration);
            transform.position = Vector3.Lerp(attackPosition, originalPosition, t);
            elapsed += Time.deltaTime;
            yield return null;
        }
        transform.position = originalPosition;

        // STEP 6: Reset attacker to Idle state (turn complete)
        if (animator != null)
        {
            animator.SetTrigger("Idle");
        }

        yield return new WaitForSeconds(0.3f);
        IsPerformingAction = false;
    }

    // Animation Event methods
    public void OnAttackAnimationComplete()
    {
        attackAnimationComplete = true;
    }

    // Animation Event method - call this from attack animation at the moment of impact
    public void OnAttackHit()
    {
        if (currentTarget != null && currentTarget.isAlive)
        {
            int damage = data.attack;
            StartCoroutine(ApplyDamageToTarget(damage));
        }
    }

    private IEnumerator ApplyDamageToTarget(int damage)
    {
        if (currentTarget != null)
        {
            yield return currentTarget.TakeDamage(damage);
        }
    }

    public void OnHitAnimationComplete()
    {
        hitAnimationComplete = true;
    }


    // Coroutine to play hit animation
    private IEnumerator PlayHitAnimationCoroutine()
    {
        hitAnimationComplete = false;

        if (animator != null)
        {
            string hitTrigger = teamId == 0 ? "HitLeft" : "HitRight";
            animator.SetTrigger(hitTrigger);
        }

        // Wait for animation to complete (via event or timeout)
        float timeWaited = 0f;
        while (!hitAnimationComplete && timeWaited < hitAnimDuration * 2f)
        {
            yield return null;
            timeWaited += Time.deltaTime;
        }

        // Transition back to Idle
        if (animator != null)
        {
            animator.SetTrigger("Idle");
        }
    }


    public void Flip(bool facingLeft)
    {
        if (modelRoot != null)
            modelRoot.localScale = new Vector3(facingLeft ? 1 : -1, 1, 1);
    }

    public void SetHeroStationVisible(bool visible)
    {
        if (heroStationRenderer != null)
            heroStationRenderer.enabled = visible;
    }

    private void SpawnDamagePopup(int damage)
    {
        if (damagePopupPrefab == null)
            return;

        // Determine spawn position (above hero or at spawn point)
        Vector3 spawnPos = damagePopupSpawnPoint != null
            ? damagePopupSpawnPoint.position
            : transform.position + Vector3.up * 1.5f;

        // Instantiate popup
        GameObject popupObj = Instantiate(damagePopupPrefab, spawnPos, Quaternion.identity);

        // Set damage value
        DamagePopup popup = popupObj.GetComponent<DamagePopup>();
        if (popup != null)
        {
            popup.SetDamage(damage);
        }
    }


}