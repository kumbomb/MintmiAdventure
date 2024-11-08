using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class SubPlayer : SubPlayerParent
{
    [SerializeField] Transform[] bulletPos;
    public BehaviourSetting behaviourSetting;
    public ItemHolder itemHolder;
    [SerializeField] float checkDist = 0f;   

    private float AniSpeed
    {
        get
        {
            return anim.GetFloat("Speed");
        }
        set
        {
            anim.SetFloat("Speed", value);
        }
    }
    private int AniWeaponBehaviourID
    {
        get
        {
            return behaviourSetting.HasHand ? (int)anim.GetFloat("AttackID") : 1;
        }
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
        get
        {
            return anim.GetBool("OnGround");
        }
        set
        {
            anim.SetBool("OnGround", value);
        }
    }
    public bool Attacking
    {
        get
        {
            return AniAttack1 || AniAttack2;
        }
    }
    private bool AniAttack1
    {
        get
        {
            return anim.GetBool("Attack1");
        }
        set
        {
            anim.SetBool("Attack1", value);
        }
    }
    private bool AniAttack2
    {
        get
        {
            return anim.GetBool("Attack2");
        }
        set
        {
            anim.SetBool("Attack2", value);
        }
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
    }

    void Update()
    {
        if (GameManager.instance.playerScript != null && GameManager.instance.playerScript.isDead)
            return;
        miniMapTop.transform.position = new Vector3(transform.position.x, miniMapTop.transform.position.y, transform.position.z);
        miniMapTop.transform.rotation = Quaternion.Euler(90, 0, 0);
            
        CheckDistfromPlayer();
        FindingEnemy();
        Attack();
    }

    void FixedUpdate()
    {
        if (GameManager.instance.playerScript != null && GameManager.instance.playerScript.isDead)
            return;
        FreezeRotation();
        StopToWall();
    }
    
    public void CheckDistfromPlayer()
    {
        //1. 플레이어와 서포터의 거리 측정
        var headingToPlayerDist = Vector3.Distance(transform.position, GameManager.instance.nowPlayerCharacter.transform.position);
        var headingToPlayerVec = transform.position - GameManager.instance.nowPlayerCharacter.transform.position;

        //플레이어랑 서포터의 거리 <-> max거리 
        if(headingToPlayerDist < checkDist)
        {
            //범위내 플레이어가 있으면 추적하지 않는다 
            //공격 사정거리에 들어가는지 
            if (atkTarget != null && !atkTarget.GetComponent<Enemy>().isDead)
            {
                var headingToEnemyDist = Vector3.Distance(transform.position, atkTarget.transform.position);
                var headingToEnemyVec = transform.position - atkTarget.transform.position;

                Quaternion rot = Quaternion.LookRotation(headingToEnemyVec.normalized);
                transform.rotation = rot;
                //공격 사정거리에 들어왔으면
                if (headingToEnemyDist < nowEquipWeapon.weaponSetInfo.atkRadius)
                {
                    ToggleChaseState(false);
                    rigid.velocity = Vector3.zero;                    
                }
                //공격 사정거리에 들어오지 않았으면 
                //추적
                else
                {
                    ToggleChaseState(true);
                    nav.SetDestination(atkTarget.transform.position);
                }
            }
            else
            {
                ToggleChaseState(false);
                Quaternion rot = Quaternion.LookRotation(headingToPlayerVec.normalized);
                transform.rotation = rot;
                rigid.velocity = Vector3.zero;
            }
        }
        else
        {
            //범위내 플레이어가 없으면 플레이어 위치를 따라간다
            ToggleChaseState(true);
            atkTarget = null;
            Quaternion rot = Quaternion.LookRotation(headingToPlayerVec.normalized);
            transform.rotation = rot;
            nav.SetDestination(chasingTarget.position);
        }

    }

    void ToggleChaseState(bool flag)
    {
        isChasing = flag;
        nav.isStopped = !isChasing;
        AniSpeed = isChasing ? 1f : 0f;
    }

    void ChangeFocus()
    {
        if (isChasing)
            viewVector = (transform.position - chasingTarget.position).normalized;
        else
        {
            if (isExistTarget && atkTarget != null)
                viewVector = (transform.position - atkTarget.transform.position).normalized;
            else
                viewVector = (transform.position - chasingTarget.position).normalized;
        }
        Quaternion rot = Quaternion.LookRotation(viewVector);
        transform.rotation = rot;
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
        var headingToEnemyVec = transform.position - atkTarget.transform.position;

        if(headingToEnemyDist > nowEquipWeapon.weaponSetInfo.atkRadius)
        {
            atkCnt = 0;
            anim.SetBool("Attack1", false);
            anim.SetBool("Attack2", false);
            fireDelay = 0f;
            return;
        }

        Quaternion rot = Quaternion.LookRotation(headingToEnemyVec.normalized);
        transform.rotation = rot;

        fireDelay += Time.deltaTime;

        isFireReady = nowEquipWeapon.weaponSetInfo.rate < fireDelay;

        if (isFireReady)
        {
            if(atkCnt % 3 == 2)
            {
                if(nowEquipWeapon.weaponSetInfo.attackType == AttackType.Range)
                    nowEquipWeapon.UseWeapon(bulletPos[1]);
                else
                    nowEquipWeapon.UseWeapon();

                anim.SetBool("Attack1",false);
                anim.SetBool("Attack2",true);
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

            if (nowEquipWeapon.weaponSetInfo.attackType == AttackType.Range && nowEquipWeapon.currentAmmo <= 0)
                nowEquipWeapon.currentAmmo = nowEquipWeapon.maxAmmo;
        }
        base.Attack();
    }

    public override void FindingEnemy()
    {
        if (isChasing || (atkTarget != null && !atkTarget.GetComponent<Enemy>().isDead))
            return;

        detectRadius = nowEquipWeapon.weaponSetInfo.detectRadius;
        Collider[] _target = Physics.OverlapSphere(transform.position, detectRadius, LayerMask.GetMask("Enemy"));

        if (GameManager.instance.RetRemainEnemyCnt() <= 0 || _target.Length <= 0)
        {
            isExistTarget = false;
            atkTarget = null;
            viewVector = transform.position;
            dist = -1;
            return;
        }
        isExistTarget = true;
        if(atkTarget != null && atkTarget.GetComponent<Enemy>().isDead)
        {
            atkTarget = null;
            viewVector = transform.position;
            dist = -1;
        }
        int ii = 0;
        for (int i = 0; i < _target.Length; i++)
        {
            Vector3 _dist = transform.position - _target[i].transform.position;
            float len = _dist.sqrMagnitude;

            if (dist == -1 || dist >= len)
            {
                dist = len;
                viewVector = _dist;
                atkTarget = _target[i].gameObject;
                ii = i;
            }
        }
        moveVector = viewVector.normalized;
        ToggleChaseState(true);
        Quaternion rot = Quaternion.LookRotation(moveVector);
        transform.rotation = rot;

        base.FindingEnemy();
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Floor"))
            AniOnGround = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if(other.CompareTag("EnemyBullet"))
        {
            if (other.GetComponent<Rigidbody>() != null)
                Destroy(other.gameObject);

            if (isOnDamage)
                return;

            bool bossAttack = other.name.Equals("BossMeleeArea");

            Bullet enemyBullet = other.GetComponent<Bullet>();

            StartCoroutine(OnDamage(bossAttack, enemyBullet.damage));
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
            rigid.velocity = Vector3.zero;
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
                rigid.velocity = Vector3.zero;
        }
    }
}
