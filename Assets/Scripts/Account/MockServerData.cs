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
    /// Loads hero stats from mock_hero_stats.json - position/row from stats, not formation
    /// </summary>
    public static List<HeroData> LoadOwnedHeroes(List<MockHeroOwned> ownedHeroes)
    {
        List<HeroData> heroList = new List<HeroData>();

        foreach (var entry in ownedHeroes)
        {
            // Try to load HeroData asset from Resources/Heroes folder
            HeroData hero = Resources.Load<HeroData>($"Heroes/{entry.heroId}");

            if (hero == null)
            {
                Debug.LogWarning($"Hero not found: {entry.heroId}");
                continue;
            }

            // Load stats from mock server (includes role/position from stats file)
            HeroStats stats = GetHeroStats(entry.heroId);
            if (stats != null)
            {
                // Apply stats from mock server to HeroData
                ApplyStatsToHeroData(hero, stats);
            }
            else
            {
                Debug.LogWarning($"Stats not found for hero: {entry.heroId}");
            }

            heroList.Add(hero);
        }

        return heroList;
    }

    /// <summary>
    /// Apply formation deployment to heroes (set position/row from formation data)
    /// </summary>
    public static void ApplyFormationData(List<HeroData> heroes, List<MockHeroEntry> formation)
    {
        foreach (var formationEntry in formation)
        {
            // Find the hero in the heroes list
            HeroData hero = heroes.Find(h => h.name.Contains(formationEntry.heroId));
            if (hero != null)
            {
                // Apply formation position/row
                hero.position = ParsePosition(formationEntry.position);
                hero.row = formationEntry.row;
            }
            else
            {
                Debug.LogWarning($"Hero in formation not found in owned heroes: {formationEntry.heroId}");
            }
        }
    }

    /// <summary>
    /// Convert mock data to actual HeroData list (legacy method for compatibility)
    /// Loads hero stats from mock_hero_stats.json and applies to HeroData
    /// </summary>
    public static List<HeroData> ConvertToHeroDataList(List<MockHeroEntry> mockTeam)
    {
        List<HeroData> heroList = new List<HeroData>();

        foreach (var entry in mockTeam)
        {
            // Try to load HeroData asset from Resources/Heroes folder
            HeroData hero = Resources.Load<HeroData>($"Heroes/{entry.heroId}");

            if (hero == null)
            {
                Debug.LogWarning($"Hero not found: {entry.heroId}");
                continue;
            }

            // Load stats from mock server
            HeroStats stats = GetHeroStats(entry.heroId);
            if (stats != null)
            {
                // Apply stats from mock server to HeroData
                ApplyStatsToHeroData(hero, stats);
            }
            else
            {
                // Fallback: Use position/row from team data if stats not found
                HeroColumn parsedPosition = ParsePosition(entry.position);
                hero.heroRole = parsedPosition; // Set permanent role
                hero.position = parsedPosition; // Set current position
                hero.row = entry.row;
            }

            heroList.Add(hero);
        }

        return heroList;
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

        // Apply position (convert string to enum)
        HeroColumn parsedPosition = stats.GetPosition();
        heroData.heroRole = parsedPosition; // Set permanent role from stats
        heroData.position = parsedPosition; // Set current position (undeployed state)
        heroData.row = 0; // Undeployed by default (formation data will override)

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
                Debug.LogWarning($"Skill not founds: {stats.specialSkillId}");
            }
        }
    }

    // ═══════════════════════════════════════════════════════════
    // ENEMY TEAM LOADING
    // ═══════════════════════════════════════════════════════════

    [System.Serializable]
    public class EnemyTeamData
    {
        public List<MockHeroEntry> team;
    }

    /// <summary>
    /// Load enemy team by ID from mock_enemy_team.json
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
            // Parse the JSON manually to get specific team
            string json = jsonFile.text;

            // Extract the specific team from JSON
            // Format: { "enermy_1_1": [...], "enermy_1_2": [...] }
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
            EnemyTeamData teamData = JsonUtility.FromJson<EnemyTeamData>(wrappedJson);

            // Convert to HeroData list
            List<HeroData> enemyTeam = ConvertToHeroDataList(teamData.team);
            return enemyTeam;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to load enemy team '{teamId}': {e.Message}");
            return new List<HeroData>();
        }
    }
}
