using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Mock server data loader - simulates loading player data from server
/// In production, replace this with real API calls
/// </summary>
public static class MockServerData
{
    [System.Serializable]
    public class MockPlayerTeamData
    {
        public List<MockHeroOwned> playerHeroes;      // All heroes player owns
        public List<MockHeroEntry> playerFormation;   // Heroes deployed in formation
        public MockPlayerStats playerStats;
    }

    [System.Serializable]
    public class MockHeroOwned
    {
        public string heroId;       // Maps to HeroData asset name
    }

    [System.Serializable]
    public class MockHeroEntry
    {
        public string heroId;       // Maps to HeroData asset name
        public string position;     // "FrontLine", "MidLine", "BackLine"
        public int row;            // 1, 2, or 3
    }

    [System.Serializable]
    public class MockPlayerStats
    {
        public int level = 1;
        public int exp = 0;
        public int gold = 500;
    }

    /// <summary>
    /// Load mock player data from JSON file
    /// In production: Replace with API call to your backend
    /// </summary>
    public static MockPlayerTeamData LoadMockData()
    {
        // Load from Resources folder
        TextAsset jsonFile = Resources.Load<TextAsset>("MockData/mock_player_team");

        if (jsonFile == null)
        {
            Debug.LogWarning("Mock data file not found! Using default empty data.");
            return new MockPlayerTeamData
            {
                playerHeroes = new List<MockHeroOwned>(),
                playerFormation = new List<MockHeroEntry>(),
                playerStats = new MockPlayerStats()
            };
        }

        try
        {
            MockPlayerTeamData data = JsonUtility.FromJson<MockPlayerTeamData>(jsonFile.text);

            // Ensure lists are initialized
            if (data.playerHeroes == null)
                data.playerHeroes = new List<MockHeroOwned>();
            if (data.playerFormation == null)
                data.playerFormation = new List<MockHeroEntry>();
            if (data.playerStats == null)
                data.playerStats = new MockPlayerStats();

            return data;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to parse mock data: {e.Message}");
            return new MockPlayerTeamData
            {
                playerHeroes = new List<MockHeroOwned>(),
                playerFormation = new List<MockHeroEntry>(),
                playerStats = new MockPlayerStats()
            };
        }
    }

    /// <summary>
    /// Load all owned heroes (for sidebar display)
    /// Combines HeroVisualData + HeroStats
    /// </summary>
    public static List<HeroData> LoadOwnedHeroes(List<MockHeroOwned> ownedHeroes)
    {
        List<HeroData> heroList = new List<HeroData>();

        foreach (var entry in ownedHeroes)
        {
            // ✅ Load visual data from Resources
            HeroVisualData visualTemplate = Resources.Load<HeroVisualData>($"Heroes/{entry.heroId}");

            if (visualTemplate == null)
            {
                Debug.LogWarning($"Hero visual data not found: {entry.heroId}");
                continue;
            }

            // Create copy of visual data
            HeroVisualData visual = Object.Instantiate(visualTemplate);

            // Load stats from JSON
            HeroStats stats = GetHeroStats(entry.heroId);

            // Create HeroData
            HeroData hero = CreateHeroFromVisualAndStats(visual, stats);

            if (hero != null)
            {
                heroList.Add(hero);
            }
        }

        return heroList;
    }

    /// <summary>
    /// Apply formation deployment to heroes (set position/row from formation data)
    /// </summary>
    public static void ApplyFormationData(List<HeroData> heroes, List<MockHeroEntry> formation)
    {
        Debug.Log($"[FORMATION] Applying formation data to {heroes.Count} heroes");

        foreach (var formationEntry in formation)
        {
            // Find the hero in the heroes list by heroId
            HeroData hero = heroes.Find(h => h.heroId == formationEntry.heroId);

            if (hero != null)
            {
                // ✅ Apply formation row only (heroRole is set from stats, not formation)
                hero.row = formationEntry.row;

                Debug.Log($"[FORMATION] ✅ Deployed {hero.heroName} (Role: {hero.heroRole}) to Row {formationEntry.row}");
            }
            else
            {
                Debug.LogWarning($"[FORMATION] ❌ Hero NOT FOUND: {formationEntry.heroId}");
            }
        }

        // Debug: Print final state
        Debug.Log("[FORMATION] Final hero states:");
        foreach (var h in heroes)
        {
            Debug.Log($"[FORMATION] {h.heroName}: heroRole={h.heroRole}, row={h.row}");
        }
    }


