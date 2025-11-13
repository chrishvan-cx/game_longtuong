using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimationEvent : MonoBehaviour
{
    public HeroUnit heroUnit;
    private HeroAnimator heroAnimator;

    void Start()
    {
        // Get HeroAnimator component from heroUnit
        if (heroUnit != null)
        {
            heroAnimator = heroUnit.GetComponent<HeroAnimator>();
        }
    }

    public void OnAttackAnimationComplete()
    {
        if (heroAnimator != null)
            heroAnimator.OnAttackAnimationComplete();
    }

    public void OnHitAnimationComplete()
    {
        if (heroAnimator != null)
            heroAnimator.OnHitAnimationComplete();
    }
    
    public void OnAttackHit()
    {
        if (heroAnimator != null)
            heroAnimator.OnAttackHit();
    }
}
