using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class SubPlayerParent : MonoBehaviour
{
    public Animator anim;
    public Rigidbody rigid;
    public MeshRenderer[] playerMesh;
    public GameObject miniMapTop;

    public int health;
    public int maxHealth;
    public GameObject hpBarPrefab;
    [HideInInspector] public Canvas hpCanvas;
    public Vector3 hpBarOffset = new Vector3(0f, 8f, 0f);

    [HideInInspector] public bool isFireReady;
    [HideInInspector] public bool isBoarder;
    [HideInInspector] public bool isOnDamage;
    [HideInInspector] public bool isReload;
    [HideInInspector] public bool isDead;
    [HideInInspector] public bool isExistTarget;

    public Transform chasingTarget;
    public NavMeshAgent nav;

    public GameObject[] weapons;
    public Weapon nowEquipWeapon;
    public WeaponType equipType;
    [HideInInspector] public float detectRadius;
    [HideInInspector] public float fireDelay;
    [HideInInspector] public Vector3 moveVector;
    [HideInInspector] public Vector3 viewVector;
    public int supporterNum = 0;
    
    public void OnDamageColor(bool flag)
    {
        if (flag)
        {
            foreach (MeshRenderer mesh in playerMesh)
            {
                mesh.material.color = Color.red;
            }
        }
        else
        {
            foreach (MeshRenderer mesh in playerMesh)
            {
                mesh.material.color = Color.white;
            }
        }
    }

    //스스로 회전 방지
    public void FreezeRotation()
    {
        rigid.angularVelocity = Vector3.zero;
    }

    public void StopToWall()
    {
        Vector3 rayFocus;

        rayFocus = moveVector == Vector3.zero ? Vector3.forward : moveVector;

        isBoarder = Physics.Raycast(transform.position, rayFocus, 5f, LayerMask.GetMask("Wall"));
    }
    
    public virtual void Attack()
    {
    }

    public void ResetAttackDealy()
    {
        fireDelay = 0f;
        isFireReady = false;
    }

    public virtual void FindingEnemy()
    {
    }

    public void SetHPBar()
    {
        hpCanvas = GameObject.Find("Canvas_HP").GetComponent<Canvas>();
        hpBarPrefab = Instantiate(hpBarPrefab, hpCanvas.transform);
        hpBarPrefab.SetActive(true);
        var hpbarScript = hpBarPrefab.GetComponent<HpBar>();
        hpbarScript.targetTr = this.gameObject.transform;
        hpbarScript.offset = hpBarOffset;
        hpbarScript.setHpBar = true;
    }

    public void ToggleHpBar()
    {
        hpBarPrefab.SetActive(!GameManager.instance.isPause);
    }

    public IEnumerator Co_Delay(float _delaytime)
    {
        float nowTime = 0f;
        while (nowTime < _delaytime)
        {
            nowTime += Time.deltaTime;
            yield return null;
        }
    }
}