    /// <summary>
    /// Map heroId to actual file name (if they differ)
    /// </summary>
    private static string GetHeroFileName(string heroId)
    {
        // Add mappings here if file names don't match heroIds
        Dictionary<string, string> idToFileName = new Dictionary<string, string>
        {
            // Example: { "hero_tienquan", "TienQuanHero" }
            // If file name matches ID, no need to add mapping
        };

        return idToFileName.ContainsKey(heroId) ? idToFileName[heroId] : heroId;
    }
    /// <summary>
    /// Convert mock enemy team data to HeroData list
    /// Uses HeroVisualData + HeroStats (from JSON)
    /// </summary>
    public static List<HeroData> ConvertToHeroDataList(List<MockHeroEntry> mockTeam)
    {
        List<HeroData> heroList = new List<HeroData>();

        foreach (var entry in mockTeam)
        {
            // ✅ Load visual data from Resources/Heroes folder
            HeroVisualData visualTemplate = Resources.Load<HeroVisualData>($"Heroes/{entry.heroId}");

            if (visualTemplate == null)
            {
                Debug.LogWarning($"Hero visual data not found: {entry.heroId}");
                continue;
            }

            // ✅ Create runtime HeroData (instantiate ScriptableObject to get a copy)
            HeroVisualData visual = Object.Instantiate(visualTemplate);

            // Load stats from mock server (JSON)
            HeroStats stats = GetHeroStats(entry.heroId);

            // ✅ Create new HeroData by combining visual + stats
            HeroData hero = CreateHeroFromVisualAndStats(visual, stats);

            if (hero != null)
            {
                // Override with formation data from mockTeam
                hero.row = entry.row;
                heroList.Add(hero);
            }
            else
            {
                Debug.LogWarning($"Failed to create hero: {entry.heroId}");
            }
        }

        return heroList;
    }

    /// <summary>
    /// Helper method to create HeroData from HeroVisualData + HeroStats
    /// </summary>
    private static HeroData CreateHeroFromVisualAndStats(HeroVisualData visual, HeroStats stats)
    {
        if (visual == null)
        {
            Debug.LogError("Visual data is null!");
            return null;
        }

        // ✅ Create new HeroData ScriptableObject instance
        HeroData hero = ScriptableObject.CreateInstance<HeroData>();
        // Below is for Production
        // HeroData hero = new HeroData();

        // Apply visual data
        hero.heroId = visual.heroId;
        hero.sprite = visual.sprite;
        hero.animatorController = visual.animatorController;

        // Apply stats if available
        if (stats != null)
        {
            ApplyStatsToHeroData(hero, stats);

            // Below is for Production
            // hero.heroName = stats.heroName;
            // hero.level = stats.level;
            // hero.maxHP = stats.maxHP;
            // hero.energy = stats.energy;
            // hero.speed = stats.speed;
            // hero.physDmg = stats.physicalDmg;
            // hero.magicDmg = stats.magicDmg;
            // hero.pureDmg = stats.pureDmg;
            // hero.physDef = stats.physicalDef;
            // hero.magicDef = stats.magicDef;
            // hero.pureDef = stats.pureDef;
            // hero.accuracy = stats.accuracy;
            // hero.dodge = stats.dodge;
            // hero.critChance = stats.critChance;
            // hero.critDamage = stats.critDamage;
            // hero.lifeSteal = stats.lifeSteal;
            // hero.blockChance = stats.blockChance;
            // hero.counterChance = stats.counterChance;
            // hero.heroRole = stats.GetPosition();
            // hero.row = 0;

            // // Load skill
            // if (!string.IsNullOrEmpty(stats.specialSkillId))
            // {
            //     hero.specialSkill = Resources.Load<Skill>($"Skills/{stats.specialSkillId}");
            // }
        }
        else
        {
            // Fallback: use default values
            Debug.LogWarning($"Stats not found for hero: {visual.heroId}, using defaults");
            hero.heroName = visual.heroId;
            hero.heroRole = HeroColumn.MidLine;
            hero.row = 0;
        }

        return hero;
    }



