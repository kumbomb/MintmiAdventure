using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class LevelManager : MonoBehaviour
{
    struct RuntimeStageConfig
    {
        public int stageNo;
        public int minimumKillsForBoss;
        public float spawnInterval;
        public int minClusterSize;
        public int maxClusterSize;
        public float clusterScatterRadius;
        public float nextClusterKillRatio;
        public int minEnemyTypeIndex;
        public int maxEnemyTypeIndex;
        public MonsterStageScaling monsterStageScaling;
    }

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
        activeStageIndex = Mathf.Max(0, stageIndex);
        stageRunning = true;
        SpawnCluster();
    }

    public void TickStage()
    {
        if (!stageRunning || GameManager.instance.nowGameResultState != GameResultState.None)
            return;

        spawnTimer += Time.deltaTime;
        RuntimeStageConfig stageConfig = GetCurrentStageConfig();

        if (!bossSpawned && regularKillCount >= stageConfig.minimumKillsForBoss)
        {
            if (spawnTimer >= stageConfig.spawnInterval && HasLatestClusterReachedThreshold(stageConfig))
            {
                SpawnBoss();
                spawnTimer = 0f;
            }
            return;
        }

        if (bossSpawned)
            return;

        if (spawnTimer < stageConfig.spawnInterval)
            return;

        if (!HasLatestClusterReachedThreshold(stageConfig))
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
        RuntimeStageConfig stageConfig = GetStageConfig(stageIndex);

        if (bossKilled)
            return "Stage " + (stageIndex + 1) + " : Clear";

        if (bossSpawned)
            return "Stage " + (stageIndex + 1) + " : Boss Battle";

        int remainToBoss = Mathf.Max(0, stageConfig.minimumKillsForBoss - regularKillCount);
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

    RuntimeStageConfig GetCurrentStageConfig()
    {
        return GetStageConfig(activeStageIndex);
    }

    RuntimeStageConfig GetStageConfig(int stageIndex)
    {
        EnsureDefaultStageData();

        int safeIndex = Mathf.Max(0, stageIndex);
        int baseIndex = Mathf.Clamp(safeIndex, 0, stageDataList.Length - 1);
        StageLevelData baseData = stageDataList[baseIndex];
        int overflowStageCount = Mathf.Max(0, safeIndex - (stageDataList.Length - 1));

        RuntimeStageConfig config = new RuntimeStageConfig
        {
            stageNo = safeIndex + 1,
            minimumKillsForBoss = baseData.minimumKillsForBoss,
            spawnInterval = baseData.spawnInterval,
            minClusterSize = baseData.minClusterSize,
            maxClusterSize = baseData.maxClusterSize,
            clusterScatterRadius = baseData.clusterScatterRadius,
            nextClusterKillRatio = baseData.nextClusterKillRatio,
            minEnemyTypeIndex = baseData.minEnemyTypeIndex,
            maxEnemyTypeIndex = baseData.maxEnemyTypeIndex,
            monsterStageScaling = baseData.monsterStageScaling,
        };

        // Stage 1/2 keeps authored values. After that, difficulty scales smoothly on the same map.
        for (int i = 0; i < overflowStageCount; i++)
        {
            config.minimumKillsForBoss = Mathf.CeilToInt(config.minimumKillsForBoss * 1.18f);
            config.minClusterSize = Mathf.Max(1, Mathf.CeilToInt(config.minClusterSize * 1.12f));
            config.maxClusterSize = Mathf.Max(config.minClusterSize, Mathf.CeilToInt(config.maxClusterSize * 1.14f));
            config.spawnInterval = Mathf.Max(2.1f, config.spawnInterval * 0.96f);
            config.clusterScatterRadius = Mathf.Min(6.5f, config.clusterScatterRadius + 0.15f);
            config.monsterStageScaling.healthPercent += 18f;
            config.monsterStageScaling.moveSpeedPercent += 6f;
            config.monsterStageScaling.attackPercent += 10f;
        }

        return config;
    }

    bool HasLatestClusterReachedThreshold(RuntimeStageConfig stageConfig)
    {
        if (latestClusterId < 0)
            return true;

        if (!clusterSpawnCounts.ContainsKey(latestClusterId))
            return true;

        int spawned = clusterSpawnCounts[latestClusterId];
        if (spawned <= 0)
            return true;

        int killed = clusterKillCounts.ContainsKey(latestClusterId) ? clusterKillCounts[latestClusterId] : 0;
        return (float)killed / spawned >= stageConfig.nextClusterKillRatio;
    }

    void SpawnCluster()
    {
        RuntimeStageConfig stageConfig = GetCurrentStageConfig();
        if (MonsterRespawnPos == null || MonsterRespawnPos.Length == 0)
            return;

        int minClusterSize = Mathf.Max(1, stageConfig.minClusterSize);
        int maxClusterSize = Mathf.Max(minClusterSize, stageConfig.maxClusterSize);
        int minEnemyIndex = Mathf.Clamp(stageConfig.minEnemyTypeIndex, 0, 2);
        int maxEnemyIndex = Mathf.Clamp(stageConfig.maxEnemyTypeIndex, minEnemyIndex, 2);
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
            Vector3 spawnPosition = GetClusterSpawnPosition(anchorPosition, stageConfig.clusterScatterRadius);
            SpawnEnemy(enemyTypeStr[enemyTypeIndex], spawnPosition, clusterId, stageConfig.monsterStageScaling);
        }
    }

    void SpawnBoss()
    {
        if (MonsterRespawnPos == null || MonsterRespawnPos.Length == 0)
            return;

        int bossSpawnIndex = MonsterRespawnPos.Length > 1 ? MonsterRespawnPos.Length - 1 : 0;
        RuntimeStageConfig stageConfig = GetCurrentStageConfig();
        SpawnEnemy(enemyTypeStr[3], MonsterRespawnPos[bossSpawnIndex].position, -1, stageConfig.monsterStageScaling);
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
