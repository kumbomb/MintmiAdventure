using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Grenade : MonoBehaviour
{
    [SerializeField] GameObject meshObj;
    [SerializeField] GameObject particleObj;
    [SerializeField] Rigidbody rigid;
    [SerializeField] TowerType thisType;
    [SerializeField] float coolTime; 

    // Start is called before the first frame update
    void Start()
    {
        StartCoroutine("Co_Explosion");
    }

    IEnumerator Co_Explosion()
    {
        yield return new WaitForSeconds(1f);
        rigid.velocity = Vector3.zero;
        rigid.angularVelocity = Vector3.zero;

        meshObj.SetActive(false);
        particleObj.SetActive(true);
        
        //터진 구 범위에 걸린 모든 obj
        RaycastHit[] rayHits = Physics.SphereCastAll(transform.position, 15f, Vector3.up, 0f, LayerMask.GetMask("Enemy"));

        foreach(RaycastHit hitObj in rayHits)
            hitObj.transform.GetComponent<Enemy>().HitByGrenade(transform.position);

        Vector3 originScale;
        GameObject Gobj = ObjectPool.instance.PopFromPool(thisType.ToString(), ObjectPool.instance.TowerPool);
        Gobj.transform.position = new Vector3(transform.position.x, Gobj.transform.localScale.y * 0.5f, transform.position.z);
        originScale = Gobj.transform.localScale;

        LeanTween.delayedCall(.5f,
            () => {
                Gobj.SetActive(true);
                Gobj.GetComponent<Tower>().SettingTower();
            });

        Destroy(gameObject,2.5f);
    }
}
