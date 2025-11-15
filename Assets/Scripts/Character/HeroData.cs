using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[CreateAssetMenu(fileName = "HeroData", menuName = "TurnBased/Hero Data")]
public class HeroData : ScriptableObject
{
    public string heroName;
    public int level = 1;
    public Sprite sprite;
    public RuntimeAnimatorController animatorController;
    [Header("Power Stats")]
    public Skill specialSkill;

    // Core
    public int maxHP, energy, speed;
    public int physDmg, magicDmg, pureDmg;
    public int physDef, magicDef, pureDef;

    // Accuracy & Crit
    public float accuracy, dodge, critChance, critDamage;

    // Secondary
    public float lifeSteal, blockChance, counterChance;

    [Header("Grid Position")]
    public HeroColumn heroRole = HeroColumn.MidLine; // Hero's permanent role/column type (never changes)
    public HeroColumn position = HeroColumn.MidLine; // Current deployment position (changes when deployed/removed)
    public int row = 1;

    // Column is automatically determined by position
    public int column => (int)position;
}