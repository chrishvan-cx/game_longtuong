using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Scene Names")]
    public string homeSceneName = "HomeScene";
    public string mapSceneName = "MapScene";
    public string battleSceneName = "BattleScene";

    [Header("Battle Data")]
    public int selectedEnemyLevel = 1;
    public string selectedEnemyName = "";
    public string lastOpenedAreaName = ""; // Track which area was opened
    
    [Header("Battle Rewards (Set by NPC)")]
    public int battleExpReward = 100;
    public int battleGoldReward = 50;
    public List<ItemReward> battleItemRewards = new List<ItemReward>();
    
    [Header("Enemy Team (Set by NPC)")]
    public List<HeroData> battleEnemyTeam = new List<HeroData>();

    void Awake()
    {
        // Singleton pattern with scene persistence
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void LoadHomeScene()
    {
        ResetBattleState();
        SceneManager.LoadScene(homeSceneName);
    }

    public void LoadMapScene()
    {
        SceneManager.LoadScene(mapSceneName);
    }

    public void SetBattleRewards(int exp, int gold, List<ItemReward> items)
    {
        battleExpReward = exp;
        battleGoldReward = gold;
        battleItemRewards = items != null ? new List<ItemReward>(items) : new List<ItemReward>();
    }
    
    public void SetEnemyTeam(List<HeroData> enemies)
    {
        battleEnemyTeam = enemies != null ? new List<HeroData>(enemies) : new List<HeroData>();
    }

    public void LoadBattleScene(string enemyName, int enemyLevel)
    {
        selectedEnemyName = enemyName;
        selectedEnemyLevel = enemyLevel;
        SceneManager.LoadScene(battleSceneName);
    }

    public void ReturnToMapFromBattle()
    {
        ResetBattleState();
        SceneManager.LoadScene(mapSceneName);
    }

    private void ResetBattleState()
    {
        // Clear battle data
        selectedEnemyName = "";
        selectedEnemyLevel = 1;
        
        // Clear rewards and enemy team
        battleExpReward = 0;
        battleGoldReward = 0;
        battleItemRewards.Clear();
        battleEnemyTeam.Clear();
        
        // Stop all coroutines to prevent any lingering battle logic
        StopAllCoroutines();
    }

    public void QuitGame()
    {
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #else
        Application.Quit();
        #endif
    }
}
