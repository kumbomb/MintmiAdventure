using UnityEngine;

[CreateAssetMenu(menuName = "Game Data/Player Stats")]
public class PlayerStatData : ScriptableObject
{
    public int maxHealth = 100;
    public float moveSpeed = 10f;
    public float survivalSkillCooldown = 12f;
    public float survivalSkillDuration = 2.5f;
    public float survivalSkillSpeedMultiplier = 1.25f;
    [Range(0f, 1f)] public float survivalSkillDamageReduction = 0.6f;
    public int survivalSkillHealAmount = 10;
}
