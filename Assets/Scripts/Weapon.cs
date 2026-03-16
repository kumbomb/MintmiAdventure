using System.Collections;
using UnityEngine;

public class Weapon : EquipWeapon
{
    [SerializeField] WeaponStatData weaponData;
    public WeaponSetInfo weaponSetInfo;
    public int maxAmmo = 0;
    public int currentAmmo = 0;

    WaitForSeconds swingWindup;
    WaitForSeconds swingActive;
    WaitForSeconds swingRecover;
    float bulletSpeed = 100f;

    void Awake()
    {
        ApplyWeaponData();
    }

    void OnEnable()
    {
        ApplyWeaponData();
        if (weaponSetInfo.attackType == AttackType.Melee && weaponSetInfo.meleeArea != null)
            weaponSetInfo.meleeArea.enabled = false;
    }

    void ApplyWeaponData()
    {
        if (weaponData != null)
            weaponData.ApplyTo(ref weaponSetInfo);

        float windup = weaponData != null ? weaponData.swingWindup : 0.2f;
        float active = weaponData != null ? weaponData.swingActive : 0.1f;
        float recover = weaponData != null ? weaponData.swingRecover : 0.3f;
        bulletSpeed = weaponData != null ? weaponData.bulletSpeed : 100f;
        swingWindup = new WaitForSeconds(windup);
        swingActive = new WaitForSeconds(active);
        swingRecover = new WaitForSeconds(recover);
    }

    public void UseWeapon(Transform position = null)
    {
        if (weaponSetInfo.attackType == AttackType.Melee)
        {
            StartCoroutine(Co_Swing());
            return;
        }

        if (weaponSetInfo.attackType != AttackType.Range || position == null)
            return;

        switch (weaponSetInfo.weaponType)
        {
            case WeaponType.HandGun:
            case WeaponType.SubMachineGun:
            case WeaponType.HandShotGun:
            case WeaponType.Shotgun:
                FireBullet(position);
                break;
        }
    }

    IEnumerator Co_Swing()
    {
        yield return swingWindup;
        if (weaponSetInfo.meleeArea != null)
            weaponSetInfo.meleeArea.enabled = true;
        if (weaponSetInfo.trailEffect != null)
            weaponSetInfo.trailEffect.enabled = true;

        yield return swingActive;
        if (weaponSetInfo.meleeArea != null)
            weaponSetInfo.meleeArea.enabled = false;

        yield return swingRecover;
        if (weaponSetInfo.trailEffect != null)
            weaponSetInfo.trailEffect.enabled = false;
    }

    void FireBullet(Transform bulletTransform)
    {
        GameObject bulletObj = ObjectPool.instance.PopFromPool(weaponSetInfo.weaponType.ToString(), ObjectPool.instance.PlayerBulletPool);
        bulletObj.transform.position = bulletTransform.position;
        bulletObj.transform.rotation = bulletTransform.rotation;
        Bullet bullet = bulletObj.GetComponent<Bullet>();
        bullet.damage = Random.Range(weaponSetInfo.minDamage, weaponSetInfo.MaxDamage);
        bulletObj.SetActive(true);
        Rigidbody bulletRig = bulletObj.GetComponent<Rigidbody>();
        bulletRig.linearVelocity = bulletTransform.forward * bulletSpeed;
    }

    public override ItemBehaviour ItemBehaviourID
    {
        get
        {
            return (
                weaponSetInfo.weaponBehaviour == WeaponBehaviour.TwoHands ||
                weaponSetInfo.weaponBehaviour == WeaponBehaviour.TwoHandsGun ||
                weaponSetInfo.weaponBehaviour == WeaponBehaviour.TwoHandsMagic
                ) ?
                ItemBehaviour.TwoHands : (
                weaponSetInfo.weaponBehaviour == WeaponBehaviour.Double ||
                weaponSetInfo.weaponBehaviour == WeaponBehaviour.DoubleGun
                ) ?
                ItemBehaviour.Double : (
                weaponSetInfo.weaponBehaviour == WeaponBehaviour.Single ||
                weaponSetInfo.weaponBehaviour == WeaponBehaviour.SingleGun ||
                weaponSetInfo.weaponBehaviour == WeaponBehaviour.SingleMagic
                ) ? ItemBehaviour.Single :
                ItemBehaviour.None;
        }
    }

    public int RetItemBehaviour()
    {
        return (int)weaponSetInfo.WeaponBehaviour;
    }

    public void StopAttack(Transform tf)
    {
    }
}
