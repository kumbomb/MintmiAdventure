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

    readonly List<Enemy> targets = new List<Enemy>();
    float tickTimer;

    void Start()
    {
        if (attackRange != null)
            attackRange.radius = dist;
        tickTimer = intervalTime;
    }

    void OnEnable()
    {
        tickTimer = intervalTime;
        targets.Clear();
    }

    void Update()
    {
        if (targets.Count == 0)
            return;

        tickTimer -= Time.deltaTime;
        if (tickTimer > 0f)
            return;

        tickTimer = intervalTime;
        for (int i = targets.Count - 1; i >= 0; i--)
        {
            Enemy enemy = targets[i];
            if (enemy == null || !enemy.gameObject.activeInHierarchy)
            {
                targets.RemoveAt(i);
                continue;
            }

            enemy.OnDamagedFromTower(thisBuffType, damage, intervalTime);
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Enemy"))
            return;

        Enemy enemy = other.GetComponent<Enemy>();
        if (enemy == null || targets.Contains(enemy))
            return;

        targets.Add(enemy);
        enemy.OnDamagedFromTower(thisBuffType, damage, intervalTime);
        tickTimer = intervalTime;
    }

    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Enemy"))
            return;

        Enemy enemy = other.GetComponent<Enemy>();
        if (enemy == null)
            return;

        targets.Remove(enemy);
    }
}
