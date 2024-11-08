using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class Player : MonoBehaviour
{
    [Header("=======Character=======")]
    public Animator anim;
    public Rigidbody rigid;
    SkinnedMeshRenderer[] playerMesh;
    public GameObject miniMapTop;
    [SerializeField] Transform rightHand;
    public Transform[] chasePos;

    [Header("=======HP=======")]
    public int health;
    public int maxHealth;
    public GameObject hpBarPrefab;
    Canvas hpCanvas;     

    public Vector3 hpBarOffSet = new Vector3(0f, 2.2f, 0f);

    [Header("=======Move=======")]
    [SerializeField] Joystick joyStick;
    [SerializeField] float speed;
    [SerializeField] float defaultSpeed;
    //[SerializeField] int jumpForce;
    float hAxis;
    float vAxis;

    //Action
    [HideInInspector] public bool walkInput;
    [HideInInspector] public bool jumpInput;
    [HideInInspector] public bool isJump;
    [HideInInspector] public bool isDodge;
    [HideInInspector] public bool isSwap;
    [HideInInspector] public bool isFireReady;
    [HideInInspector] public bool isBoarder;
    [HideInInspector] public bool isOnDamage;
    [HideInInspector] public bool isReload;
    [HideInInspector] public bool isSettingFocus;
    [HideInInspector] public bool isThrow;
    [HideInInspector] public bool isDead;
    [HideInInspector] public bool isExistTarget;
    int throwInput = -1;

    //Vector
    Vector3 moveVec;
    Vector3 dodgeVec;
    Vector3 lookVec;
    Vector3 lookDestVec;
    Vector3 destVec;

     [Header("=======Weapon=======")]
    public GameObject[] weapons;
    public Weapon nowEquipWeapon;
    public WeaponType equipType;
    public Transform[] bulletPos;
    [HideInInspector] public float detectRadius;
    [HideInInspector] public float fireDelay;
    //획득할 아이템
    GameObject NearObj;

    [Header("=======Grenades=======")]
    [SerializeField] GameObject[] Grenade_Prefab;

    int atkCnt = 0;

    private void Start()
    {
        ResetEquipWeapon();
        playerMesh = GetComponentsInChildren<SkinnedMeshRenderer>();
        joyStick = GameObject.Find("JoystickTouchArea").GetComponent<Joystick>();
        defaultSpeed = speed;
        SetHPBar();
    }

    // 캐릭터의 전체적인 움직임 관련 처리 
    void Update()
    {
        if (isDead)
            return;

        //미니맵에 자신의 위치 표현 
        miniMapTop.transform.position = new Vector3(transform.position.x, miniMapTop.transform.position.y, transform.position.z);
        miniMapTop.transform.rotation = Quaternion.Euler(90, 0, 0);


        //입력을 받아서 이동 ( 조이스틱 드래그 )
        GetInput();
        MovePlayer();

        //적이 근처에 있다면 자동으로 적에게 포커스 
        ChangePlayerFocus();
        
        //자동으로 적을 향해 공격 
        Attack();
        Reload();

        //만약 수류탄 투척이 입력되었다면 수류탄 투척 
        ThrowGrenade();

        //회피
        Dodge();
    }
    // 물리연산이 필요한 부분 
    // Physics Raycast를 활용한 벽이 있는지 체크 등 
    void FixedUpdate()
    {
        if (isDead)
            return;

        FreezeRotation();
        StopToWall();
    }

    //스스로 회전 방지
    void FreezeRotation()
    {
        rigid.angularVelocity = Vector3.zero;
    }
    //벽체크
    void StopToWall()
    {
        Vector3 rayFocus;

        if (isDodge)
            rayFocus = dodgeVec;
        else
            rayFocus = moveVec;

        isBoarder = Physics.Raycast(transform.position, rayFocus, 5f, LayerMask.GetMask("Wall"));
    }

    #region 공격 및 공격 쿨타임 관련 
    public void Attack()
    {
        if (nowEquipWeapon == null)
            return;

        if (GameManager.instance.RetRemainEnemyCnt() <= 0 || !isExistTarget)
            return;

        fireDelay += Time.deltaTime;
        isFireReady = true;
        isFireReady = nowEquipWeapon.weaponSetInfo.rate < fireDelay;

        if (isFireReady && !isJump && !isDodge && !isSwap && !isReload && !isThrow/* && isExistTarget*/)
        {
            nowEquipWeapon.UseWeapon(bulletPos[0]);
            if (nowEquipWeapon.weaponSetInfo.attackType == AttackType.Melee)
            {
                if (atkCnt % 3 == 2)
                {
                    anim.SetFloat("SwingId", 1);
                    atkCnt = 0;
                }
                else
                {
                    anim.SetFloat("SwingId", 0);
                    atkCnt++;
                }
                anim.SetTrigger("doSwing");
            }
            else
            {
                atkCnt = 0;
                anim.SetTrigger("doShot");
            }

            Debug.Log("4");
            anim.SetTrigger(nowEquipWeapon.weaponSetInfo.attackType == AttackType.Melee ? "doSwing" : "doShot");
            ResetAttackDealy();
            Debug.Log("5");
        }
        Debug.Log("6");
    }

    void ResetAttackDealy()
    {
        fireDelay = 0f;
    }

    // 재공격 쿨타임 대기
    void Reload()
    {
        if (nowEquipWeapon == null)
            return;

        if (isReload == true)
            return;

        if (nowEquipWeapon.weaponSetInfo.attackType == AttackType.Range && nowEquipWeapon.currentAmmo <= 0)
        {
            isReload = true;
            anim.SetTrigger("doReload");
            StartCoroutine(Co_Reloading());
        }
    }

    IEnumerator Co_Reloading()
    {
        yield return StartCoroutine(Co_Delay(2f));
        nowEquipWeapon.currentAmmo = nowEquipWeapon.maxAmmo;
        isReload = false;
    }
    #endregion

    #region 캐릭터 이동 방향 감지 & 실제 이동 & 캐릭터 시점 세팅
    //캐릭터 이동방향 감지
    void GetInput()
    {
        //Axis 값을 정수로 return
        hAxis = joyStick.inputHorizontal();
        vAxis = joyStick.inputVertical();

#if UNITY_EDITOR
        if (Input.GetKeyDown(KeyCode.Keypad1))
            throwInput = 0;
        else if(Input.GetKeyDown(KeyCode.Keypad2))
            throwInput = 1;
        else if (Input.GetKeyDown(KeyCode.Keypad3))
            throwInput = 2;

        jumpInput = Input.GetKeyDown(KeyCode.Space);
#endif
    }

    //캐릭터 실제 이동
    void MovePlayer()
    {
        moveVec = new Vector3(hAxis, 0, vAxis).normalized;

        if (isDodge)
            moveVec = dodgeVec;

        if (isSwap)
            moveVec = Vector3.zero;

        if(!isBoarder)
            transform.position += moveVec * speed * 1f * Time.deltaTime;

        anim.SetBool("isRun", moveVec != Vector3.zero);
    }

    //캐릭터 시점 자동으로 근처 적에게
    void ChangePlayerFocus()
    {
        lookVec = transform.position + moveVec;

        lookDestVec = (moveVec == Vector3.zero) ? Vector3.zero : moveVec;

        transform.LookAt(lookVec);
        detectRadius = nowEquipWeapon.weaponSetInfo.detectRadius;
        Collider[] _target = Physics.OverlapSphere(transform.position, detectRadius, LayerMask.GetMask("Enemy"));

        destVec = Vector3.zero;
        float dist = -1;

        if (isDodge || isReload || GameManager.instance.RetRemainEnemyCnt() <= 0 || _target.Length <= 0)
        {
            isExistTarget = false;
            return;
        }
        isExistTarget = true;
        for (int i = 0; i < _target.Length; i++)
        {
            Transform targetTf = _target[i].transform;
            Vector3 _dist = targetTf.position - transform.position;
            float len = _dist.sqrMagnitude;

            if (dist == -1 || dist >= len)
            {
                dist = len;
                destVec = _dist;
            }
        }
        transform.LookAt(transform.position + destVec);
        lookVec = transform.position + destVec;
        destVec = destVec.normalized;
        lookDestVec = new Vector3(destVec.x, 0f, destVec.z);
    }
    #endregion

    #region 회피 
    // 회피
    void Dodge()
    {
        if (moveVec == Vector3.zero)
        {
            jumpInput = false;
            return;
        }
        if (jumpInput /*&& moveVec != Vector3.zero && !isJump &&*/ &&!isDodge && !isSwap && !isReload)
        {
            speed *= 2f;
            isDodge = true;
            anim.SetTrigger("doDodge");
            dodgeVec = moveVec;
            Invoke("DodgeOut",0.6f);
        }
    }
    // 회피 종료
    void DodgeOut()
    {
        isDodge = false;
        jumpInput = false;
        speed *= 0.5f;
    }
    #endregion

    #region 아이템 획득 => 필드에 드랍되는 무기를 획득하면 무기가 교체된다
    //아이템 획득 처리
    void ResetEquipWeapon()
    {
        for (int i = 0; i < weapons.Length; i++)
            weapons[i].SetActive(false);
        equipType = (WeaponType)GameManager.instance.curPlayerWeaponNum;
        weapons[GameManager.instance.curPlayerWeaponNum].SetActive(true);
        nowEquipWeapon = weapons[GameManager.instance.curPlayerWeaponNum].GetComponent<Weapon>();
    }

    public void ChangeEquipWeapon(int num, bool flag = false)
    {
        if (nowEquipWeapon != null)
            nowEquipWeapon.gameObject.SetActive(false);
        equipType = (WeaponType)num;
        weapons[num].SetActive(true);
        nowEquipWeapon = weapons[num].GetComponent<Weapon>();

        if(!flag)
        {
            isSwap = true;
            anim.SetTrigger("doSwap");
            Invoke("ChangeEquipWeaponEnd", 0.5f);
        }
    }
    void ChangeEquipWeaponEnd()
    {
        isSwap = false;
    }

    //점프는 미획득 => 근처에 있는 Weapon 이라는 아이템 획득 처리
    void Interaction()
    {
        if (NearObj != null && !isJump && !isDodge)
        {
            if (NearObj.tag.Equals("Weapon"))
            {
                Item item = NearObj.GetComponent<Item>();
                int weaponIndex = (int)item.thisWeaponType;
                int weaponBulletNum = item.value;
                ChangeEquipWeapon(weaponIndex);
                Destroy(NearObj);
            }
        }
    }
    #endregion

    #region 수류탄 투척 
    void ThrowGrenade()
    {

        if (throwInput == -1)
            return;

        if ((throwInput != -1) && !isSwap && !isJump && !isDodge && !isReload && !isThrow)
        {
            isThrow = true;
            StartCoroutine(Co_ThrowGrenade(throwInput));
        }
    }

    IEnumerator Co_ThrowGrenade(int num)
    {
        for (int i = 0; i < weapons.Length; i++)
            weapons[i].SetActive(false);

        GameObject grenade = Instantiate(Grenade_Prefab[num], rightHand.position, rightHand.rotation);
        grenade.transform.parent = rightHand;
        Rigidbody rigidGrenade = grenade.GetComponent<Rigidbody>();

        Vector3 throwVector = lookDestVec;

        anim.SetTrigger("doThrow");

        yield return StartCoroutine(Co_Delay(0.3f));
        grenade.transform.parent = null;
        rigidGrenade.velocity = new Vector3(throwVector.x * 20f, 10f, throwVector.z * 20f);
        rigidGrenade.AddTorque(Vector3.forward * 15, ForceMode.Impulse);

        yield return StartCoroutine(Co_Delay(1f));
        ChangeEquipWeapon((int)(nowEquipWeapon.weaponSetInfo.weaponType), true);
        throwInput = -1;
        isThrow = false;
    }
    #endregion


    //바닥에 닿는 거 충돌 확인 => 점프가 가능할 경우 바닥에 있어야만 점프가 가능 / 현재 미구현
    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.tag.Equals("Floor"))
        {
            Invoke("ResetJump",0.2f);
            //isJump = false;
            //anim.SetBool("isJump", isJump);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        //아이템 획득 처리
        if (other.CompareTag("Weapon"))
            NearObj = other.gameObject;
        //아이템 획득 처리
        else if (other.CompareTag("Item"))
        {
            Item item = other.gameObject.GetComponent<Item>();
            switch (item.thisItemType)
            {
                case ItemType.GoldCoin:
                    GameManager.instance.GetCoin(item.value);
                    break;
            }
            other.gameObject.SetActive(false);
        }
        //적한테 피격될때
        else if (other.CompareTag("EnemyBullet"))
        {
            if (other.GetComponent<Rigidbody>() != null)
                Destroy(other.gameObject);

            if (isOnDamage)
                return;

            bool bossAttack = other.name.Equals("BossMeleeArea");
            
            Bullet enemyBullet = other.GetComponent<Bullet>();

            StartCoroutine(OnDamage(bossAttack, enemyBullet.damage));
        }
        //스테이지 클리어 후 포탈에 진입시
        else if(other.CompareTag("NextPortal"))
            GameManager.instance.GoLobby();
    }
    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Weapon"))
        {
            NearObj = null;
        }
    }

    #region 체력 바 및 피격 처리
    public void SetHPBar()
    {
        hpCanvas = GameObject.Find("Canvas_HP").GetComponent<Canvas>();
        hpBarPrefab = Instantiate(hpBarPrefab, hpCanvas.transform);
        hpBarPrefab.SetActive(true);
        var hpbarScript = hpBarPrefab.GetComponent<HpBar>();
        hpbarScript.targetTr = this.gameObject.transform;
        hpbarScript.offset = hpBarOffSet;
        hpbarScript.setHpBar = true;
    }
    public void ToggleHpBar()
    {
        hpBarPrefab.SetActive(!GameManager.instance.isPause);
    }

    //피격 처리
    IEnumerator OnDamage(bool bossAttack, int damage)
    {
        if (GameManager.instance.nowGameResultState != GameResultState.None || isDodge)
            yield break;

        isOnDamage = true;
        OnDamageColor(true);

        health -= 0;//damage;

        hpBarPrefab.GetComponent<HpBar>().UpdateHp(health, maxHealth);

        if (health <= 0)
        {
            isDead = true;
            moveVec = Vector3.zero;
            rigid.velocity = Vector3.zero;
            rigid.angularVelocity = Vector3.zero;
            GameManager.instance.GameEnd(GameResultState.Lose);
            anim.SetTrigger("doDie");
            isOnDamage = false;
            OnDamageColor(false);
        }
        else
        {
            if (bossAttack)
            {
                rigid.AddForce(transform.forward * -15, ForceMode.Impulse);
            }

            yield return StartCoroutine(Co_Delay(1f));
            OnDamageColor(false);
            isOnDamage = false;

            if (bossAttack)
            {
                rigid.velocity = Vector3.zero;
            }
        }
    }
    
    //피격시 색상 변경
    void OnDamageColor(bool flag)
    {
        if(flag)
        {
            foreach(SkinnedMeshRenderer mesh in playerMesh)
            {
                mesh.material.color = Color.red;
            }
        }
        else
        {
            foreach (SkinnedMeshRenderer mesh in playerMesh)
            {
                mesh.material.color = Color.white;
            }
        }
    }
    #endregion

    public void ToggleDodge()
    {
        jumpInput = true;
    }

    public void ToggleThrow(int num)
    {
        if (throwInput != -1)
            return;
        throwInput = num;
    }
    public int RetThrowState()
    {
        return throwInput;
    }

    //딜레이 코루틴 
    public IEnumerator Co_Delay(float _delaytime)
    {
        float nowTime = 0f;
        while(nowTime < _delaytime)
        {
            nowTime += Time.deltaTime;
            yield return null;
        }
    }
}
