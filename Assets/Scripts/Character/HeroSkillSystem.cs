using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Events;

/// <summary>
/// Handles hero special skills, energy system, and skill visual effects
/// </summary>
public class HeroSkillSystem : MonoBehaviour
{
    private HeroUnit hero;
    private HeroAnimator heroAnimator;

    [Header("Energy System")]
    private int currentEnergy;
    private const int MaxEnergy = 100;
    public const int EnergyGainPerEvent = 25;

    public int CurrentEnergy => currentEnergy;

    // Energy Change Event System
    [System.Serializable]
    public class EnergyChangeEvent : UnityEvent<int> { }
    public EnergyChangeEvent OnEnergyChanged = new EnergyChangeEvent();

    public void Initialize(HeroUnit heroUnit)
    {
        hero = heroUnit;
        heroAnimator = GetComponent<HeroAnimator>();
    }

    public void SetupEnergy(int initialEnergy)
    {
        currentEnergy = initialEnergy;
        OnEnergyChanged.Invoke(currentEnergy);
    }

    public bool CanUseSpecialSkill()
    {
        return currentEnergy >= MaxEnergy;
    }

    public void AddEnergy(int amount)
    {
        if (!hero.isAlive)
            return;

        currentEnergy += amount;
        // Energy cap disabled to allow overflow bonus damage
        if (currentEnergy < 0)
            currentEnergy = 0;

        OnEnergyChanged.Invoke(currentEnergy);
    }

    public IEnumerator PerformSpecialSkill()
    {
        if (!hero.isAlive || hero.data.specialSkill == null || IsBattleEnded())
            yield break;

        hero.IsPerformingAction = true;
        Skill skill = hero.data.specialSkill;

        // STEP 1: Play cast animation
        heroAnimator.TriggerCastSpell();

        // Show skill name text
        if (hero.skillText != null)
        {
            hero.skillText.gameObject.SetActive(true);
            hero.skillText.text = skill.skillName;
        }

        // STEP 2: Wait for cast windup
        yield return new WaitForSeconds(skill.castWindup);

        // Check if battle ended during windup
        if (IsBattleEnded())
        {
            if (hero.skillText != null)
                hero.skillText.gameObject.SetActive(false);
            hero.IsPerformingAction = false;
            yield break;
        }

        // STEP 3: Get enemy targets
        var aliveEnemies = GetAliveEnemies();
        if (aliveEnemies.Count == 0)
        {
            // No targets, reset energy and exit
            currentEnergy = 0;
            OnEnergyChanged.Invoke(currentEnergy);
            hero.IsPerformingAction = false;
            yield break;
        }

        // STEP 4: Execute skill based on type
        if (skill.skillType == SkillType.Meteor)
        {
            yield return ExecuteMeteorSkill(skill, aliveEnemies);
        }
        else if (skill.skillType == SkillType.Projectile)
        {
            yield return ExecuteProjectileSkill(skill, aliveEnemies);
        }

        // STEP 5: Hide skill text
        if (hero.skillText != null)
        {
            hero.skillText.gameObject.SetActive(false);
        }

        // STEP 6: Reset energy to 0
        currentEnergy = 0;
        OnEnergyChanged.Invoke(currentEnergy);

        yield return new WaitForSeconds(0.3f);
        hero.IsPerformingAction = false;
    }

    private IEnumerator ExecuteMeteorSkill(Skill skill, List<HeroUnit> targets)
    {
        if (skill.isAOE)
        {
            // AOE: Launch all meteors simultaneously
            int meteorCount = targets.Count;
            int completedMeteors = 0;

            foreach (var enemy in targets)
            {
                StartCoroutine(MeteorFallWithCallback(enemy, skill, () => completedMeteors++));
            }

            // Transition to Idle immediately after launching
            heroAnimator.TriggerIdle();

            // Wait for ALL meteors to complete
            while (completedMeteors < meteorCount)
            {
                yield return null;
            }
        }
        else
        {
            // Single target meteor
            var target = targets[UnityEngine.Random.Range(0, targets.Count)];
            yield return MeteorFall(target, skill);
            heroAnimator.TriggerIdle();
        }
    }

