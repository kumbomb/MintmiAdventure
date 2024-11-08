using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Weapon : EquipWeapon
{
    public WeaponSetInfo weaponSetInfo;
    //public WeaponData thisWeaponData = new WeaponData();
    public int maxAmmo = 0;
    public int currentAmmo = 0;

    public void OnEnable()
    {
        if(weaponSetInfo.attackType == AttackType.Range)
        {
            currentAmmo = maxAmmo;
        }
        else
        {
            weaponSetInfo.meleeArea.enabled = false;
        }
    }

    public void UseWeapon(Transform position = null)
    {
        if(weaponSetInfo.attackType == AttackType.Melee)
        {
            //StopCoroutine("Co_Swing");
            StartCoroutine(Co_Swing());
        }
        else if (weaponSetInfo.attackType == AttackType.Range)
        {
            switch (weaponSetInfo.weaponType)
            {
                case WeaponType.HandGun:
                case WeaponType.SubMachineGun:
                    currentAmmo--;
                    StartCoroutine(Co_Shot(position));
                    break;
            }
        }
    }
    
    IEnumerator Co_Swing()
    {
        yield return new WaitForSeconds(0.2f);

        weaponSetInfo.meleeArea.enabled = true;
        weaponSetInfo.trailEffect.enabled = true;

        yield return new WaitForSeconds(0.1f);
        weaponSetInfo.meleeArea.enabled = false;


        yield return new WaitForSeconds(0.3f);
        weaponSetInfo.trailEffect.enabled = false;
    }

    IEnumerator Co_Shot(Transform _bulletPos)
    {
        GameObject bulletObj = ObjectPool.instance.PopFromPool(weaponSetInfo.weaponType.ToString(), ObjectPool.instance.PlayerBulletPool);
        bulletObj.transform.position = _bulletPos.position;
        bulletObj.transform.rotation = _bulletPos.rotation;
        bulletObj.GetComponent<Bullet>().damage = (int)Random.Range(weaponSetInfo.minDamage, weaponSetInfo.MaxDamage);
        bulletObj.SetActive(true);
        Rigidbody bulletRig = bulletObj.GetComponent<Rigidbody>();
        bulletRig.velocity = _bulletPos.forward * 100f;

        yield return null;
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
