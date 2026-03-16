using UnityEngine;

[CreateAssetMenu(menuName = "Game Data/Monster Stats")]
public class MonsterStatData : ScriptableObject
{
    public MonsterType monsterType;
    public int maxHealth = 30;
    public float moveSpeed = 10f;
    public float acceleration = 25f;
    public float angularSpeed = 360f;
    public float attackRange = 5f;
    public float meleeWindup = 0.5f;
    public float meleeActiveTime = 1f;
    public float meleeRecovery = 1f;
    public float dashWindup = 0.25f;
    public float dashDistance = 4.5f;
    public float dashDuration = 0.22f;
    public float dashRecovery = 0.8f;
    public float rangedWindup = 0.5f;
    public float rangedRecovery = 2f;
    public float projectileSpeed = 20f;
    public float hudTopPadding = 0.8f;

    public MonsterRuntimeStats CreateRuntimeStats(MonsterStageScaling scaling)
    {
        MonsterRuntimeStats stats = new MonsterRuntimeStats
        {
            maxHealth = maxHealth,
            moveSpeed = moveSpeed,
            acceleration = acceleration,
            angularSpeed = angularSpeed,
            attackRange = attackRange,
            meleeWindup = meleeWindup,
            meleeActiveTime = meleeActiveTime,
            meleeRecovery = meleeRecovery,
            dashWindup = dashWindup,
            dashDistance = dashDistance,
            dashDuration = dashDuration,
            dashRecovery = dashRecovery,
            rangedWindup = rangedWindup,
            rangedRecovery = rangedRecovery,
            projectileSpeed = projectileSpeed,
            hudTopPadding = hudTopPadding,
        };
        stats.ApplyScaling(scaling);
        return stats;
    }
}