    /// <summary>
    /// Parse position string to enum
    /// </summary>
    private static HeroColumn ParsePosition(string positionStr)
    {
        switch (positionStr)
        {
            case "FrontLine":
                return HeroColumn.FrontLine;
            case "MidLine":
                return HeroColumn.MidLine;
            case "BackLine":
                return HeroColumn.BackLine;
            default:
                Debug.LogWarning($"Unknown position: {positionStr}, defaulting to MidLine");
                return HeroColumn.MidLine;
        }
    }

    /// <summary>
    /// Simulate async server call (for testing)
    /// In production: Replace with actual HTTP request
    /// </summary>
    public static void LoadFromServerAsync(System.Action<MockPlayerTeamData> onComplete)
    {
        // In a real implementation, you would do:
        // StartCoroutine(FetchFromAPI("https://yourapi.com/player/data", onComplete));

        // For now, load from local JSON
        MockPlayerTeamData data = LoadMockData();
        onComplete?.Invoke(data);
    }

    // ═══════════════════════════════════════════════════════════
    // HERO STATS LOADING
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Load all hero stats from mock JSON
    /// </summary>
    public static HeroStatsResponse LoadHeroStats()
    {
        TextAsset jsonFile = Resources.Load<TextAsset>("MockData/mock_hero_stats");

        if (jsonFile == null)
        {
            Debug.LogWarning("Hero stats file not found! Using empty data.");
            return new HeroStatsResponse { heroes = new HeroStats[0] };
        }

        try
        {
            HeroStatsResponse data = JsonUtility.FromJson<HeroStatsResponse>(jsonFile.text);
            return data;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to parse hero stats: {e.Message}");
            return new HeroStatsResponse { heroes = new HeroStats[0] };
        }
    }

    /// <summary>
    /// Get stats for a specific hero by ID
    /// </summary>
    public static HeroStats GetHeroStats(string heroId)
    {
        var allStats = LoadHeroStats();

        foreach (var stats in allStats.heroes)
        {
            if (stats.heroId == heroId)
                return stats;
        }

        Debug.LogWarning($"Hero stats not found for: {heroId}");
        return null;
    }

    /// <summary>
    /// Apply stats to HeroData (update existing HeroData with server stats)
    /// </summary>
    public static void ApplyStatsToHeroData(HeroData heroData, HeroStats stats)
    {
        if (heroData == null || stats == null) return;

        heroData.heroName = stats.heroName;
        heroData.level = stats.level;

        // Apply core stats
        heroData.maxHP = stats.maxHP;
        heroData.energy = stats.energy;
        heroData.speed = stats.speed;

        // Apply damage stats
        heroData.physDmg = stats.physicalDmg;
        heroData.magicDmg = stats.magicDmg;
        heroData.pureDmg = stats.pureDmg;

        // Apply defense stats
        heroData.physDef = stats.physicalDef;
        heroData.magicDef = stats.magicDef;
        heroData.pureDef = stats.pureDef;

        // Apply advanced stats
        heroData.accuracy = stats.accuracy;
        heroData.dodge = stats.dodge;
        heroData.critChance = stats.critChance;
        heroData.critDamage = stats.critDamage;

        // Apply special stats
        heroData.lifeSteal = stats.lifeSteal;
        heroData.blockChance = stats.blockChance;
        heroData.counterChance = stats.counterChance;

        // ✅ FIX: Set heroRole (permanent class) from stats
        HeroColumn parsedRole = stats.GetPosition();
        heroData.heroRole = parsedRole;

        // ✅ FIX: Initialize as UNDEPLOYED (formation will override if deployed)
        heroData.row = 0; // 0 = not deployed

        // Apply skill (if specified)
        if (!string.IsNullOrEmpty(stats.specialSkillId))
        {
            Skill skill = Resources.Load<Skill>($"Skills/{stats.specialSkillId}");
            if (skill != null)
            {
                heroData.specialSkill = skill;
            }
            else
            {
                Debug.LogWarning($"Skill not found: {stats.specialSkillId}");
            }
        }
    }
    // ═══════════════════════════════════════════════════════════
    // ENEMY TEAM LOADING
    // ═══════════════════════════════════════════════════════════
    [System.Serializable]
    public class EnemyFormationData
    {
        public string heroId;
        public int row;
        // heroRole is NOT needed here - comes from stats!
    }

