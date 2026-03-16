using UnityEngine;

[System.Serializable]
public struct MonsterStageScaling
{
    [Range(0f, 500f)] public float healthPercent;
    [Range(0f, 500f)] public float moveSpeedPercent;
    [Range(0f, 500f)] public float attackPercent;

    public float HealthMultiplier => 1f + (healthPercent * 0.01f);
    public float MoveSpeedMultiplier => 1f + (moveSpeedPercent * 0.01f);
    public float AttackMultiplier => 1f + (attackPercent * 0.01f);
}

[System.Serializable]
public struct MonsterRuntimeStats
{
    public int maxHealth;
    public float moveSpeed;
    public float acceleration;
    public float angularSpeed;
    public float attackRange;
    public float meleeWindup;
    public float meleeActiveTime;
    public float meleeRecovery;
    public float dashWindup;
    public float dashDistance;
    public float dashDuration;
    public float dashRecovery;
    public float rangedWindup;
    public float rangedRecovery;
    public float projectileSpeed;
    public float hudTopPadding;

    public void ApplyScaling(MonsterStageScaling scaling)
    {
        maxHealth = Mathf.Max(1, Mathf.RoundToInt(maxHealth * scaling.HealthMultiplier));
        moveSpeed *= scaling.MoveSpeedMultiplier;
        acceleration *= scaling.MoveSpeedMultiplier;
        angularSpeed *= scaling.MoveSpeedMultiplier;
        attackRange *= scaling.AttackMultiplier;
        dashDistance *= scaling.AttackMultiplier;
        projectileSpeed *= scaling.AttackMultiplier;
    }
}
