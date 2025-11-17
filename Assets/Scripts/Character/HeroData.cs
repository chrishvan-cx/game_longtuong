using System.Collections;
using System.Collections.Generic;
using UnityEngine;


/// <summary>
/// Runtime hero data - combines visual data from assets with stats from server
/// This is NOT a ScriptableObject - it's created at runtime
/// </summary>
[System.Serializable]
public class HeroData : ScriptableObject
{
    [Header("Identity")]
    public string heroId;
    public string heroName;
    public int level = 1;

    [Header("Visuals (from HeroVisualData)")]
    public Sprite sprite;
    public RuntimeAnimatorController animatorController;

    [Header("Stats (from Server/JSON)")]
    public int maxHP, energy, speed;
    public int physDmg, magicDmg, pureDmg;
    public int physDef, magicDef, pureDef;
    public float accuracy, dodge, critChance, critDamage;
    public float lifeSteal, blockChance, counterChance;

    [Header("Skills (loaded from server)")]
    public Skill specialSkill;

    [Header("Grid Position")]
    public HeroColumn heroRole = HeroColumn.MidLine; // From server
    public int row = 0; // 0 = undeployed

    public int column => (int)heroRole;

    /// <summary>
    /// Create HeroData from server stats + visual data
    /// </summary>
    public static HeroData Create(HeroStats stats, HeroVisualData visual)
    {
        HeroData hero = new HeroData();

        // Identity
        hero.heroId = stats.heroId;
        hero.heroName = stats.heroName;
        hero.level = stats.level;

        // Visuals
        if (visual != null)
        {
            hero.sprite = visual.sprite;
            hero.animatorController = visual.animatorController;
        }

        // Stats from server
        hero.maxHP = stats.maxHP;
        hero.energy = stats.energy;
        hero.speed = stats.speed;
        hero.physDmg = stats.physicalDmg;
        hero.magicDmg = stats.magicDmg;
        hero.pureDmg = stats.pureDmg;
        hero.physDef = stats.physicalDef;
        hero.magicDef = stats.magicDef;
        hero.pureDef = stats.pureDef;
        hero.accuracy = stats.accuracy;
        hero.dodge = stats.dodge;
        hero.critChance = stats.critChance;
        hero.critDamage = stats.critDamage;
        hero.lifeSteal = stats.lifeSteal;
        hero.blockChance = stats.blockChance;
        hero.counterChance = stats.counterChance;

        // Position
        hero.heroRole = stats.GetPosition();
        hero.row = 0; // Undeployed by default

        // Load skill
        if (!string.IsNullOrEmpty(stats.specialSkillId))
        {
            hero.specialSkill = Resources.Load<Skill>($"Skills/{stats.specialSkillId}");
        }

        return hero;
    }
}