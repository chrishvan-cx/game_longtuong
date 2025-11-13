using UnityEngine;
using System.Collections;

/// <summary>
/// Handles hero animations - triggers, events, and transitions
/// </summary>
public class HeroAnimator : MonoBehaviour
{
    private HeroUnit hero;
    private Animator animator;

    [Header("Animation Timing")]
    [SerializeField] private float hitAnimDuration = 0.5f;

    // Animation event flags
    private bool attackAnimationComplete = false;
    private bool hitAnimationComplete = false;

    public bool IsAttackAnimationComplete => attackAnimationComplete;
    public bool IsHitSequenceDone => hitAnimationComplete;

    public void Initialize(HeroUnit heroUnit)
    {
        hero = heroUnit;
        animator = hero.animator;
    }

    // Animation triggers
    public void TriggerAttack()
    {
        attackAnimationComplete = false;
        if (animator != null)
        {
            animator.SetTrigger("Attack");
        }
    }

    public void TriggerCastSpell()
    {
        if (animator != null)
        {
            animator.SetTrigger("CastSpell");
        }
    }

    public void TriggerIdle()
    {
        if (animator != null)
        {
            animator.SetTrigger("Idle");
        }
    }

    public void Flip(bool facingLeft)
    {
        if (hero.modelRoot != null)
            hero.modelRoot.localScale = new Vector3(facingLeft ? 1 : -1, 1, 1);
    }

    // Animation Event methods (called by Unity Animation Events)
    public void OnAttackAnimationComplete()
    {
        attackAnimationComplete = true;
    }

    public void OnAttackHit()
    {
        // Delegate to combat system
        var combat = GetComponent<HeroCombat>();
        if (combat != null)
        {
            combat.OnAttackHit();
        }
    }

    public void OnHitAnimationComplete()
    {
        hitAnimationComplete = true;
    }

    public IEnumerator PlayHitAnimation()
    {
        hitAnimationComplete = false;

        if (animator != null)
        {
            string hitTrigger = hero.teamId == 0 ? "HitLeft" : "HitRight";
            animator.SetTrigger(hitTrigger);
        }

        // Wait for animation to complete
        float timeWaited = 0f;
        while (!hitAnimationComplete && timeWaited < hitAnimDuration * 2f)
        {
            yield return null;
            timeWaited += Time.deltaTime;
        }

        // Transition back to Idle
        TriggerIdle();
    }
}
