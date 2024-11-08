using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class Enemy : MonoBehaviour
{
    [SerializeField] MonsterType enemyType;
    [SerializeField] int curHealth;
    [SerializeField] int maxHealth;
    [SerializeField] GameObject item;
    public Transform target;
    public Transform attackTarget;
    public BoxCollider MeleeArea;
    public GameObject bullet;
    [SerializeField] bool isChase;
    public bool isDead;
    [SerializeField] bool isAttack;

    [SerializeField] Rigidbody rigid;
    public BoxCollider boxCollider;

    [SerializeField] MeshRenderer[] matRenders;

    public NavMeshAgent nav;
    public Animator anim;

    public SpriteRenderer miniMapTop;
    public Color aliveColor;
    public Color deathColor;

    int resetHealth;
    float defaultSpeed;
    float defaultAccSpeed;
    float defaultAngularSpeed;

    bool isSlow;
    bool isBurn;
    bool isAddictive;

    private void Awake()
    {
        nav.enabled = false;
        matRenders = GetComponentsInChildren<MeshRenderer>();
        if (MeleeArea != null)
            MeleeArea.enabled = false;
    }
    
    private void Update()
    {
        if(nav.enabled && enemyType != MonsterType.EnemyBoss)
        {
            if(GameManager.instance.nowGameResultState != GameResultState.None)
            {
                isChase = false;
                anim.SetBool("isWalk", false);
            }
            nav.SetDestination(target.position);
            //Navigation 완전 종료
            nav.isStopped = !isChase;
        }
    }

    private void FixedUpdate()
    {
        Targeting();
        FreezeRotation();
    }
    
    void FreezeRotation()
    {
        if (isChase)
        {
            rigid.velocity = Vector3.zero;
            rigid.angularVelocity = Vector3.zero;
        }
    }

    void Targeting()
    {
        if (enemyType == MonsterType.EnemyBoss || isDead ||
            GameManager.instance.nowGameResultState != GameResultState.None)
            return;

        float targetRadius = 0f;
        float targetRange = 0f;

        switch (enemyType)
        {
            case MonsterType.EnemyA:
                targetRadius = 2f;
                targetRange = 5f;
                break;

            case MonsterType.EnemyB:
                targetRadius = 2f;
                targetRange = 10f;
                break;

            case MonsterType.EnemyC:
                targetRadius = 15f;
                targetRange = 20f;
                break;
        }
        
        RaycastHit[] rayHits = Physics.SphereCastAll(transform.position, targetRadius, transform.forward, targetRange, LayerMask.GetMask("Player"));
               
        //범위내에 플레이어가 걸리면 쏴라
        if (rayHits.Length > 0 && !isAttack)
        {
            StartCoroutine(Co_Attack());
        }
    }

    //Enemy c 는 멈춰서 쏨
    IEnumerator Co_Attack()
    {
        isChase = false;
        rigid.velocity = Vector3.zero;
        isAttack = true;

        Vector3 dest = attackTarget.transform.position - transform.position;
        transform.LookAt(transform.position + (dest * 3f));

        anim.SetBool("isAttack", true);
        
        switch (enemyType)
        {
            case MonsterType.EnemyA:
                {
                    yield return StartCoroutine(Co_Delay(0.5f));
                    MeleeArea.enabled = true;

                    yield return StartCoroutine(Co_Delay(1f));

                    MeleeArea.enabled = false;
                    
                    yield return StartCoroutine(Co_Delay(1f));
                }
                break;
            case MonsterType.EnemyB:
                {
                    yield return StartCoroutine(Co_Delay(0.5f));
                    rigid.AddForce(transform.forward * 30, ForceMode.Impulse);
                    MeleeArea.enabled = true;

                    yield return StartCoroutine(Co_Delay(0.5f));

                    rigid.velocity = Vector3.zero;
                    MeleeArea.enabled = false;

                    yield return StartCoroutine(Co_Delay(1f));
                }
                break;
            case MonsterType.EnemyC:
                {
                    yield return StartCoroutine(Co_Delay(.5f));
                    GameObject instantBullet = ObjectPool.instance.PopFromPool("EnemyCBullet", ObjectPool.instance.MonsterBulletPool);//Instantiate(bullet, transform.position, transform.rotation);
                    instantBullet.transform.position = transform.position;
                    instantBullet.transform.rotation = transform.rotation;
                    Rigidbody bulletRigid = instantBullet.GetComponent<Rigidbody>();
                    bulletRigid.velocity = Vector3.zero;
                    instantBullet.SetActive(true);
                    bulletRigid.velocity = transform.forward * 20;

                    yield return StartCoroutine(Co_Delay(2f));
                }
                break;
        }

        anim.SetBool("isAttack", false);
        isChase = true;
        isAttack = false;
    }

    public void ResetEnemy()
    {
        target = GameObject.FindGameObjectWithTag("Player").transform;
        attackTarget = GameObject.FindGameObjectWithTag("Player").transform;
        curHealth = maxHealth;
        defaultSpeed = nav.speed;
        defaultAccSpeed = nav.acceleration;
        defaultAngularSpeed = nav.angularSpeed;
        isSlow = false;
        isBurn = false;
        isAddictive = false;
        isDead = false;
        nav.enabled = true;
        OnDamageMaterial(Color.white);
        gameObject.layer = 13;
        if (MeleeArea != null)
            MeleeArea.enabled = false;
        isAttack = false;
        miniMapTop.color = aliveColor;
        ChaseStart();
    }

    public void ChasingStart()
    {
        //보스는 추적 x
        if (enemyType != MonsterType.EnemyBoss)
            Invoke("ChaseStart", 2f);
    }

    void ChaseStart()
    {
        if (enemyType != MonsterType.EnemyBoss)
        {
            isChase = true;
            anim.SetBool("isWalk", true);
        }
    }
    
    #region ======= 피격 관련 =======

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Melee"))
        {
            Weapon weapon = other.GetComponent<Weapon>();
            //curHealth -= weapon.thisWeaponData.damage;

            Vector3 _reactVec = transform.position - other.transform.position;

            StartCoroutine(OnDamage(_reactVec, false, weapon.weaponSetInfo.MaxDamage));
        }
        else if (other.CompareTag("Bullet"))
        {
            Bullet bullet = other.GetComponent<Bullet>();
            //curHealth -= bullet.damage;

            Vector3 _reactVec = transform.position - other.transform.position;
            other.gameObject.SetActive(false);
            //Destroy(other.gameObject);
            StartCoroutine(OnDamage(_reactVec, false, bullet.damage));
        }
    }

    // ============ Tower 관련 =============== //

    public void OnDamagedFromTower(BuffType buffType, float value, float time)
    {
        switch (buffType)
        {
            case BuffType.Slow:
                if (isSlow)
                    return;
                isSlow = true;
                Slow(value, time);
                break;
            case BuffType.Burn:
                if (isBurn)
                    return;
                isBurn = true;
                Burn(value, time);
                break;
            case BuffType.Addiction:
                if (isAddictive)
                    return;
                isAddictive = true;
                Addictive(value, time);
                break;
        }
    }

    //Freeze Slow 
    void Slow(float value, float time)
    {
        nav.speed *= value;
        nav.acceleration *= value;
        nav.angularSpeed *= value;
        LeanTween.delayedCall(time, ReturnDefaultSpeed);
    }
    void ReturnDefaultSpeed()
    {
        isSlow = false;
        nav.speed = defaultSpeed;
        nav.acceleration = defaultAccSpeed;
        nav.angularSpeed = defaultAngularSpeed;
    }
    
    //Blaze Burn
    void Burn(float value, float time)
    {
        StartCoroutine(OnBurnDamage(value, time));
    }
    IEnumerator OnBurnDamage(float value, float time)
    {
        for (int i = 0; i < (int)time; i++)
        {
            if (isDead)
            {
                isBurn = false;
                yield break;
            }
            //curHealth -= 10;
            StartCoroutine(OnDamage(transform.position, false, (int)value));
            yield return StartCoroutine(Co_Delay(1f));
            //yield return new WaitForSeconds(1f);
        }
        isBurn = false;
    }

    //Poison Addictive
    void Addictive(float value, float time)
    {
        StartCoroutine(OnAddictiveDamage(value, time));
    }
    IEnumerator OnAddictiveDamage(float value, float time)
    {
        for (int i = 0; i < (int)time; i++)
        {
            if (isDead)
            {
                isAddictive = false;
                yield break;
            }
            //curHealth -= 8;
            StartCoroutine(OnDamage(transform.position, false, (int)value));
            yield return StartCoroutine(Co_Delay(1f));
            //yield return new WaitForSeconds(1f);
        }
        isAddictive = false;
    }
        
    // ======================================== //

    // 실제 데미지 처리
    IEnumerator OnDamage(Vector3 reactVec, bool isGrenade, int damage)
    {
        if (damage > 0)
        {
            isChase = false;
            OnDamageMaterial(Color.red);

            //reactVec = reactVec.normalized;
            curHealth -= damage;

            if (curHealth <= 0)
            {
                rigid.velocity = Vector3.zero;
                if (isDead)
                    yield break;
                miniMapTop.color = deathColor;
                isDead = true;
                OnDamageMaterial(Color.gray);
                gameObject.layer = 14;

                GameManager.instance.UpdateGameState(true);

                nav.speed = defaultSpeed;
                nav.acceleration = defaultAccSpeed;
                nav.angularSpeed = defaultAngularSpeed;

                nav.enabled = false;
                isChase = false;
                isAttack = false;

                if (MeleeArea != null)
                    MeleeArea.enabled = false;
                anim.SetTrigger("doDie");

                if (isGrenade)
                {
                    reactVec = reactVec.normalized;
                    reactVec += (Vector3.up * 6);

                    rigid.freezeRotation = false;
                    rigid.AddForce(reactVec * 5, ForceMode.Impulse);
                    rigid.AddTorque(reactVec * 15, ForceMode.Impulse);
                }
                else
                {
                    //날아가는연출
                    reactVec = reactVec.normalized;
                    reactVec += Vector3.up;
                    rigid.AddForce(reactVec * 2, ForceMode.Impulse);
                }

                DropItem();
                //StartCoroutine("Co_DropItem");
                //StopAllCoroutines();
            }
            else
            {
                //reactVec += (Vector3.up * 6);
                rigid.AddForce(transform.forward * -10f, ForceMode.Impulse);

                yield return new WaitForSeconds(0.1f);
                rigid.velocity = Vector3.zero;
                OnDamageMaterial(Color.white);
                isChase = true;
            }
        }
    }

    #endregion

    public void DropItem()
    {
        int maxCnt = enemyType == MonsterType.EnemyBoss ? 5 : 1;

        for (int i = 0; i < maxCnt; i++)
            LeanTween.delayedCall((i * 0.1f), SpawnItem);

        Invoke("DelayDestroy", 2f);
    }

    void SpawnItem()
    {
       // Debug.Log("drop");
        GameObject itemObj = ObjectPool.instance.PopFromPool(ItemType.GoldCoin.ToString(), ObjectPool.instance.ItemPool);
        itemObj.SetActive(true);
        itemObj.transform.position = this.transform.position;
        itemObj.transform.rotation = Quaternion.identity;
        Rigidbody rigidd = itemObj.GetComponent<Rigidbody>();
        rigidd.AddForce(transform.up * ((float)Random.Range(15, 22)), ForceMode.Impulse);
        itemObj.GetComponent<Item>().SettingItem();
    }

    IEnumerator Co_DropItem()
    {
        int maxCnt = enemyType == MonsterType.EnemyBoss ? 5 : 1;
        for (int i = 0; i < maxCnt; i++)
        {
            Debug.Log("drop");
            DropItem();
            yield return null;
        }
        yield return StartCoroutine(Co_Delay(2f));
       // yield return new WaitForSeconds(2f);
        DelayDestroy();
        //Invoke("DelayDestroy", 2f);
    }

    public void DelayDestroy()
    {
        this.gameObject.SetActive(false);
       // ObjectPool.instance.PushToPool(enemyType.ToString(), this.gameObject);
    }

    public void HitByGrenade(Vector3 explosionPos)
    {
       // curHealth -= 200;
        Vector3 reactVec = transform.position - explosionPos;
        StartCoroutine(OnDamage(reactVec, true, 0));
    }

    public void OnDamageMaterial(Color col)
    {
        foreach(MeshRenderer mesh in matRenders)
        {
            mesh.material.color = col;
        }
    }
    
    IEnumerator Co_Delay(float _delaytime)
    {
        float nowTime = 0f;
        while (nowTime < _delaytime)
        {
            nowTime += Time.deltaTime;
            yield return null;
        }
    }

    void DelayTime(float time)
    {
        float delayTime = 0f;

        while(delayTime <= time)
            delayTime += Time.deltaTime;

       // return true;
    }
}
