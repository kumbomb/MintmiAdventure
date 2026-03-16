using UnityEngine;

[CreateAssetMenu(menuName = "Game Data/Weapon Stats")]
public class WeaponStatData : ScriptableObject
{
    public WeaponType weaponType;
    public AttackType attackType;
    public WeaponBehaviour weaponBehaviour;
    public int minDamage = 5;
    public int maxDamage = 6;
    public float rate = 0.5f;
    public float detectRadius = 8f;
    public float atkRadius = 1f;
    public float swingWindup = 0.2f;
    public float swingActive = 0.1f;
    public float swingRecover = 0.3f;
    public float bulletSpeed = 100f;

    public void ApplyTo(ref WeaponSetInfo weaponSetInfo)
    {
        weaponSetInfo.attackType = attackType;
        weaponSetInfo._type = weaponType;
        weaponSetInfo.weaponBehaviour = weaponBehaviour;
        weaponSetInfo.minDamage = minDamage;
        weaponSetInfo.maxDamage = maxDamage;
        weaponSetInfo.rate = rate;
        weaponSetInfo.detectRadius = detectRadius;
        weaponSetInfo.atkRadius = atkRadius;
    }
}
