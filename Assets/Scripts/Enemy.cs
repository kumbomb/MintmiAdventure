using System.Collections;
using DG.Tweening;
using UnityEngine;
using UnityEngine.AI;

public class Enemy : MonoBehaviour
{
    [SerializeField] MonsterType enemyType;
    [SerializeField] int curHealth;
    [SerializeField] int maxHealth;
    [SerializeField] GameObject item;
    [SerializeField] GameObject hpBarPrefab;
    [SerializeField] Vector3 hpBarOffset = new Vector3(0f, 2.8f, 0f);
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

    float defaultSpeed;
    float defaultAccSpeed;
    float defaultAngularSpeed;
    bool isSlow;
    bool isBurn;
    bool isAddictive;
    HpBar enemyHpBar;

    private void Awake()
    {
        nav.enabled = false;
        matRenders = GetComponentsInChildren<MeshRenderer>();
        if (MeleeArea != null)
            MeleeArea.enabled = false;
    }

    private void OnDisable()
    {
        if (enemyHpBar != null)
            enemyHpBar.gameObject.SetActive(false);
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
            rigid.linearVelocity = Vector3.zero;
            rigid.angularVelocity = Vector3.zero;
        }
    }

    void Targeting()
    {
        if (enemyType == MonsterType.EnemyBoss || isDead || GameManager.instance.nowGameResultState != GameResultState.None)
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
        if (rayHits.Length > 0 && !isAttack)
            StartCoroutine(Co_Attack());
    }

    IEnumerator Co_Attack()
    {
        isChase = false;
        rigid.linearVelocity = Vector3.zero;
        isAttack = true;

        Vector3 dest = attackTarget.transform.position - transform.position;
        transform.LookAt(transform.position + dest * 3f);
        anim.SetBool("isAttack", true);
        
        switch (enemyType)
        {
            case MonsterType.EnemyA:
                yield return StartCoroutine(Co_Delay(0.5f));
                MeleeArea.enabled = true;
                yield return StartCoroutine(Co_Delay(1f));
                MeleeArea.enabled = false;
                yield return StartCoroutine(Co_Delay(1f));
                break;
            case MonsterType.EnemyB:
                yield return StartCoroutine(Co_Delay(0.5f));
                rigid.AddForce(transform.forward * 30, ForceMode.Impulse);
                MeleeArea.enabled = true;
                yield return StartCoroutine(Co_Delay(0.5f));
                rigid.linearVelocity = Vector3.zero;
                MeleeArea.enabled = false;
                yield return StartCoroutine(Co_Delay(1f));
                break;
            case MonsterType.EnemyC:
                yield return StartCoroutine(Co_Delay(0.5f));
                GameObject instantBullet = ObjectPool.instance.PopFromPool("EnemyCBullet", ObjectPool.instance.MonsterBulletPool);
                instantBullet.transform.position = transform.position;
                instantBullet.transform.rotation = transform.rotation;
                Rigidbody bulletRigid = instantBullet.GetComponent<Rigidbody>();
                bulletRigid.linearVelocity = Vector3.zero;
                instantBullet.SetActive(true);
                bulletRigid.linearVelocity = transform.forward * 20;
                yield return StartCoroutine(Co_Delay(2f));
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
        EnsureHpBar();
        UpdateHpBar();
        enemyHpBar.gameObject.SetActive(true);
        ChaseStart();
    }

    void EnsureHpBar()
    {
        if (enemyHpBar != null)
            return;

        if (hpBarPrefab == null)
            hpBarPrefab = Resources.Load<GameObject>("Prefabs/UI/HP_Bar");

        Canvas hpCanvas = GameObject.Find("Canvas_HP")?.GetComponent<Canvas>();
        if (hpCanvas == null || hpBarPrefab == null)
            return;

        GameObject hpBarObject = Instantiate(hpBarPrefab, hpCanvas.transform);
        enemyHpBar = hpBarObject.GetComponent<HpBar>();
        enemyHpBar.targetTr = transform;
        enemyHpBar.offset = hpBarOffset;
        enemyHpBar.setHpBar = true;
    }

    void UpdateHpBar()
    {
        if (enemyHpBar != null)
            enemyHpBar.UpdateHp(curHealth, maxHealth);
    }

    public void ChasingStart()
    {
        if (enemyType != MonsterType.EnemyBoss)
            Invoke(nameof(ChaseStart), 2f);
    }

    void ChaseStart()
    {
        if (enemyType != MonsterType.EnemyBoss)
        {
            isChase = true;
            anim.SetBool("isWalk", true);
        }
    }
    
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Melee"))
        {
            Weapon weapon = other.GetComponent<Weapon>();
            Vector3 reactVec = transform.position - other.transform.position;
            StartCoroutine(OnDamage(reactVec, false, weapon.weaponSetInfo.MaxDamage));
        }
        else if (other.CompareTag("Bullet"))
        {
            Bullet bulletComponent = other.GetComponent<Bullet>();
            Vector3 reactVec = transform.position - other.transform.position;
            other.gameObject.SetActive(false);
            StartCoroutine(OnDamage(reactVec, false, bulletComponent.damage));
        }
    }

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

    void Slow(float value, float time)
    {
        nav.speed *= value;
        nav.acceleration *= value;
        nav.angularSpeed *= value;
        DOVirtual.DelayedCall(time, ReturnDefaultSpeed);
    }

    void ReturnDefaultSpeed()
    {
        isSlow = false;
        nav.speed = defaultSpeed;
        nav.acceleration = defaultAccSpeed;
        nav.angularSpeed = defaultAngularSpeed;
    }
    
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
            StartCoroutine(OnDamage(transform.position, false, (int)value));
            yield return StartCoroutine(Co_Delay(1f));
        }
        isBurn = false;
    }

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
            StartCoroutine(OnDamage(transform.position, false, (int)value));
            yield return StartCoroutine(Co_Delay(1f));
        }
        isAddictive = false;
    }
        
    IEnumerator OnDamage(Vector3 reactVec, bool isGrenade, int damage)
    {
        if (damage <= 0)
            yield break;

        isChase = false;
        OnDamageMaterial(Color.red);
        curHealth -= damage;
        UpdateHpBar();
        DamageTextManager.ShowDamage(transform.position + hpBarOffset, damage, Color.white);

        if (curHealth <= 0)
        {
            rigid.linearVelocity = Vector3.zero;
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
            if (enemyHpBar != null)
                enemyHpBar.gameObject.SetActive(false);

            if (isGrenade)
            {
                reactVec = reactVec.normalized;
                reactVec += Vector3.up * 6;
                rigid.freezeRotation = false;
                rigid.AddForce(reactVec * 5, ForceMode.Impulse);
                rigid.AddTorque(reactVec * 15, ForceMode.Impulse);
            }
            else
            {
                reactVec = reactVec.normalized;
                reactVec += Vector3.up;
                rigid.AddForce(reactVec * 2, ForceMode.Impulse);
            }

            DropItem();
        }
        else
        {
            rigid.AddForce(transform.forward * -10f, ForceMode.Impulse);
            yield return new WaitForSeconds(0.1f);
            rigid.linearVelocity = Vector3.zero;
            OnDamageMaterial(Color.white);
            isChase = true;
        }
    }

    public void DropItem()
    {
        int maxCnt = enemyType == MonsterType.EnemyBoss ? 5 : 1;
        for (int i = 0; i < maxCnt; i++)
        {
            float delay = i * 0.1f;
            DOVirtual.DelayedCall(delay, SpawnItem);
        }
        Invoke(nameof(DelayDestroy), 2f);
    }

    void SpawnItem()
    {
        GameObject itemObj = ObjectPool.instance.PopFromPool(ItemType.GoldCoin.ToString(), ObjectPool.instance.ItemPool);
        itemObj.SetActive(true);
        itemObj.transform.position = transform.position;
        itemObj.transform.rotation = Quaternion.identity;
        Rigidbody itemRigid = itemObj.GetComponent<Rigidbody>();
        itemRigid.AddForce(transform.up * Random.Range(15f, 22f), ForceMode.Impulse);
        itemObj.GetComponent<Item>().SettingItem();
    }

    public void DelayDestroy()
    {
        gameObject.SetActive(false);
    }

    public void HitByGrenade(Vector3 explosionPos)
    {
        Vector3 reactVec = transform.position - explosionPos;
        StartCoroutine(OnDamage(reactVec, true, 200));
    }

    public void OnDamageMaterial(Color col)
    {
        foreach (MeshRenderer mesh in matRenders)
            mesh.material.color = col;
    }
    
    IEnumerator Co_Delay(float delayTime)
    {
        float nowTime = 0f;
        while (nowTime < delayTime)
        {
            nowTime += Time.deltaTime;
            yield return null;
        }
    }
}
