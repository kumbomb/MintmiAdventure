using System.Collections;
using DG.Tweening;
using UnityEngine;
using UnityEngine.AI;

public class Enemy : MonoBehaviour
{
    [SerializeField] MonsterType enemyType;
    [SerializeField] MonsterStatData monsterData;
    [SerializeField] MonsterStageScaling stageScaling;
    [SerializeField] int curHealth;
    [SerializeField] int maxHealth;
    [SerializeField] GameObject item;
    [SerializeField] GameObject hpBarPrefab;
    [SerializeField] Vector3 hpBarOffset = new Vector3(0f, 2.8f, 0f);
    [SerializeField] float hudTopPadding = 0.8f;
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
    float targetRefreshTimer;
    bool isSlow;
    bool isBurn;
    bool isAddictive;
    int runtimeClusterId = -1;
    Vector3 runtimeHudOffset;
    HpBar enemyHpBar;
    MonsterRuntimeStats runtimeStats;

    const float ChaseRefreshInterval = 0.2f;

    void Awake()
    {
        ResolveRuntimeStats();
        if (nav != null)
            nav.enabled = false;
        matRenders = GetComponentsInChildren<MeshRenderer>();
        if (MeleeArea != null)
            MeleeArea.enabled = false;
    }

    void OnDisable()
    {
        if (enemyHpBar != null)
            enemyHpBar.gameObject.SetActive(false);
    }

    public void ConfigureStats(MonsterStageScaling scaling)
    {
        stageScaling = scaling;
        ResolveRuntimeStats();
    }

    void ResolveRuntimeStats()
    {
        runtimeStats = monsterData != null ? monsterData.CreateRuntimeStats(stageScaling) : CreateFallbackStats();
        maxHealth = runtimeStats.maxHealth;
        hudTopPadding = runtimeStats.hudTopPadding;
        defaultSpeed = runtimeStats.moveSpeed;
        defaultAccSpeed = runtimeStats.acceleration;
        defaultAngularSpeed = runtimeStats.angularSpeed;
        if (nav != null)
        {
            nav.speed = defaultSpeed;
            nav.acceleration = defaultAccSpeed;
            nav.angularSpeed = defaultAngularSpeed;
        }
    }

    MonsterRuntimeStats CreateFallbackStats()
    {
        MonsterRuntimeStats stats = new MonsterRuntimeStats
        {
            maxHealth = Mathf.Max(1, maxHealth),
            moveSpeed = nav != null ? nav.speed : 10f,
            acceleration = nav != null ? nav.acceleration : 25f,
            angularSpeed = nav != null ? nav.angularSpeed : 360f,
            attackRange = enemyType == MonsterType.EnemyA ? 5f : enemyType == MonsterType.EnemyB ? 7f : enemyType == MonsterType.EnemyC ? 20f : 0f,
            meleeWindup = 0.5f,
            meleeActiveTime = 1f,
            meleeRecovery = 1f,
            dashWindup = 0.25f,
            dashDistance = 4.5f,
            dashDuration = 0.22f,
            dashRecovery = 0.8f,
            rangedWindup = 0.5f,
            rangedRecovery = 2f,
            projectileSpeed = 20f,
            hudTopPadding = hudTopPadding,
        };
        stats.ApplyScaling(stageScaling);
        return stats;
    }

    void Update()
    {
        UpdateMiniMapMarker();

        if (nav == null || !nav.enabled || enemyType == MonsterType.EnemyBoss)
            return;

        if (GameManager.instance.nowGameResultState != GameResultState.None)
        {
            StopChasing();
            return;
        }

        if (enemyType == MonsterType.EnemyC && attackTarget != null && IsWithinAttackRange())
            StopChasing();

        nav.isStopped = !isChase;
        UpdateMoveAnimation();

        if (!isChase || target == null)
            return;

        targetRefreshTimer -= Time.deltaTime;
        if (targetRefreshTimer > 0f)
            return;

        targetRefreshTimer = ChaseRefreshInterval;
        nav.SetDestination(target.position);
    }

    void FixedUpdate()
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

    void UpdateMiniMapMarker()
    {
        if (miniMapTop == null)
            return;

        Transform markerTransform = miniMapTop.transform;
        markerTransform.position = new Vector3(transform.position.x, markerTransform.position.y, transform.position.z);
        markerTransform.rotation = Quaternion.Euler(90f, 0f, 0f);
    }

    void UpdateMoveAnimation()
    {
        if (anim == null)
            return;

        bool isMoving = isChase && nav.enabled && !nav.isStopped && nav.velocity.sqrMagnitude > 0.01f;
        anim.SetBool("isWalk", isMoving);
    }

    float GetAttackRange()
    {
        return runtimeStats.attackRange;
    }

    bool IsWithinAttackRange()
    {
        if (attackTarget == null)
            return false;

        float attackRange = GetAttackRange();
        Vector3 toTarget = attackTarget.position - transform.position;
        toTarget.y = 0f;
        return toTarget.sqrMagnitude <= attackRange * attackRange;
    }

