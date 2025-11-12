using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine.Events;

public class HeroUnit : MonoBehaviour
{
    [Header("References")]
    public Animator animator;              // PlayerAnimation Animator
    public Transform modelRoot;            // model (to flip left/right)
    public Image hpFill;                   // HeartBar/Image
    public TMP_Text nameText;              // Text (TMP)
    public TMP_Text skillText;             // Skill text component (access GameObject via .gameObject)
    public SpriteRenderer heroStationRenderer; // Herostation indicator
    public GameObject heroBody; // Hero body
    public GameObject damagePopupPrefab;   // Damage popup prefab
    public Transform damagePopupSpawnPoint; // Where to spawn damage popup (above hero)

    [HideInInspector]
    public HeroData data;

    [Header("Stats")]
    public int currentHP;
    public bool isAlive = true;
    public int teamId; // 0 = left, 1 = right
    public bool IsPerformingAction { get; private set; } = false;

    [Header("Energy System")]
    private int currentEnergy;
    private const int MaxEnergy = 100;
    private const int EnergyGainPerEvent = 25;
    public int CurrentEnergy => currentEnergy; // Read-only property for UI

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
    [SerializeField] private EnergyBar energyBar;

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

    // Energy Change Event System - triggers when energy changes
    [System.Serializable]
    public class EnergyChangeEvent : UnityEvent<int> { } // newEnergy
    public EnergyChangeEvent OnEnergyChanged = new EnergyChangeEvent();

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

        // Initialize energy to 50 (50% of max)
        currentEnergy = data.energy;
        OnEnergyChanged.Invoke(currentEnergy);

        // Initialize energy bar UI
        if (energyBar != null)
            energyBar.SetEnergy(currentEnergy);

        // Initialize health bar UI
        if (healthBar != null)
            healthBar.SetHealth(currentHP, data.maxHP);

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

        // Check if hero has full energy and a special skill
        if (currentEnergy >= MaxEnergy && data.specialSkill != null)
        {
            // Perform special skill attack
            yield return PerformSpecialSkill();
        }
        else
        {
            // Choose a random alive enemy for normal attack
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

            // Execute normal attack
            yield return PerformAttack(target);
        }

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
        {
            healthBar.SetHealth(currentHP, data.maxHP);
        }
        if (codeHP != null)
        {
            codeHP.SetHealth((float)currentHP / data.maxHP, data.maxHP);
        }
        UpdateHPUI();

        // Fire HP change event
        OnHPChanged.Invoke(previousHP, currentHP);

        // Gain energy from taking damage (only if alive)
        if (currentHP > 0)
        {
            AddEnergy(EnergyGainPerEvent);
        }

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

        // STEP 6: Gain energy from performing attack
        AddEnergy(EnergyGainPerEvent);

        // STEP 7: Reset attacker to Idle state (turn complete)
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

    // Energy System Methods
    // Note: Energy can overflow past 100% for bonus damage
    // Every 25% overflow adds 25% of base attack damage to special skills
    private void AddEnergy(int amount)
    {
        if (!isAlive)
            return;

        currentEnergy += amount;
        // Energy cap disabled to allow overflow bonus damage
        // if (currentEnergy > MaxEnergy)
        //     currentEnergy = MaxEnergy;
        if (currentEnergy < 0)
            currentEnergy = 0;

        OnEnergyChanged.Invoke(currentEnergy);
    }

