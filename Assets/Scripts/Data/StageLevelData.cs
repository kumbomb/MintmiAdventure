using UnityEngine;

[CreateAssetMenu(menuName = "Game Data/Stage Level Data")]
public class StageLevelData : ScriptableObject
{
    public int stageNo = 1;
    public int minimumKillsForBoss = 18;
    public float spawnInterval = 3.5f;
    public int minClusterSize = 4;
    public int maxClusterSize = 6;
    public float clusterScatterRadius = 4f;
    [Range(0.5f, 1f)] public float nextClusterKillRatio = 0.8f;
    [Range(0, 2)] public int minEnemyTypeIndex = 0;
    [Range(0, 2)] public int maxEnemyTypeIndex = 2;
    public MonsterStageScaling monsterStageScaling;
}
