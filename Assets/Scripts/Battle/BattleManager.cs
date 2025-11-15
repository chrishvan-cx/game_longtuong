using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class BattleManager : MonoBehaviour
{
    public static BattleManager Instance;

    private bool battleEnded = false;
    private int round = 0;

    [Header("Prefabs & Positions")]
    public HeroUnit heroPrefab;
    public Transform[] teamASlots;
    public Transform[] teamBSlots;

    // Team A (player) comes from PlayerData
    // Team B (enemies) comes from GameManager via NPC

    [Header("Battle Timing")]
    [SerializeField] private float delayAfterAction = 0.5f; // Delay after each hero's attack completes

    [Header("Battle Result")]
    public BattleResultPanel resultPanel;

    [HideInInspector] public List<HeroUnit> teamA = new List<HeroUnit>();
    [HideInInspector] public List<HeroUnit> teamB = new List<HeroUnit>();

    void Awake() => Instance = this;

    void Start()
    {
        Application.targetFrameRate = 60;

        // Load enemy team from GameManager (set by NPC)
        if (GameManager.Instance != null && GameManager.Instance.battleEnemyTeam.Count > 0)
        {
            LoadEnemyTeamFromGameManager();
        }

        SpawnTeams();
        StartCoroutine(BattleLoop());
    }

    private void LoadEnemyTeamFromGameManager()
    {
        teamB.Clear();
        // We'll spawn these in SpawnTeams, just need to prepare the data
        // Store in a temporary list to use during spawning
    }

    void SpawnTeams()
    {
        // Team A - use deployed heroes from formation (position != None)
        List<HeroData> playerData = (PlayerData.Instance != null)
            ? PlayerData.Instance.GetDeployedHeroes()
            : new List<HeroData>(); // fallback to empty if testing

        for (int i = 0; i < playerData.Count && i < teamASlots.Length; i++)
        {
            if (teamASlots[i] != null)
            {
                teamASlots[i].localScale = new Vector3(0.01f, 0.01f, 0.01f);
                SetSlotPosition(teamASlots[i], playerData[i].position, playerData[i].row, "TeamLeft");
            }

            Vector3 spawnPos = teamASlots[i].position;
            // Set Z based on row for proper sorting (lower row = more forward = lower Z)
            spawnPos.z = GetZPositionForRow(playerData[i].row);
            var hero = Instantiate(heroPrefab, spawnPos, Quaternion.identity);
            hero.Setup(playerData[i], 0);
            hero.Flip(false);
            teamA.Add(hero);
        }

        // Team B - use enemy team from GameManager if available
        List<HeroData> enemyData = (GameManager.Instance != null && GameManager.Instance.battleEnemyTeam.Count > 0)
            ? GameManager.Instance.battleEnemyTeam
            : new List<HeroData>(); // fallback to empty if testing

        for (int i = 0; i < enemyData.Count && i < teamBSlots.Length; i++)
        {
            if (teamBSlots[i] != null)
            {
                teamBSlots[i].localScale = new Vector3(0.01f, 0.01f, 0.01f);
                SetSlotPosition(teamBSlots[i], enemyData[i].position, enemyData[i].row, "TeamRight");
            }
            Vector3 spawnPos = teamBSlots[i].position;
            // Set Z based on row for proper sorting (lower row = more forward = lower Z)
            spawnPos.z = GetZPositionForRow(enemyData[i].row);
            var hero = Instantiate(heroPrefab, spawnPos, Quaternion.identity);
            hero.Setup(enemyData[i], 1);
            hero.Flip(true);
            teamB.Add(hero);
        }
    }

    private float GetZPositionForRow(int row)
    {
        // Lower row (row 1) should be in front (lower Z)
        // Higher row (row 3) should be behind (higher Z)
        switch (row)
        {
            case 1: return -1f;  // Front row (bottom)
            case 2: return 0f;   // Middle row
            case 3: return 1f;   // Back row (top)
            default: return 0f;
        }
    }

    private void SetSlotPosition(Transform slot, HeroColumn column, int row, string team)
    {
        float posX = 0, posY = 0;

        // Calculate X position based on column and team
        if (team == "TeamLeft")
        {
            switch (column)
            {
                case HeroColumn.FrontLine:
                    posX = -3f;
                    break;
                case HeroColumn.MidLine:
                    posX = -4.5f;
                    break;
                case HeroColumn.BackLine:
                    posX = -6f;
                    break;
            }
        }
        else if (team == "TeamRight")
        {
            switch (column)
            {
                case HeroColumn.FrontLine:
                    posX = 3f;
                    break;
                case HeroColumn.MidLine:
                    posX = 4.5f;
                    break;
                case HeroColumn.BackLine:
                    posX = 6f;
                    break;
            }
        }

        // Calculate Y position based on row
        switch (row)
        {
            case 1:
                posY = -0.5f;  // Bottom row
                break;
            case 2:
                posY = 1f;     // Middle row
                break;
            case 3:
                posY = 2.5f;   // Top row
                break;
            default:
                posY = 1f;     // Default to middle
                break;
        }

        slot.position = new Vector3(posX, posY, 0);
        slot.localScale = new Vector3(0.01f, 0.01f, 0.01f);
        BoxCollider box = slot.GetComponent<BoxCollider>();
        if (box == null)
        {
            box = slot.gameObject.AddComponent<BoxCollider>();
            box.size = new Vector3(100, 100, 1);
        }
        else
        {
            box.size = new Vector3(100, 100, 1);
        }
    }

    private IEnumerator BattleLoop()
    {
        // Step 1: Start the battle
        round = 0;
        foreach (var hero in teamA)
        {
            if (hero != null)
            {
                hero.h_round = 0;
                hero.is_turn = false;
            }
        }
        foreach (var hero in teamB)
        {
            if (hero != null)
            {
                hero.h_round = 0;
                hero.is_turn = false;
            }
        }

        yield return new WaitForSeconds(1f);

        // Main battle loop
        while (!battleEnded)
        {
            // Check victory conditions
            List<HeroUnit> aliveA = teamA.FindAll(h => h != null && h.isAlive);
            List<HeroUnit> aliveB = teamB.FindAll(h => h != null && h.isAlive);

            if (aliveA.Count == 0)
            {
                EndBattle(1);
                break;
            }
            if (aliveB.Count == 0)
            {
                EndBattle(0);
                break;
            }

            // Step 2: Start Round
            round += 1;

            // Step 3-4: Pick and execute each hero's turn until all have acted this round
            while (true)
            {
                if (battleEnded) break;

                // Step 3: Pick next hero to act
                HeroUnit actor = PickNextActor();
                if (actor == null) break; // Step 5: All heroes' h_round == round, round ends

                // Check if battle ended (a team was eliminated)
                if (battleEnded) break;

                // Set turn state
                actor.is_turn = true;
                actor.h_round = round;

                // Execute turn
                yield return actor.TakeTurn();

                // Reset turn state
                actor.is_turn = false;

                // Check victory after each action
                aliveA = teamA.FindAll(h => h != null && h.isAlive);
                aliveB = teamB.FindAll(h => h != null && h.isAlive);
                if (aliveA.Count == 0)
                {
                    EndBattle(1);
                    yield break; // Exit coroutine immediately
                }
                if (aliveB.Count == 0)
                {
                    EndBattle(0);
                    yield break; // Exit coroutine immediately
                }

                // Step 4: Wait after hero's attack - but check if battle ended first
                if (!battleEnded)
                {
                    yield return new WaitForSeconds(delayAfterAction);
                }
            }
        }
    }

    private HeroUnit PickNextActor()
    {
        // Get all alive heroes from both teams
        List<HeroUnit> allHeroes = new List<HeroUnit>();
        allHeroes.AddRange(teamA.FindAll(h => h != null && h.isAlive));
        allHeroes.AddRange(teamB.FindAll(h => h != null && h.isAlive));

        // Filter heroes who haven't acted this round (h_round < round)
        var candidates = allHeroes.FindAll(h => h.h_round < round);

        if (candidates.Count == 0)
            return null;

        // Pick highest speed hero (with stable tiebreaker)
        candidates.Sort((a, b) =>
        {
            int speedCompare = b.data.speed.CompareTo(a.data.speed);
            if (speedCompare != 0) return speedCompare;
            // Tiebreaker: use hero name for deterministic order
            return string.Compare(a.data.heroName, b.data.heroName);
        });

        return candidates[0];
    }
    // No longer used by the main loop; kept for potential external use
    private HeroUnit GetRandomAliveHero(List<HeroUnit> team)
    {
        List<HeroUnit> alive = team.FindAll(h => h != null && h.isAlive);
        if (alive.Count == 0) return null;
        return alive[Random.Range(0, alive.Count)];
    }

    private void ResetAllHeroesToIdle()
    {
        // Intentionally left empty — heroes manage their own states now.
    }

    public bool IsBattleEnded()
    {
        return battleEnded;
    }

    private void EndBattle(int winningTeam)
    {
        battleEnded = true;

        // Show result panel
        if (resultPanel != null)
        {
            if (winningTeam == 0) // Team A wins (player team)
            {
                // Increase map level (unlocks new NPCs)
                if (QuestManager.Instance != null)
                {
                    QuestManager.Instance.OnBattleWon();
                }

                // Get rewards from GameManager (set by NPC)
                if (GameManager.Instance != null)
                {
                    int expGained = GameManager.Instance.battleExpReward;
                    int goldGained = GameManager.Instance.battleGoldReward;
                    List<ItemReward> itemRewards = GameManager.Instance.battleItemRewards;

                    // If testing directly from Battle Scene, use default rewards
                    if (expGained == 0 && goldGained == 0 && itemRewards.Count == 0)
                    {
                        expGained = 100;
                        goldGained = 50;
                    }

                    resultPanel.ShowVictory(expGained, goldGained, itemRewards);
                }
                else
                {
                    resultPanel.ShowVictory(0, 0, new List<ItemReward>());
                }
            }
            else // Team B wins (enemy team)
            {
                resultPanel.ShowDefeat();
            }
        }
    }

    public void OnHeroClicked(HeroUnit clickedHero)
    {
        // Implementation for hero clicking if needed
    }
}