    private IEnumerator ExecuteProjectileSkill(Skill skill, List<HeroUnit> targets)
    {
        if (skill.isAOE)
        {
            // AOE: Launch all projectiles simultaneously
            int projectileCount = targets.Count;
            int completedProjectiles = 0;

            foreach (var enemy in targets)
            {
                StartCoroutine(ProjectileFlyWithCallback(enemy, skill, () => completedProjectiles++));
            }

            // Transition to Idle immediately after launching
            heroAnimator.TriggerIdle();

            // Wait for ALL projectiles to complete
            while (completedProjectiles < projectileCount)
            {
                yield return null;
            }
        }
        else
        {
            // Single target projectile
            var target = targets[UnityEngine.Random.Range(0, targets.Count)];
            yield return ProjectileFly(target, skill);
            heroAnimator.TriggerIdle();
        }
    }

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
        if (target.heroStationRenderer != null)
        {
            groundPosition = target.heroStationRenderer.transform.position;
        }

        // Calculate spawn position above the ground target
        Vector3 spawnPosition = groundPosition + Vector3.up * skill.meteorSpawnHeight;
        Vector3 targetPosition = groundPosition;

        // Instantiate meteor effect
        GameObject meteorObj = Instantiate(skill.effectPrefab, spawnPosition, Quaternion.identity);
        MeteorVisual meteorVisual = meteorObj.GetComponent<MeteorVisual>();

        // Animate meteor falling
        float elapsed = 0f;
        while (elapsed < skill.meteorFallDuration)
        {
            if (meteorObj != null)
            {
                float t = elapsed / skill.meteorFallDuration;
                meteorObj.transform.position = Vector3.Lerp(spawnPosition, targetPosition, t);

                if (meteorVisual != null)
                {
                    meteorVisual.SetFallProgress(t);
                }
            }
            elapsed += Time.deltaTime;
            yield return null;
        }

        // Ensure meteor reaches target
        if (meteorObj != null)
        {
            meteorObj.transform.position = targetPosition;

            // Stop rotation and start fade-out
            if (meteorVisual != null)
            {
                meteorVisual.rotateWhileFalling = false;
            }

            StartCoroutine(FadeOutAndDestroy(meteorObj, skill.impactPause));
        }

        // Apply damage with overflow energy bonus
        if (target != null && target.isAlive)
        {
            int totalDamage = CalculateSkillDamage(skill);
            yield return target.TakeDamage(totalDamage);
        }
        else
        {
            yield return new WaitForSeconds(skill.impactPause);
        }
    }

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
                                new Vector3(skill.projectileSpawnOffsetX * (hero.teamId == 0 ? 1 : -1),
                                            skill.projectileSpawnOffsetY - 1,
                                            0);

        // Calculate target position
        Vector3 bodyPosition = target.transform.position;
        if (target.heroBody != null)
        {
            bodyPosition = target.heroBody.transform.position;
        }
        Vector3 targetPosition = bodyPosition;

        // Instantiate projectile effect
        GameObject projectileObj = Instantiate(skill.effectPrefab, spawnPosition, Quaternion.identity);
        BlastVisual blastVisual = projectileObj.GetComponent<BlastVisual>();

        // Calculate flight duration
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

            // Stop rotation and start fade-out
            if (blastVisual != null)
            {
                blastVisual.StopRotation();
            }

            StartCoroutine(FadeOutAndDestroy(projectileObj, skill.impactPause));
        }

        // Apply damage with overflow energy bonus
        if (target != null && target.isAlive)
        {
            int totalDamage = CalculateSkillDamage(skill);
            yield return target.TakeDamage(totalDamage);
        }
        else
        {
            yield return new WaitForSeconds(skill.impactPause);
        }
    }

    private int CalculateSkillDamage(Skill skill)
    {
        // Calculate base damage from skill multiplier
        float baseDamage = hero.data.physDmg * skill.damageMultiplier;

        // Calculate overflow bonus if energy > 100%
        float overflowBonus = 0f;
        if (currentEnergy > MaxEnergy)
        {
            // For every 25% overflow (25 energy points), add 25% of base attack damage
            int overflowEnergy = currentEnergy - MaxEnergy;
            float overflowMultiplier = overflowEnergy / 25f;
            overflowBonus = hero.data.physDmg * 0.25f * overflowMultiplier;
        }

        return Mathf.RoundToInt(baseDamage + overflowBonus);
    }

    private IEnumerator FadeOutAndDestroy(GameObject obj, float duration)
    {
        if (obj == null)
            yield break;

        // Get all renderers
        Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();

        // Store original colors
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

        Destroy(obj);
    }

    private List<HeroUnit> GetAliveEnemies()
    {
        var bm = BattleManager.Instance;
        if (bm == null) return new List<HeroUnit>();

        var enemies = hero.teamId == 0 ? bm.teamB : bm.teamA;
        return enemies.FindAll(e => e != null && e.isAlive);
    }

    private bool IsBattleEnded()
    {
        return BattleManager.Instance != null && BattleManager.Instance.IsBattleEnded();
    }
}
