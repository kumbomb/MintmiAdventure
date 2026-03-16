using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SubPlayer : SubPlayerParent
{
    [SerializeField] Transform[] bulletPos;
    public BehaviourSetting behaviourSetting;
    public ItemHolder itemHolder;
    [SerializeField] float checkDist = 0f;
    [SerializeField] float followStopDistance = 1.2f;
    [SerializeField] float followResumeDistance = 2.4f;
    [SerializeField] bool reverseFacing = true;

    private float AniSpeed
    {
        get => anim.GetFloat("Speed");
        set => anim.SetFloat("Speed", value);
    }

    private int AniWeaponBehaviourID
    {
        get => behaviourSetting.HasHand ? (int)anim.GetFloat("AttackID") : 1;
        set
        {
            anim.SetFloat("AttackID", value);
            anim.SetFloat("MovementID",
                value <= 0 ? 0 :
                value <= 2 ? 1 :
                value <= 5 ? 2 :
                value <= 7 ? 3 :
                4);
        }
    }

    private bool AniOnGround
    {
        get => anim.GetBool("OnGround");
        set => anim.SetBool("OnGround", value);
    }

    public bool Attacking => AniAttack1 || AniAttack2;

    private bool AniAttack1
    {
        get => anim.GetBool("Attack1");
        set => anim.SetBool("Attack1", value);
    }

    private bool AniAttack2
    {
        get => anim.GetBool("Attack2");
        set => anim.SetBool("Attack2", value);
    }

    int atkCnt = 0;
    public bool isChasing;
    float dist = -1f;

    [SerializeField] GameObject atkTarget;

    void Start()
    {
        rigid = GetComponent<Rigidbody>();
        playerMesh = GetComponentsInChildren<MeshRenderer>();
        SetHPBar();
        atkCnt = 0;
        AniWeaponBehaviourID = (int)nowEquipWeapon.RetItemBehaviour();
        chasingTarget = GameManager.instance.playerScript.chasePos[supporterNum].transform;
        if (nav != null)
        {
            nav.stoppingDistance = followStopDistance;
            nav.updateRotation = true;
        }
    }

    void Update()
    {
        if (GameManager.instance.playerScript != null && GameManager.instance.playerScript.isDead)
            return;

        miniMapTop.transform.position = new Vector3(transform.position.x, miniMapTop.transform.position.y, transform.position.z);
        miniMapTop.transform.rotation = Quaternion.Euler(90, 0, 0);

        FindingEnemy();
        CheckDistfromPlayer();
        SyncMoveAnimation();
        Attack();
    }

    void FixedUpdate()
    {
        if (GameManager.instance.playerScript != null && GameManager.instance.playerScript.isDead)
            return;
        FreezeRotation();
        StopToWall();
    }

    void SyncMoveAnimation()
    {
        if (nav == null)
            return;

        bool isMoving = nav.enabled && !nav.isStopped && nav.velocity.sqrMagnitude > 0.04f;
        AniSpeed = isMoving ? 1f : 0f;
        if (isMoving)
            FaceDirection(nav.desiredVelocity);
    }

    public void CheckDistfromPlayer()
    {
        if (GameManager.instance.nowPlayerCharacter == null || chasingTarget == null || nav == null)
            return;

        float distToPlayer = Vector3.Distance(transform.position, GameManager.instance.nowPlayerCharacter.transform.position);
        bool canFight = distToPlayer < checkDist;
        bool hasTarget = atkTarget != null && atkTarget.activeInHierarchy;
        Enemy targetEnemy = hasTarget ? atkTarget.GetComponent<Enemy>() : null;
        hasTarget = hasTarget && targetEnemy != null && !targetEnemy.isDead;

        if (!canFight)
        {
            atkTarget = null;
            isExistTarget = false;
            MoveTo(chasingTarget.position, followStopDistance);
            return;
        }

        if (hasTarget)
        {
            float distToEnemy = Vector3.Distance(transform.position, atkTarget.transform.position);
            float attackStopDistance = Mathf.Max(0.8f, nowEquipWeapon.weaponSetInfo.atkRadius * 0.85f);
            FaceDirection(atkTarget.transform.position - transform.position);

            if (distToEnemy <= nowEquipWeapon.weaponSetInfo.atkRadius)
            {
                StopChasing();
                return;
            }

            MoveTo(atkTarget.transform.position, attackStopDistance);
            return;
        }

        float distToAnchor = Vector3.Distance(transform.position, chasingTarget.position);
        if (distToAnchor > followResumeDistance)
        {
            MoveTo(chasingTarget.position, followStopDistance);
            return;
        }

        StopChasing();
        if (GameManager.instance.nowPlayerCharacter != null)
            FaceDirection(GameManager.instance.nowPlayerCharacter.transform.forward);
    }

    void MoveTo(Vector3 destination, float stopDistance)
    {
        if (nav == null)
            return;

        isChasing = true;
        nav.isStopped = false;
        nav.stoppingDistance = stopDistance;
        nav.SetDestination(destination);
    }

    void StopChasing()
    {
        isChasing = false;
        if (nav == null)
            return;

        nav.isStopped = true;
        nav.ResetPath();
        rigid.linearVelocity = Vector3.zero;
    }

    void FaceDirection(Vector3 direction)
    {
        direction.y = 0f;
        if (direction.sqrMagnitude <= 0.001f)
            return;

        Vector3 lookDirection = reverseFacing ? -direction.normalized : direction.normalized;
        transform.rotation = Quaternion.LookRotation(lookDirection);
    }

    public override void Attack()
    {
        if (GameManager.instance.RetRemainEnemyCnt() <= 0 || isChasing || !isExistTarget || atkTarget == null)
        {
            atkCnt = 0;
            anim.SetBool("Attack1", false);
            anim.SetBool("Attack2", false);
            fireDelay = 0f;
            return;
        }

        var headingToEnemyDist = Vector3.Distance(transform.position, atkTarget.transform.position);
        var headingToEnemyVec = atkTarget.transform.position - transform.position;

        if (headingToEnemyDist > nowEquipWeapon.weaponSetInfo.atkRadius)
        {
            atkCnt = 0;
            anim.SetBool("Attack1", false);
            anim.SetBool("Attack2", false);
            fireDelay = 0f;
            return;
        }

        FaceDirection(headingToEnemyVec);

        fireDelay += Time.deltaTime;
        isFireReady = nowEquipWeapon.weaponSetInfo.rate < fireDelay;

        if (isFireReady)
        {
            if (atkCnt % 3 == 2)
            {
                if (nowEquipWeapon.weaponSetInfo.attackType == AttackType.Range)
                    nowEquipWeapon.UseWeapon(bulletPos[1]);
                else
                    nowEquipWeapon.UseWeapon();

                anim.SetBool("Attack1", false);
                anim.SetBool("Attack2", true);
                atkCnt = 0;
            }
            else
            {
                if (nowEquipWeapon.weaponSetInfo.attackType == AttackType.Range)
                    nowEquipWeapon.UseWeapon(bulletPos[0]);
                else
                    nowEquipWeapon.UseWeapon();

                anim.SetBool("Attack2", false);
                anim.SetBool("Attack1", true);
                atkCnt++;
            }

            ResetAttackDealy();
        }
        base.Attack();
    }

    public override void FindingEnemy()
    {
        if (atkTarget != null)
        {
            Enemy currentEnemy = atkTarget.GetComponent<Enemy>();
            if (currentEnemy != null && !currentEnemy.isDead)
            {
                isExistTarget = true;
                return;
            }
        }

        detectRadius = nowEquipWeapon.weaponSetInfo.detectRadius;
        Collider[] targets = Physics.OverlapSphere(transform.position, detectRadius, LayerMask.GetMask("Enemy"));

        if (GameManager.instance.RetRemainEnemyCnt() <= 0 || targets.Length <= 0)
        {
            isExistTarget = false;
            atkTarget = null;
            viewVector = transform.position;
            dist = -1f;
            return;
        }

        isExistTarget = true;
        atkTarget = null;
        dist = -1f;

        for (int i = 0; i < targets.Length; i++)
        {
            Enemy enemy = targets[i].GetComponent<Enemy>();
            if (enemy == null || enemy.isDead)
                continue;

            Vector3 targetDelta = targets[i].transform.position - transform.position;
            float len = targetDelta.sqrMagnitude;
            if (dist >= 0f && dist < len)
                continue;

            dist = len;
            viewVector = targetDelta;
            atkTarget = targets[i].gameObject;
        }

        if (atkTarget == null)
            isExistTarget = false;

        base.FindingEnemy();
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Floor"))
            AniOnGround = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("EnemyBullet"))
        {
            if (other.GetComponent<Rigidbody>() != null)
                other.gameObject.SetActive(false);

            if (isOnDamage)
                return;

            bool bossAttack = other.name.Equals("BossMeleeArea");
            Bullet enemyBullet = other.GetComponent<Bullet>();
            int incomingDamage = enemyBullet != null ? enemyBullet.damage : 0;

            StartCoroutine(OnDamage(bossAttack, incomingDamage));
        }
    }

    IEnumerator OnDamage(bool bossAttack, int damage)
    {
        if (GameManager.instance.nowGameResultState != GameResultState.None)
            yield break;

        isOnDamage = true;
        OnDamageColor(true);

        health -= damage;
        hpBarPrefab.GetComponent<HpBar>().UpdateHp(health, maxHealth);

        if (health <= 0)
        {
            if (isDead)
                yield break;
            isDead = true;
            rigid.linearVelocity = Vector3.zero;
            rigid.angularVelocity = Vector3.zero;
            hpBarPrefab.SetActive(false);
            isOnDamage = false;
            OnDamageColor(false);
            yield return StartCoroutine(Co_Delay(.5f));
            this.gameObject.SetActive(false);
        }
        else
        {
            if (bossAttack)
                rigid.AddForce(transform.forward * -15, ForceMode.Impulse);

            yield return StartCoroutine(Co_Delay(1f));

            OnDamageColor(false);
            isOnDamage = false;

            if (bossAttack)
                rigid.linearVelocity = Vector3.zero;
        }
    }
}