    void StopChasing()
    {
        isChase = false;
        if (nav != null && nav.enabled)
        {
            nav.isStopped = true;
            nav.ResetPath();
        }
        if (anim != null)
            anim.SetBool("isWalk", false);
    }

    void ResumeChasing()
    {
        if (enemyType == MonsterType.EnemyBoss)
            return;

        isChase = true;
        if (nav != null && nav.enabled)
            nav.isStopped = false;
        targetRefreshTimer = 0f;
    }

    void Targeting()
    {
        if (enemyType == MonsterType.EnemyBoss || isDead || isAttack || attackTarget == null || GameManager.instance.nowGameResultState != GameResultState.None)
            return;

        if (!IsWithinAttackRange())
        {
            if (enemyType == MonsterType.EnemyC)
                ResumeChasing();
            return;
        }

        StartCoroutine(Co_Attack());
    }

    IEnumerator Co_Attack()
    {
        StopChasing();
        rigid.linearVelocity = Vector3.zero;
        isAttack = true;

        Vector3 dest = attackTarget.position - transform.position;
        dest.y = 0f;
        if (dest != Vector3.zero)
            transform.LookAt(transform.position + dest);
        anim.SetBool("isAttack", true);

        switch (enemyType)
        {
            case MonsterType.EnemyA:
                yield return StartCoroutine(Co_Delay(runtimeStats.meleeWindup));
                MeleeArea.enabled = true;
                yield return StartCoroutine(Co_Delay(runtimeStats.meleeActiveTime));
                MeleeArea.enabled = false;
                yield return StartCoroutine(Co_Delay(runtimeStats.meleeRecovery));
                break;
            case MonsterType.EnemyB:
                yield return StartCoroutine(Co_Delay(runtimeStats.dashWindup));
                yield return StartCoroutine(Co_DashAttack(runtimeStats.dashDistance, runtimeStats.dashDuration));
                yield return StartCoroutine(Co_Delay(runtimeStats.dashRecovery));
                break;
            case MonsterType.EnemyC:
                yield return StartCoroutine(Co_Delay(runtimeStats.rangedWindup));
                GameObject instantBullet = ObjectPool.instance.PopFromPool("EnemyCBullet", ObjectPool.instance.MonsterBulletPool);
                instantBullet.transform.position = transform.position;
                instantBullet.transform.rotation = transform.rotation;
                Rigidbody bulletRigid = instantBullet.GetComponent<Rigidbody>();
                bulletRigid.linearVelocity = Vector3.zero;
                instantBullet.SetActive(true);
                bulletRigid.linearVelocity = transform.forward * runtimeStats.projectileSpeed;
                yield return StartCoroutine(Co_Delay(runtimeStats.rangedRecovery));
                break;
        }

        anim.SetBool("isAttack", false);
        isAttack = false;

        if (enemyType == MonsterType.EnemyC)
        {
            if (!IsWithinAttackRange())
                ResumeChasing();
            else
                StopChasing();
        }
        else
        {
            ResumeChasing();
        }
    }

    IEnumerator Co_DashAttack(float dashDistance, float dashDuration)
    {
        if (nav != null && nav.enabled)
            nav.enabled = false;

        Vector3 dashDirection = transform.forward;
        dashDirection.y = 0f;
        dashDirection.Normalize();
        Vector3 startPosition = transform.position;
        Vector3 targetPosition = startPosition + dashDirection * dashDistance;
        MeleeArea.enabled = true;

        float elapsed = 0f;
        while (elapsed < dashDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / dashDuration);
            Vector3 nextPosition = Vector3.Lerp(startPosition, targetPosition, t);
            rigid.MovePosition(nextPosition);
            yield return null;
        }

