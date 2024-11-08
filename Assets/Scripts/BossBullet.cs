using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class BossBullet : Bullet
{
    public Transform target;
    [SerializeField] NavMeshAgent nav;
    private void Start()
    {
        nav.enabled = false;
        SetNav();
    }
    private void OnDisable()
    {
        remainTime = 0f;
    }
    public void SetNav()
    {
        nav.enabled = true;
    }
    void Update()
    {
        if (!nav.enabled)
            return;
        nav.SetDestination(target.position);
        remainTime += Time.deltaTime;
        if (remainTime >= remainMaxTime && this.gameObject.activeSelf)
        {
            //Destroy(this.gameObject);
            DelayToHide(0f);
            remainTime = 0f;
        }
    }
}