    // Special Skill System
    public IEnumerator PerformSpecialSkill()
    {
        if (!isAlive || data.specialSkill == null)
        {
            yield break;
        }

        IsPerformingAction = true;
        Skill skill = data.specialSkill;

        // STEP 1: Play cast animation and VFX
        // Note: Don't use Attack animation as it triggers OnAttackHit() event!
        // Use Idle or create a separate Cast animation without damage events
        if (animator != null)
        {
            animator.SetTrigger("CastSpell");
        }
        
        // Show skill name text
        if (skillText != null)
        {
            skillText.gameObject.SetActive(true);
            skillText.text = skill.skillName;
        }

        // STEP 2: Wait for cast windup
        yield return new WaitForSeconds(skill.castWindup);

        // STEP 3: Get enemy targets
        var bm = BattleManager.Instance;
        if (bm == null)
        {
            IsPerformingAction = false;
            yield break;
        }

        var enemies = teamId == 0 ? bm.teamB : bm.teamA;
        if (enemies == null)
        {
            IsPerformingAction = false;
            yield break;
        }

        // Build list of alive enemies
        var aliveEnemies = new System.Collections.Generic.List<HeroUnit>();
        for (int i = 0; i < enemies.Count; i++)
        {
            var e = enemies[i];
            if (e != null && e.isAlive) aliveEnemies.Add(e);
        }

        if (aliveEnemies.Count == 0)
        {
            // No targets, reset energy and exit
            currentEnergy = 0;
            OnEnergyChanged.Invoke(currentEnergy);
            IsPerformingAction = false;
            yield break;
        }

        // STEP 4: Execute skill based on type (Meteor or Projectile)
        if (skill.skillType == SkillType.Meteor)
        {
            // Meteor skill - falls from above
            if (skill.isAOE)
            {
                // AOE: Launch all meteors simultaneously
                int meteorCount = aliveEnemies.Count;
                int completedMeteors = 0;

                foreach (var enemy in aliveEnemies)
                {
                    StartCoroutine(MeteorFallWithCallback(enemy, skill, () => completedMeteors++));
                }

                // Transition to Idle immediately after launching
                if (animator != null)
                {
                    animator.SetTrigger("Idle");
                }

                // Wait for ALL meteors to complete
                while (completedMeteors < meteorCount)
                {
                    yield return null;
                }
            }
            else
            {
                // Single target meteor
                var target = aliveEnemies[UnityEngine.Random.Range(0, aliveEnemies.Count)];
                yield return MeteorFall(target, skill);

                if (animator != null)
                {
                    animator.SetTrigger("Idle");
                }
            }
        }
        else if (skill.skillType == SkillType.Projectile)
        {
            // Projectile skill - flies from caster to target
            if (skill.isAOE)
            {
                // AOE: Launch all projectiles simultaneously
                int projectileCount = aliveEnemies.Count;
                int completedProjectiles = 0;

                foreach (var enemy in aliveEnemies)
                {
                    StartCoroutine(ProjectileFlyWithCallback(enemy, skill, () => completedProjectiles++));
                }

                // Transition to Idle immediately after launching
                if (animator != null)
                {
                    animator.SetTrigger("Idle");
                }

                // Wait for ALL projectiles to complete
                while (completedProjectiles < projectileCount)
                {
                    yield return null;
                }
            }
            else
            {
                // Single target projectile
                var target = aliveEnemies[UnityEngine.Random.Range(0, aliveEnemies.Count)];
                yield return ProjectileFly(target, skill);

                if (animator != null)
                {
                    animator.SetTrigger("Idle");
                }
            }
        }

        // STEP 5: Hide skill text
        if (skillText != null)
        {
            skillText.gameObject.SetActive(false);
        }
        
        // STEP 6: Reset energy to 0
        currentEnergy = 0;
        OnEnergyChanged.Invoke(currentEnergy);

        yield return new WaitForSeconds(0.3f);
        IsPerformingAction = false;
    }

    // Wrapper for MeteorFall with completion callback (used for AOE)
    private IEnumerator MeteorFallWithCallback(HeroUnit target, Skill skill, System.Action onComplete)
    {
        yield return MeteorFall(target, skill);
        onComplete?.Invoke();
    }

    private IEnumerator MeteorFall(HeroUnit target, Skill skill)
    {
        if (target == null || !target.isAlive || skill.effectPrefab == null)
            yield break;

        // Calculate target position at ground (herostation position)
        Vector3 groundPosition = target.transform.position;

        // If target has heroStationRenderer, use its position for more accurate ground placement
        if (target.heroStationRenderer != null)
        {
            groundPosition = target.heroStationRenderer.transform.position;
        }

        // Calculate spawn position above the ground target
        Vector3 spawnPosition = groundPosition + Vector3.up * skill.meteorSpawnHeight;
        Vector3 targetPosition = groundPosition;

        // Instantiate meteor effect
        GameObject meteorObj = Instantiate(skill.effectPrefab, spawnPosition, Quaternion.identity);

        // Get MeteorVisual component to control animation
        MeteorVisual meteorVisual = meteorObj.GetComponent<MeteorVisual>();

        // Animate meteor falling
        float elapsed = 0f;
        while (elapsed < skill.meteorFallDuration)
        {
            if (meteorObj != null)
            {
                float t = elapsed / skill.meteorFallDuration;
                meteorObj.transform.position = Vector3.Lerp(spawnPosition, targetPosition, t);

                // Update fall progress for visual effects
                if (meteorVisual != null)
                {
                    meteorVisual.SetFallProgress(t);
                }
            }
            elapsed += Time.deltaTime;
            yield return null;
        }

        // Ensure meteor reaches target (ground/herostation)
        if (meteorObj != null)
        {
            meteorObj.transform.position = targetPosition;

            // IMPACT: Stop rotation and start fade-out IMMEDIATELY
            if (meteorVisual != null)
            {
                meteorVisual.rotateWhileFalling = false;
            }

            // Start fade-out immediately (don't wait)
            StartCoroutine(FadeOutAndDestroy(meteorObj, skill.impactPause));
        }

        // IMPACT: Apply damage with overflow energy bonus (in parallel with fade)
        if (target != null && target.isAlive)
        {
            // Calculate base damage from skill multiplier
            float baseDamage = data.attack * skill.damageMultiplier;

            // Calculate overflow bonus if energy > 100%
            float overflowBonus = 0f;
            if (currentEnergy > MaxEnergy)
            {
                // For every 25% overflow (25 energy points), add 25% of base attack damage
                int overflowEnergy = currentEnergy - MaxEnergy;
                float overflowMultiplier = overflowEnergy / 25f; // How many 25% increments
                overflowBonus = data.attack * 0.25f * overflowMultiplier;
            }

            // Total damage = base skill damage + overflow bonus
            int totalDamage = Mathf.RoundToInt(baseDamage + overflowBonus);

            StartCoroutine(target.TakeDamage(totalDamage));
        }

        // Wait for fade to complete before ending this coroutine
        yield return new WaitForSeconds(skill.impactPause);
    }

