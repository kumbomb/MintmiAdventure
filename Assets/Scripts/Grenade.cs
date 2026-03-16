using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class Grenade : MonoBehaviour
{
    [SerializeField] GameObject meshObj;
    [SerializeField] GameObject particleObj;
    [SerializeField] Rigidbody rigid;
    [SerializeField] TowerType thisType;
    [SerializeField] float coolTime; 

    void Start()
    {
        StartCoroutine("Co_Explosion");
    }

    IEnumerator Co_Explosion()
    {
        yield return new WaitForSeconds(1f);
        rigid.linearVelocity = Vector3.zero;
        rigid.angularVelocity = Vector3.zero;

        meshObj.SetActive(false);
        particleObj.SetActive(true);
        
        RaycastHit[] rayHits = Physics.SphereCastAll(transform.position, 15f, Vector3.up, 0f, LayerMask.GetMask("Enemy"));

        foreach (RaycastHit hitObj in rayHits)
            hitObj.transform.GetComponent<Enemy>().HitByGrenade(transform.position);

        GameObject towerObject = ObjectPool.instance.PopFromPool(thisType.ToString(), ObjectPool.instance.TowerPool);
        towerObject.transform.position = new Vector3(transform.position.x, towerObject.transform.localScale.y * 0.5f, transform.position.z);

        DOVirtual.DelayedCall(0.5f, () =>
        {
            towerObject.SetActive(true);
            towerObject.GetComponent<Tower>().SettingTower();
        });

        Destroy(gameObject, 2.5f);
    }
}
