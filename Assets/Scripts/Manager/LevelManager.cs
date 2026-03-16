using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class LevelManager : MonoBehaviour
{
    public NavMeshSurface surface;
    public StageLevelData[] stageDataList;
    public Transform stagePos;

    [SerializeField] Transform[] MonsterRespawnPos;
    [SerializeField] GameObject NextStagePortal;

    readonly Dictionary<int, int> clusterSpawnCounts = new Dictionary<int, int>();
    readonly Dictionary<int, int> clusterKillCounts = new Dictionary<int, int>();
    readonly string[] enemyTypeStr = { "EnemyA", "EnemyB", "EnemyC", "EnemyBoss" };

    int activeStageIndex;
    int nextClusterId;
    int latestClusterId = -1;
    int regularKillCount;
    float spawnTimer;
    bool stageRunning;
    bool bossSpawned;
    bool bossKilled;

    public bool IsStageCleared => bossKilled;

    public void InitLevel()
    {
        if (surface == null)
            surface = GameObject.Find("NavMesh")?.GetComponent<NavMeshSurface>();

        GameObject spawnParent = GameObject.Find("SpawnPoint");
        if (spawnParent != null)
        {
            MonsterRespawnPos = new Transform[spawnParent.transform.childCount];
            for (int i = 0; i < spawnParent.transform.childCount; i++)
                MonsterRespawnPos[i] = spawnParent.transform.GetChild(i).transform;
        }

        stagePos = GameObject.Find("StageTile")?.transform;
        NextStagePortal = GameObject.Find("NextStagePortal");
        if (NextStagePortal != null)
            NextStagePortal.SetActive(false);

        RefreshSurface();
    }

    public void RefreshSurface()
    {
        surface?.BuildNavMesh();
    }

    public void ResetStageRuntime()
    {
        activeStageIndex = 0;
        nextClusterId = 0;
        latestClusterId = -1;
        regularKillCount = 0;
        spawnTimer = 0f;
        stageRunning = false;
        bossSpawned = false;
        bossKilled = false;
        clusterSpawnCounts.Clear();
        clusterKillCounts.Clear();
    }

    public void StartStage(int stageIndex)
    {
        InitLevel();
        ResetStageRuntime();
        EnsureDefaultStageData();
        activeStageIndex = Mathf.Clamp(stageIndex, 0, Mathf.Max(0, stageDataList.Length - 1));
        stageRunning = true;
        SpawnCluster();
    }

    public void TickStage()
    {
        if (!stageRunning || GameManager.instance.nowGameResultState != GameResultState.None)
            return;

        spawnTimer += Time.deltaTime;
        StageLevelData stageData = GetCurrentStageData();
        if (stageData == null)
            return;

        if (!bossSpawned && regularKillCount >= stageData.minimumKillsForBoss)
        {
            if (spawnTimer >= stageData.spawnInterval && HasLatestClusterReachedThreshold(stageData))
            {
                SpawnBoss();
                spawnTimer = 0f;
            }
            return;
        }

        if (bossSpawned)
            return;

        if (spawnTimer < stageData.spawnInterval)
            return;

        if (!HasLatestClusterReachedThreshold(stageData))
            return;

        SpawnCluster();
        spawnTimer = 0f;
    }

    public void NotifyEnemyKilled(MonsterType monsterType, int clusterId)
    {
        if (monsterType == MonsterType.EnemyBoss)
        {
            bossKilled = true;
            stageRunning = false;
            return;
        }

        regularKillCount++;
        if (clusterId >= 0 && clusterKillCounts.ContainsKey(clusterId))
            clusterKillCounts[clusterId]++;
    }

    public string GetStageStatusText(int stageIndex)
    {
        StageLevelData stageData = GetStageData(stageIndex);
        if (stageData == null)
            return "Stage : " + (stageIndex + 1);

        if (bossKilled)
            return "Stage " + (stageIndex + 1) + " : Clear";

        if (bossSpawned)
            return "Stage " + (stageIndex + 1) + " : Boss Battle";

        int remainToBoss = Mathf.Max(0, stageData.minimumKillsForBoss - regularKillCount);
        return "Stage " + (stageIndex + 1) + " : Boss in " + remainToBoss + " kills";
    }

    public void ShowNextStagePortal()
    {
        if (NextStagePortal != null)
            NextStagePortal.SetActive(true);
    }

    void EnsureDefaultStageData()
    {
        if (stageDataList != null && stageDataList.Length > 0)
            return;

        StageLevelData defaultStage = ScriptableObject.CreateInstance<StageLevelData>();
        defaultStage.stageNo = 1;
        defaultStage.minimumKillsForBoss = 18;
        defaultStage.spawnInterval = 3.5f;
        defaultStage.minClusterSize = 4;
        defaultStage.maxClusterSize = 6;
        defaultStage.clusterScatterRadius = 4f;
        defaultStage.nextClusterKillRatio = 0.8f;
        defaultStage.minEnemyTypeIndex = 0;
        defaultStage.maxEnemyTypeIndex = 2;
        defaultStage.monsterStageScaling = new MonsterStageScaling();
        stageDataList = new[] { defaultStage };
    }

    StageLevelData GetCurrentStageData()
    {
        return GetStageData(activeStageIndex);
    }

    StageLevelData GetStageData(int stageIndex)
    {
        if (stageDataList == null || stageDataList.Length == 0)
            return null;

        return stageDataList[Mathf.Clamp(stageIndex, 0, stageDataList.Length - 1)];
    }

    bool HasLatestClusterReachedThreshold(StageLevelData stageData)
    {
        if (latestClusterId < 0)
            return true;

        if (!clusterSpawnCounts.ContainsKey(latestClusterId))
            return true;

        int spawned = clusterSpawnCounts[latestClusterId];
        if (spawned <= 0)
            return true;

        int killed = clusterKillCounts.ContainsKey(latestClusterId) ? clusterKillCounts[latestClusterId] : 0;
        return (float)killed / spawned >= stageData.nextClusterKillRatio;
    }

    void SpawnCluster()
    {
        StageLevelData stageData = GetCurrentStageData();
        if (stageData == null || MonsterRespawnPos == null || MonsterRespawnPos.Length == 0)
            return;

        int minClusterSize = Mathf.Max(1, stageData.minClusterSize);
        int maxClusterSize = Mathf.Max(minClusterSize, stageData.maxClusterSize);
        int minEnemyIndex = Mathf.Clamp(stageData.minEnemyTypeIndex, 0, 2);
        int maxEnemyIndex = Mathf.Clamp(stageData.maxEnemyTypeIndex, minEnemyIndex, 2);
        int clusterSize = Random.Range(minClusterSize, maxClusterSize + 1);
        int spawnAnchorIndex = GetRegularSpawnIndex();
        Vector3 anchorPosition = MonsterRespawnPos[spawnAnchorIndex].position;
        int clusterId = nextClusterId++;
        latestClusterId = clusterId;
        clusterSpawnCounts[clusterId] = 0;
        clusterKillCounts[clusterId] = 0;

        for (int i = 0; i < clusterSize; i++)
        {
            int enemyTypeIndex = Random.Range(minEnemyIndex, maxEnemyIndex + 1);
            Vector3 spawnPosition = GetClusterSpawnPosition(anchorPosition, stageData.clusterScatterRadius);
            SpawnEnemy(enemyTypeStr[enemyTypeIndex], spawnPosition, clusterId, stageData.monsterStageScaling);
        }
    }

    void SpawnBoss()
    {
        if (MonsterRespawnPos == null || MonsterRespawnPos.Length == 0)
            return;

        int bossSpawnIndex = MonsterRespawnPos.Length > 1 ? MonsterRespawnPos.Length - 1 : 0;
        StageLevelData stageData = GetCurrentStageData();
        MonsterStageScaling scaling = stageData != null ? stageData.monsterStageScaling : new MonsterStageScaling();
        SpawnEnemy(enemyTypeStr[3], MonsterRespawnPos[bossSpawnIndex].position, -1, scaling);
        bossSpawned = true;
    }

    void SpawnEnemy(string poolName, Vector3 spawnPosition, int clusterId, MonsterStageScaling stageScaling)
    {
        GameObject monster = ObjectPool.instance.PopFromPool(poolName, ObjectPool.instance.MonsterPool);
        monster.transform.position = spawnPosition;
        monster.SetActive(true);
        Enemy enemy = monster.GetComponent<Enemy>();
        enemy.ConfigureStats(stageScaling);
        enemy.ResetEnemy();
        enemy.SetSpawnContext(clusterId);
        GameManager.instance.CreateAddMonster(1);

        if (clusterId >= 0)
            clusterSpawnCounts[clusterId]++;
    }

    int GetRegularSpawnIndex()
    {
        if (MonsterRespawnPos.Length <= 1)
            return 0;

        return Random.Range(0, MonsterRespawnPos.Length - 1);
    }

    Vector3 GetClusterSpawnPosition(Vector3 anchorPosition, float scatterRadius)
    {
        Vector2 offset2D = Random.insideUnitCircle * scatterRadius;
        Vector3 candidate = anchorPosition + new Vector3(offset2D.x, 0f, offset2D.y);

        if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, scatterRadius + 2f, NavMesh.AllAreas))
            return hit.position;

        return candidate;
    }
}
