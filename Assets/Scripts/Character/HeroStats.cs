using UnityEngine;

/// <summary>
/// Hero stats data - should come from server to prevent hacking
/// In production, this is loaded from API, not stored in build
/// </summary>
[System.Serializable]
public class HeroStats
{
    [Header("Identity")]
    public string heroId;          // Must match HeroVisualData.heroId
    public string heroName;        // Display name
    public int level = 1;          // Hero level

    [Header("Core Stats")]
    public int maxHP = 200;
    public int energy = 50;
    public int speed = 100;

    [Header("Damage Stats")]
    public int physicalDmg = 100;
    public int magicDmg = 50;
    public int pureDmg = 10;

    [Header("Defense Stats")]
    public int physicalDef = 50;
    public int magicDef = 25;
    public int pureDef = 5;

    [Header("Advanced Stats")]
    public float accuracy = 0.9f;      // 90% hit chance
    public float dodge = 0.1f;         // 10% dodge
    public float critChance = 0.15f;   // 15% crit
    public float critDamage = 1.5f;    // 150% on crit

    [Header("Special Stats")]
    public float lifeSteal = 0f;       // % of damage as heal
    public float blockChance = 0f;     // % to block attack
    public float counterChance = 0f;   // % to counter attack

    [Header("Position")]
    public string position = "MidLine";  // String from JSON, converted to enum
    public int row = 1;

    // Get position as enum
    public HeroColumn GetPosition()
    {
        switch (position)
        {
            case "FrontLine": return HeroColumn.FrontLine;
            case "MidLine": return HeroColumn.MidLine;
            case "BackLine": return HeroColumn.BackLine;
            default:
                Debug.LogWarning($"Unknown position: {position}, using MidLine");
                return HeroColumn.MidLine;
        }
    }

    [Header("Skills")]
    public string specialSkillId;      // ID to load Skill asset

    // Computed property - get column from position enum
    public int column => (int)GetPosition();
}

/// <summary>
/// Response from server when fetching hero stats
/// </summary>
[System.Serializable]
public class HeroStatsResponse
{
    public HeroStats[] heroes;
}