    [System.Serializable]
    public class EnemyTeamWrapper
    {
        public List<EnemyFormationData> team;
    }

    [System.Serializable]
    public class EnemyTeamData
    {
        public List<MockHeroEntry> team;
    }

    /// <summary>
    /// Load enemy team by ID from mock_enemy_team.json
    /// Format: { "monster_map_1": [...], "monster_mountain_2": [...] }
    /// </summary>
    public static List<HeroData> LoadEnemyTeam(string teamId)
    {
        TextAsset jsonFile = Resources.Load<TextAsset>("MockData/mock_enemy_team");

        if (jsonFile == null)
        {
            Debug.LogWarning("Enemy team file not found!");
            return new List<HeroData>();
        }

        try
        {
            string json = jsonFile.text;

            // Find the team by ID
            string searchKey = $"\"{teamId}\"";
            int startIndex = json.IndexOf(searchKey);

            if (startIndex == -1)
            {
                Debug.LogWarning($"Enemy team '{teamId}' not found in JSON");
                return new List<HeroData>();
            }

            // Find the array for this team
            int arrayStart = json.IndexOf('[', startIndex);
            int arrayEnd = json.IndexOf(']', arrayStart);

            if (arrayStart == -1 || arrayEnd == -1)
            {
                Debug.LogError($"Failed to parse enemy team '{teamId}'");
                return new List<HeroData>();
            }

            // Extract the array content
            string arrayContent = json.Substring(arrayStart, arrayEnd - arrayStart + 1);

            // Wrap in a structure JsonUtility can parse
            string wrappedJson = $"{{\"team\":{arrayContent}}}";
            EnemyTeamWrapper teamData = JsonUtility.FromJson<EnemyTeamWrapper>(wrappedJson);

            // Convert to HeroData list
            List<HeroData> enemyTeam = ConvertEnemyFormationToHeroData(teamData.team);
            return enemyTeam;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to load enemy team '{teamId}': {e.Message}");
            return new List<HeroData>();
        }
    }

    /// <summary>
    /// Convert enemy formation data to HeroData list
    /// Loads stats from mock_hero_stats.json + visuals from Resources
    /// </summary>
    private static List<HeroData> ConvertEnemyFormationToHeroData(List<EnemyFormationData> formation)
    {
        List<HeroData> heroList = new List<HeroData>();

        foreach (var entry in formation)
        {
            // Load visual data from Resources
            HeroVisualData visualTemplate = Resources.Load<HeroVisualData>($"Heroes/{entry.heroId}");

            if (visualTemplate == null)
            {
                Debug.LogWarning($"Enemy visual data not found: {entry.heroId}");
                continue;
            }

            HeroVisualData visual = Object.Instantiate(visualTemplate);

            // Load stats from JSON (this includes heroRole!)
            HeroStats stats = GetHeroStats(entry.heroId);

            if (stats == null)
            {
                Debug.LogWarning($"Enemy stats not found: {entry.heroId}");
                continue;
            }

            // Create HeroData
            HeroData hero = CreateHeroFromVisualAndStats(visual, stats);

            if (hero != null)
            {
                // ✅ Only apply row from formation, heroRole comes from stats!
                hero.row = entry.row;
                heroList.Add(hero);

                Debug.Log($"[ENEMY] Loaded {hero.heroName} (Role: {hero.heroRole}, Row: {hero.row})");
            }
        }

        return heroList;
    }
}
