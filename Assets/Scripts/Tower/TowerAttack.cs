using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TowerAttack : MonoBehaviour
{
    [SerializeField] BuffType thisBuffType;
    [SerializeField] SphereCollider attackRange;
    [SerializeField] GameObject Gobj;
    [SerializeField] float damage;
    [SerializeField] float dist;
    [SerializeField] float intervalTime;

    private void Start()
    {
        attackRange.radius = dist;
    }

    private void OnTriggerStay(Collider other)
    {
        if(other.gameObject.CompareTag("Enemy"))
        {
            other.gameObject.GetComponent<Enemy>().OnDamagedFromTower(thisBuffType, damage, intervalTime);
        }
    }
}
