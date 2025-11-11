using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimationEvent : MonoBehaviour
{
    public HeroUnit heroUnit;

    public void OnAttackAnimationComplete()
    {
        heroUnit.OnAttackAnimationComplete();
    }

    public void OnHitAnimationComplete()
    {
        heroUnit.OnHitAnimationComplete();
    }
    
    public void OnAttackHit()
    {
        heroUnit.OnAttackHit();
    }
}