        rigid.linearVelocity = Vector3.zero;
        MeleeArea.enabled = false;
        if (nav != null)
        {
            nav.enabled = true;
            nav.speed = defaultSpeed;
            nav.acceleration = defaultAccSpeed;
            nav.angularSpeed = defaultAngularSpeed;
        }
    }

    public void ResetEnemy()
    {
        ResolveRuntimeStats();
        Transform playerTransform = GameObject.FindGameObjectWithTag("Player")?.transform;
        target = playerTransform;
        attackTarget = playerTransform;
        curHealth = maxHealth;
        targetRefreshTimer = 0f;
        isSlow = false;
        isBurn = false;
        isAddictive = false;
        isDead = false;
        runtimeClusterId = -1;
        runtimeHudOffset = CalculateHudOffset();
        if (nav != null)
        {
            nav.speed = defaultSpeed;
            nav.acceleration = defaultAccSpeed;
            nav.angularSpeed = defaultAngularSpeed;
            nav.enabled = true;
        }
        OnDamageMaterial(Color.white);
        gameObject.layer = 13;
        if (MeleeArea != null)
            MeleeArea.enabled = false;
        isAttack = false;
        if (miniMapTop != null)
            miniMapTop.color = aliveColor;
        EnsureHpBar();
        UpdateHpBar();
        if (enemyHpBar != null)
            enemyHpBar.gameObject.SetActive(true);
        ResumeChasing();
    }

    public void SetSpawnContext(int clusterId)
    {
        runtimeClusterId = clusterId;
    }

    Vector3 CalculateHudOffset()
    {
        float topY = transform.position.y + hpBarOffset.y;
        bool hasBounds = false;

        if (matRenders != null)
        {
            for (int i = 0; i < matRenders.Length; i++)
            {
                MeshRenderer mesh = matRenders[i];
                if (mesh == null)
                    continue;

                Bounds bounds = mesh.bounds;
                if (!hasBounds || bounds.max.y > topY)
                {
                    topY = bounds.max.y;
                    hasBounds = true;
                }
            }
        }

        if (boxCollider != null)
        {
            Bounds colliderBounds = boxCollider.bounds;
            if (!hasBounds || colliderBounds.max.y > topY)
            {
                topY = colliderBounds.max.y;
                hasBounds = true;
            }
        }

        float finalOffsetY = (topY - transform.position.y) + hudTopPadding;
        finalOffsetY = Mathf.Max(finalOffsetY, hpBarOffset.y + hudTopPadding);
        return new Vector3(hpBarOffset.x, finalOffsetY, hpBarOffset.z);
    }

    Vector3 GetHudWorldPosition()
    {
        return transform.position + runtimeHudOffset;
    }

    void EnsureHpBar()
    {
        if (enemyHpBar == null)
        {
            if (hpBarPrefab == null)
                hpBarPrefab = Resources.Load<GameObject>("Prefabs/UI/HP_Bar");

            Canvas hpCanvas = GameObject.Find("Canvas_HP")?.GetComponent<Canvas>();
            if (hpCanvas == null || hpBarPrefab == null)
                return;

            GameObject hpBarObject = Instantiate(hpBarPrefab, hpCanvas.transform);
            enemyHpBar = hpBarObject.GetComponent<HpBar>();
            enemyHpBar.targetTr = transform;
            enemyHpBar.setHpBar = true;
        }

        enemyHpBar.offset = runtimeHudOffset;
    }

    void UpdateHpBar()
    {
        if (enemyHpBar != null)
            enemyHpBar.UpdateHp(curHealth, maxHealth);
    }

    public void ChasingStart()
    {
        if (enemyType != MonsterType.EnemyBoss)
            Invoke(nameof(ResumeChasing), 2f);
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Melee"))
        {
            Weapon weapon = other.GetComponent<Weapon>();
            if (weapon == null)
                return;

            Vector3 reactVec = transform.position - other.transform.position;
            StartCoroutine(OnDamage(reactVec, false, weapon.weaponSetInfo.MaxDamage));
        }
        else if (other.CompareTag("Bullet"))
        {
            Bullet bulletComponent = other.GetComponent<Bullet>();
            if (bulletComponent == null)
                return;

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
        int tickCount = Mathf.Max(1, Mathf.RoundToInt(time));
        for (int i = 0; i < tickCount; i++)
        {
            if (isDead)
            {
                isBurn = false;
                yield break;
            }

            StartCoroutine(OnDamage(transform.position, false, Mathf.RoundToInt(value)));
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
        int tickCount = Mathf.Max(1, Mathf.RoundToInt(time));
        for (int i = 0; i < tickCount; i++)
        {
            if (isDead)
            {
                isAddictive = false;
                yield break;
            }

            StartCoroutine(OnDamage(transform.position, false, Mathf.RoundToInt(value)));
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
        DamageTextManager.ShowDamage(GetHudWorldPosition(), damage, new Color(1f, 0.25f, 0.25f));

        if (curHealth <= 0)
        {
            rigid.linearVelocity = Vector3.zero;
            if (isDead)
                yield break;

            if (miniMapTop != null)
                miniMapTop.color = deathColor;
            isDead = true;
            GameManager.instance.RegisterEnemyKill(enemyType, runtimeClusterId);
            runtimeClusterId = -1;
            OnDamageMaterial(Color.gray);
            gameObject.layer = 14;
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

            reactVec = reactVec.normalized;
            if (isGrenade)
            {
                reactVec += Vector3.up * 6f;
                rigid.freezeRotation = false;
                rigid.AddForce(reactVec * 5f, ForceMode.Impulse);
                rigid.AddTorque(reactVec * 15f, ForceMode.Impulse);
            }
            else
            {
                reactVec += Vector3.up;
                rigid.AddForce(reactVec * 2f, ForceMode.Impulse);
            }

            DropItem();
        }
        else
        {
            rigid.AddForce(transform.forward * -10f, ForceMode.Impulse);
            yield return new WaitForSeconds(0.1f);
            rigid.linearVelocity = Vector3.zero;
            OnDamageMaterial(Color.white);
            if (enemyType == MonsterType.EnemyC && IsWithinAttackRange())
                StopChasing();
            else
                ResumeChasing();
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
