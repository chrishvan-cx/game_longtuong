using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum SkillType // Used in: Skill.cs, HeroSkillSystem.cs
{
    Meteor,      // Falls from above to ground
    Projectile   // Flies from caster to target
}

public enum DamageType
{
    Physical,
    Magical,
    Pure
}

public enum HeroState
{
    Idle,
    Acting,
    Dead
}

public enum TeamType
{
    TeamA,
    TeamB
}

public enum HeroColumn
{
    FrontLine = 1,  // Column 1
    MidLine = 2,    // Column 2
    BackLine = 3    // Column 3
}
