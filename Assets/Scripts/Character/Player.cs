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
    [SerializeField] PlayerStatData playerData;
    public int health;
    public int maxHealth;
    public GameObject hpBarPrefab;
    Canvas hpCanvas;
    HpBar hpBar;
    public Vector3 hpBarOffSet = new Vector3(0f, 2.2f, 0f);

    [Header("=======Move=======")]
    [SerializeField] Joystick joyStick;
    [SerializeField] float speed;
    [SerializeField] float defaultSpeed;
    float hAxis;
    float vAxis;

    [Header("=======Survival Skill=======")]
    [SerializeField] float survivalSkillCooldown = 12f;
    [SerializeField] float survivalSkillDuration = 2.5f;
    [SerializeField] float survivalSkillSpeedMultiplier = 1.25f;
    [SerializeField] float survivalSkillDamageReduction = 0.6f;
    [SerializeField] int survivalSkillHealAmount = 10;
    bool isSurvivalSkillActive;
    float survivalSkillTimer;
    float survivalSkillCooldownTimer;

    [HideInInspector] public bool walkInput;
    [HideInInspector] public bool jumpInput;
    [HideInInspector] public bool isJump;
    [HideInInspector] public bool isSwap;
    [HideInInspector] public bool isFireReady;
    [HideInInspector] public bool isBoarder;
    [HideInInspector] public bool isOnDamage;
    [HideInInspector] public bool isSettingFocus;
    [HideInInspector] public bool isThrow;
    [HideInInspector] public bool isDead;
    [HideInInspector] public bool isExistTarget;
    int throwInput = -1;

    Vector3 moveVec;
    Vector3 lookVec;
    Vector3 lookDestVec;
    Vector3 destVec;
    Transform currentTarget;

    [Header("=======Weapon=======")]
    public GameObject[] weapons;
    public Weapon nowEquipWeapon;
    public WeaponType equipType;
    public Transform[] bulletPos;
    [HideInInspector] public float detectRadius;
    [HideInInspector] public float fireDelay;
    GameObject NearObj;

    [Header("=======Grenades=======")]
    [SerializeField] GameObject[] Grenade_Prefab;

    readonly Collider[] targetBuffer = new Collider[32];
    float targetRefreshTimer;
    const float TargetRefreshInterval = 0.12f;
    int atkCnt = 0;

    void Start()
    {
        ApplyPlayerData();
        ResetEquipWeapon();
        playerMesh = GetComponentsInChildren<SkinnedMeshRenderer>();
        joyStick = GameObject.Find("JoystickTouchArea").GetComponent<Joystick>();
        defaultSpeed = speed;
        SetHPBar();
    }


    void ApplyPlayerData()
    {
        if (playerData == null)
            return;

        maxHealth = playerData.maxHealth;
        health = Mathf.Clamp(health <= 0 ? maxHealth : health, 1, maxHealth);
        speed = playerData.moveSpeed;
        defaultSpeed = speed;
        survivalSkillCooldown = playerData.survivalSkillCooldown;
        survivalSkillDuration = playerData.survivalSkillDuration;
        survivalSkillSpeedMultiplier = playerData.survivalSkillSpeedMultiplier;
        survivalSkillDamageReduction = playerData.survivalSkillDamageReduction;
        survivalSkillHealAmount = playerData.survivalSkillHealAmount;
    }
    void Update()
    {
        if (isDead)
            return;

        UpdateSurvivalSkillState();
        UpdateMiniMapMarker();
        GetInput();
        MovePlayer();
        ChangePlayerFocus();
        Attack();
        ThrowGrenade();
    }

    void FixedUpdate()
    {
        if (isDead)
            return;

        FreezeRotation();
        StopToWall();
    }

    void UpdateMiniMapMarker()
    {
        if (miniMapTop == null)
            return;

        Transform markerTransform = miniMapTop.transform;
        markerTransform.position = new Vector3(transform.position.x, markerTransform.position.y, transform.position.z);
        markerTransform.rotation = Quaternion.Euler(90f, 0f, 0f);
    }

    void FreezeRotation()
    {
        rigid.angularVelocity = Vector3.zero;
    }

    void StopToWall()
    {
        Vector3 rayFocus = moveVec == Vector3.zero ? transform.forward : moveVec;
        isBoarder = Physics.Raycast(transform.position, rayFocus, 5f, LayerMask.GetMask("Wall"));
    }

    public void Attack()
    {
        if (nowEquipWeapon == null || currentTarget == null)
            return;

        if (GameManager.instance.RetRemainEnemyCnt() <= 0 || !isExistTarget)
            return;

        fireDelay += Time.deltaTime;
        isFireReady = nowEquipWeapon.weaponSetInfo.rate < fireDelay;

        if (!isFireReady || isJump || isSwap || isThrow)
            return;

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

        fireDelay = 0f;
    }

    void GetInput()
    {
        hAxis = joyStick.inputHorizontal();
        vAxis = joyStick.inputVertical();

#if UNITY_EDITOR
        if (Input.GetKeyDown(KeyCode.Keypad1))
            throwInput = 0;
        else if (Input.GetKeyDown(KeyCode.Keypad2))
            throwInput = 1;
        else if (Input.GetKeyDown(KeyCode.Keypad3))
            throwInput = 2;
#endif
    }

    void MovePlayer()
    {
        moveVec = new Vector3(hAxis, 0, vAxis).normalized;

        if (isSwap)
            moveVec = Vector3.zero;

        if (!isBoarder)
            transform.position += moveVec * speed * Time.deltaTime;

        anim.SetBool("isRun", moveVec != Vector3.zero);
    }

    void ChangePlayerFocus()
    {
        lookVec = transform.position + moveVec;
        lookDestVec = moveVec == Vector3.zero ? Vector3.zero : moveVec;

        if (moveVec != Vector3.zero)
            transform.LookAt(lookVec);

        if (nowEquipWeapon == null)
        {
            currentTarget = null;
            isExistTarget = false;
            return;
        }

        targetRefreshTimer -= Time.deltaTime;
        if (targetRefreshTimer > 0f && currentTarget != null)
        {
            FaceCurrentTarget();
            return;
        }

        targetRefreshTimer = TargetRefreshInterval;
        detectRadius = nowEquipWeapon.weaponSetInfo.detectRadius;
        int targetCount = Physics.OverlapSphereNonAlloc(transform.position, detectRadius, targetBuffer, LayerMask.GetMask("Enemy"));

        destVec = Vector3.zero;
        float nearestDistance = float.MaxValue;
        currentTarget = null;

        if (GameManager.instance.RetRemainEnemyCnt() <= 0 || targetCount <= 0)
        {
            isExistTarget = false;
            return;
        }

        for (int i = 0; i < targetCount; i++)
        {
            Collider candidate = targetBuffer[i];
            if (candidate == null || !candidate.gameObject.activeInHierarchy)
                continue;

            Vector3 targetDist = candidate.transform.position - transform.position;
            float sqrDistance = targetDist.sqrMagnitude;
            if (sqrDistance >= nearestDistance)
                continue;

            nearestDistance = sqrDistance;
            destVec = targetDist;
            currentTarget = candidate.transform;
        }

        isExistTarget = currentTarget != null;
        if (isExistTarget)
            FaceCurrentTarget();
    }

    void FaceCurrentTarget()
    {
        if (currentTarget == null)
        {
            isExistTarget = false;
            return;
        }

        destVec = currentTarget.position - transform.position;
        if (destVec.sqrMagnitude > detectRadius * detectRadius)
        {
            currentTarget = null;
            isExistTarget = false;
            return;
        }

        transform.LookAt(transform.position + destVec);
        lookVec = transform.position + destVec;
        Vector3 normalizedDirection = destVec.normalized;
        lookDestVec = new Vector3(normalizedDirection.x, 0f, normalizedDirection.z);
        isExistTarget = true;
    }

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
        currentTarget = null;
        targetRefreshTimer = 0f;

        if (!flag)
        {
            isSwap = true;
            anim.SetTrigger("doSwap");
            Invoke(nameof(ChangeEquipWeaponEnd), 0.5f);
        }
    }

    void ChangeEquipWeaponEnd()
    {
        isSwap = false;
    }

    void Interaction()
    {
        if (NearObj != null && !isJump)
        {
            if (NearObj.CompareTag("Weapon"))
            {
                Item item = NearObj.GetComponent<Item>();
                int weaponIndex = (int)item.thisWeaponType;
                ChangeEquipWeapon(weaponIndex);
                Destroy(NearObj);
            }
        }
    }

    void ThrowGrenade()
    {
        if (throwInput == -1)
            return;

        if (!isSwap && !isJump && !isThrow)
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
        rigidGrenade.linearVelocity = new Vector3(throwVector.x * 20f, 10f, throwVector.z * 20f);
        rigidGrenade.AddTorque(Vector3.forward * 15, ForceMode.Impulse);

        yield return StartCoroutine(Co_Delay(1f));
        ChangeEquipWeapon((int)nowEquipWeapon.weaponSetInfo.weaponType, true);
        throwInput = -1;
        isThrow = false;
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Floor"))
            Invoke(nameof(ResetJump), 0.2f);
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Weapon"))
        {
            NearObj = other.gameObject;
        }
        else if (other.CompareTag("Item"))
        {
            Item item = other.gameObject.GetComponent<Item>();
            if (item.thisItemType == ItemType.GoldCoin)
                GameManager.instance.GetCoin(item.value);
            other.gameObject.SetActive(false);
        }
        else if (other.CompareTag("EnemyBullet"))
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
        else if (other.CompareTag("NextPortal"))
        {
            GameManager.instance.GoLobby();
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Weapon"))
            NearObj = null;
    }

    public void SetHPBar()
    {
        hpCanvas = GameObject.Find("Canvas_HP").GetComponent<Canvas>();
        hpBarPrefab = Instantiate(hpBarPrefab, hpCanvas.transform);
        hpBarPrefab.SetActive(true);
        hpBar = hpBarPrefab.GetComponent<HpBar>();
        hpBar.targetTr = gameObject.transform;
        hpBar.offset = hpBarOffSet;
        hpBar.setHpBar = true;
    }

    public void ToggleHpBar()
    {
        hpBarPrefab.SetActive(!GameManager.instance.isPause);
    }

    IEnumerator OnDamage(bool bossAttack, int damage)
    {
        if (GameManager.instance.nowGameResultState != GameResultState.None)
            yield break;

        isOnDamage = true;
        OnDamageColor(true);

        int finalDamage = isSurvivalSkillActive
            ? Mathf.Max(1, Mathf.CeilToInt(damage * (1f - survivalSkillDamageReduction)))
            : damage;
        health -= finalDamage;
        if (hpBar != null)
            hpBar.UpdateHp(health, maxHealth);

        if (health <= 0)
        {
            isDead = true;
            moveVec = Vector3.zero;
            rigid.linearVelocity = Vector3.zero;
            rigid.angularVelocity = Vector3.zero;
            GameManager.instance.GameEnd(GameResultState.Lose);
            anim.SetTrigger("doDie");
            isOnDamage = false;
            OnDamageColor(false);
        }
        else
        {
            if (bossAttack)
                rigid.AddForce(transform.forward * -10f, ForceMode.Impulse);

            yield return StartCoroutine(Co_Delay(0.5f));
            RefreshSkillTint();
            isOnDamage = false;

            if (bossAttack)
                rigid.linearVelocity = Vector3.zero;
        }
    }

    void OnDamageColor(bool flag)
    {
        foreach (SkinnedMeshRenderer mesh in playerMesh)
            mesh.material.color = flag ? Color.red : (isSurvivalSkillActive ? new Color(0.5f, 1f, 1f) : Color.white);
    }

    void RefreshSkillTint()
    {
        foreach (SkinnedMeshRenderer mesh in playerMesh)
            mesh.material.color = isSurvivalSkillActive ? new Color(0.5f, 1f, 1f) : Color.white;
    }

    void UpdateSurvivalSkillState()
    {
        if (survivalSkillCooldownTimer > 0f)
            survivalSkillCooldownTimer -= Time.deltaTime;

        if (!isSurvivalSkillActive)
            return;

        survivalSkillTimer -= Time.deltaTime;
        if (survivalSkillTimer <= 0f)
            EndSurvivalSkill();
    }

    public void ToggleDodge()
    {
        ActivateSurvivalSkill();
    }

    public void ActivateSurvivalSkill()
    {
        if (isDead || isThrow || isSwap || isSurvivalSkillActive || survivalSkillCooldownTimer > 0f)
            return;

        isSurvivalSkillActive = true;
        survivalSkillTimer = survivalSkillDuration;
        survivalSkillCooldownTimer = survivalSkillCooldown;
        speed = defaultSpeed * survivalSkillSpeedMultiplier;
        health = Mathf.Min(maxHealth, health + survivalSkillHealAmount);
        if (hpBar != null)
            hpBar.UpdateHp(health, maxHealth);
        RefreshSkillTint();
    }

    void EndSurvivalSkill()
    {
        isSurvivalSkillActive = false;
        speed = defaultSpeed;
        RefreshSkillTint();
    }

    public bool CanUseSurvivalSkill()
    {
        return !isSurvivalSkillActive && survivalSkillCooldownTimer <= 0f && !isDead;
    }

    public float GetSurvivalSkillCooldownRemaining()
    {
        return Mathf.Max(0f, survivalSkillCooldownTimer);
    }

    public string GetCombatStatusText()
    {
        if (isSurvivalSkillActive)
            return "Survival Skill : Active";
        if (CanUseSurvivalSkill())
            return "Survival Skill : Ready";
        return "Survival Skill : " + Mathf.CeilToInt(GetSurvivalSkillCooldownRemaining()) + "s";
    }


    public void ApplyMaxHealthUpgrade(int value)
    {
        maxHealth += Mathf.Max(1, value);
        health = Mathf.Min(maxHealth, health + value);
        if (hpBar != null)
            hpBar.UpdateHp(health, maxHealth);
    }

    public void ApplyMoveSpeedUpgrade(float value)
    {
        float increase = Mathf.Max(0f, value);
        speed += increase;
        defaultSpeed += increase;
        if (isSurvivalSkillActive)
            speed = defaultSpeed * survivalSkillSpeedMultiplier;
    }

    public void RestoreHealth(int value)
    {
        health = Mathf.Min(maxHealth, health + Mathf.Max(0, value));
        if (hpBar != null)
            hpBar.UpdateHp(health, maxHealth);
    }
    void ResetJump()
    {
        isJump = false;
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

    public IEnumerator Co_Delay(float delayTime)
    {
        float nowTime = 0f;
        while (nowTime < delayTime)
        {
            nowTime += Time.deltaTime;
            yield return null;
        }
    }
}