    // Wrapper for ProjectileFly with completion callback (used for AOE)
    private IEnumerator ProjectileFlyWithCallback(HeroUnit target, Skill skill, System.Action onComplete)
    {
        yield return ProjectileFly(target, skill);
        onComplete?.Invoke();
    }

    private IEnumerator ProjectileFly(HeroUnit target, Skill skill)
    {
        if (target == null || !target.isAlive || skill.effectPrefab == null)
            yield break;

        // Calculate spawn position near caster
        Vector3 spawnPosition = transform.position +
                                new Vector3(skill.projectileSpawnOffsetX * (teamId == 0 ? 1 : -1),
                                            skill.projectileSpawnOffsetY - 1,
                                            0);

        // Calculate target position at ground (herostation position)
        Vector3 bodyPosition = target.transform.position;

        // If target has heroStationRenderer, use its position for more accurate ground placement
        if (target.heroBody != null)
        {
            bodyPosition = target.heroBody.transform.position;
        }
        Vector3 targetPosition = bodyPosition;

        // Instantiate projectile effect
        GameObject projectileObj = Instantiate(skill.effectPrefab, spawnPosition, Quaternion.identity);

        // Get BlastVisual component to control animation
        BlastVisual blastVisual = projectileObj.GetComponent<BlastVisual>();

        // Calculate flight duration based on distance and speed
        float distance = Vector3.Distance(spawnPosition, targetPosition);
        float flyDuration = distance / skill.projectileSpeed;

        // Animate projectile flying
        float elapsed = 0f;
        while (elapsed < flyDuration)
        {
            if (projectileObj != null)
            {
                float t = elapsed / flyDuration;
                projectileObj.transform.position = Vector3.Lerp(spawnPosition, targetPosition, t);

                // Update fly progress for visual effects
                if (blastVisual != null)
                {
                    blastVisual.SetFlyProgress(t);
                }
            }
            elapsed += Time.deltaTime;
            yield return null;
        }

        // Ensure projectile reaches target
        if (projectileObj != null)
        {
            projectileObj.transform.position = targetPosition;

            // IMPACT: Stop rotation and start fade-out IMMEDIATELY
            if (blastVisual != null)
            {
                blastVisual.StopRotation();
            }

            // Start fade-out immediately (don't wait)
            StartCoroutine(FadeOutAndDestroy(projectileObj, skill.impactPause));
        }

        // IMPACT: Apply damage with overflow energy bonus (in parallel with fade)
        if (target != null && target.isAlive)
        {
            // Calculate base damage from skill multiplier
            float baseDamage = data.attack * skill.damageMultiplier;

            // Calculate overflow bonus if energy > 100%
            float overflowBonus = 0f;
            if (currentEnergy > MaxEnergy)
            {
                // For every 25% overflow (25 energy points), add 25% of base attack damage
                int overflowEnergy = currentEnergy - MaxEnergy;
                float overflowMultiplier = overflowEnergy / 25f; // How many 25% increments
                overflowBonus = data.attack * 0.25f * overflowMultiplier;
            }

            // Total damage = base skill damage + overflow bonus
            int totalDamage = Mathf.RoundToInt(baseDamage + overflowBonus);

            StartCoroutine(target.TakeDamage(totalDamage));
        }

        // Wait for fade to complete before ending this coroutine
        yield return new WaitForSeconds(skill.impactPause);
    }

    // Helper coroutine to fade out and destroy a game object
    private IEnumerator FadeOutAndDestroy(GameObject obj, float duration)
    {
        if (obj == null)
            yield break;

        // Get all renderers on the meteor
        Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();

        // Store original colors/alphas
        Dictionary<Renderer, Color[]> originalColors = new Dictionary<Renderer, Color[]>();
        foreach (var renderer in renderers)
        {
            Material[] materials = renderer.materials;
            Color[] colors = new Color[materials.Length];
            for (int i = 0; i < materials.Length; i++)
            {
                colors[i] = materials[i].color;
            }
            originalColors[renderer] = colors;
        }

        // Fade out over time
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(1f, 0f, elapsed / duration);

            // Fade all renderers
            foreach (var kvp in originalColors)
            {
                if (kvp.Key == null) continue;

                Material[] materials = kvp.Key.materials;
                Color[] origColors = kvp.Value;

                for (int i = 0; i < materials.Length && i < origColors.Length; i++)
                {
                    Color newColor = origColors[i];
                    newColor.a = alpha;
                    materials[i].color = newColor;
                }
            }

            yield return null;
        }

        // Destroy the object
        Destroy(obj);
    }

}
