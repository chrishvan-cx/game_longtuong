using UnityEngine;
using System.Collections;
using UnityEngine.Events;

/// <summary>
/// Handles hero combat - normal attacks, taking damage, and damage popups
/// </summary>
public class HeroCombat : MonoBehaviour
{
    private HeroUnit hero;
    private HeroAnimator heroAnimator;
    private HeroSkillSystem skillSystem;
    private HealthBar healthBar;

    [Header("Combat Timing")]
    [SerializeField] private float attackAnimDuration = 0.35f;
    [SerializeField] private float hitAnimDuration = 0.5f;
    [SerializeField] private AnimationCurve moveCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    private HeroUnit currentTarget;
    private bool isTakingDamage = false;

    public bool IsTakingDamage => isTakingDamage;

    // Damage Event System
    public UnityEvent OnTakeDamage = new UnityEvent();

    public void Initialize(HeroUnit heroUnit)
    {
        hero = heroUnit;
        heroAnimator = GetComponent<HeroAnimator>();
        skillSystem = GetComponent<HeroSkillSystem>();
    }

    public void SetHealthBar(HealthBar bar)
    {
        healthBar = bar;
    }

    public IEnumerator PerformAttack(HeroUnit target)
    {
        if (!hero.isAlive || target == null || IsBattleEnded())
            yield break;

        hero.IsPerformingAction = true;
        currentTarget = target;

        // STEP 1: Move towards target
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

        // Check if battle ended during movement
        if (IsBattleEnded())
        {
            hero.IsPerformingAction = false;
            yield break;
        }

        // STEP 2: Trigger attack animation
        heroAnimator.TriggerAttack();

        // STEP 3: Wait for attack animation to complete
        float timeWaited = 0f;
        float damageWaitTimeout = attackAnimDuration * 3f;
        while (!heroAnimator.IsAttackAnimationComplete && timeWaited < damageWaitTimeout)
        {
            yield return null;
            timeWaited += Time.deltaTime;
        }

        yield return null; // Wait one frame for animation events

        // STEP 4: Wait for target's hit reaction
        if (currentTarget != null && currentTarget.GetComponent<HeroCombat>().IsTakingDamage)
        {
            timeWaited = 0f;
            while (currentTarget.GetComponent<HeroCombat>().IsTakingDamage && timeWaited < hitAnimDuration * 3f)
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
        skillSystem.AddEnergy(HeroSkillSystem.EnergyGainPerEvent);

        // STEP 7: Return to idle
        heroAnimator.TriggerIdle();

        yield return new WaitForSeconds(0.3f);
        hero.IsPerformingAction = false;
    }

    // Called by animation event during attack
    public void OnAttackHit()
    {
        if (currentTarget != null && currentTarget.isAlive)
        {
            int damage = hero.data.physDmg;
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

    public IEnumerator TakeDamage(int dmg)
    {
        if (!hero.isAlive)
            yield break;

        // Wait if already taking damage
        if (isTakingDamage)
        {
            while (isTakingDamage)
            {
                yield return null;
            }
        }

        isTakingDamage = true;

        // Apply HP loss
        int previousHP = hero.currentHP;
        int newHP = hero.currentHP - dmg;
        if (newHP < 0) newHP = 0;
        hero.currentHP = newHP;

        // Spawn damage popup
        SpawnDamagePopup(dmg);

        // Update UI
        if (healthBar != null)
        {
            healthBar.SetHealth(hero.currentHP, hero.data.maxHP);
        }
        hero.UpdateHPUI();

        // Fire HP change event
        hero.OnHPChanged.Invoke(previousHP, hero.currentHP);

        // Gain energy from taking damage (only if alive)
        if (hero.currentHP > 0)
        {
            skillSystem.AddEnergy(HeroSkillSystem.EnergyGainPerEvent);
        }

        // Play hit animation
        yield return heroAnimator.PlayHitAnimation();

        // Check if hero died
        if (hero.currentHP == 0)
        {
            hero.OnHeroDeath();
        }

        isTakingDamage = false;
    }

    private void SpawnDamagePopup(int damage)
    {
        if (hero.damagePopupPrefab == null)
            return;

        Vector3 spawnPos = hero.damagePopupSpawnPoint != null
            ? hero.damagePopupSpawnPoint.position
            : transform.position + Vector3.up * 1.5f;

        GameObject popupObj = Instantiate(hero.damagePopupPrefab, spawnPos, Quaternion.identity);
        DamagePopup popup = popupObj.GetComponent<DamagePopup>();
        if (popup != null)
        {
            popup.SetDamage(damage);
        }
    }

    private bool IsBattleEnded()
    {
        return BattleManager.Instance != null && BattleManager.Instance.IsBattleEnded();
    }
}
