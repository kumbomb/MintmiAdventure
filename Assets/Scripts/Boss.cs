using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Boss : Enemy
{
    [Header("JoyStick Move")]
    [SerializeField] Joystick joyStick;
    [SerializeField] GameObject Missile;
    [SerializeField] Transform MissilePosList_1;
    [SerializeField] Transform MissilePosList_2;
    public bool isLookPlayer;
    [SerializeField] float waitCheckMotionTime = 0.1f;
    //플레이어 이동위치 예측 
    Vector3 lookAtVector;
    //타운트 위치 
    Vector3 tauntVector;

    // Start is called before the first frame update
    void Start()
    {
        joyStick = GameObject.Find("JoystickTouchArea").GetComponent<Joystick>();
        nav.isStopped = true;
        Invoke("StartPattern", 2f);
        //StartCoroutine(Co_MotionCheck());
    }

    // Update is called once per frame
    void Update()
    {
        if (isDead || GameManager.instance.nowGameResultState != GameResultState.None)
        {
            StopAllCoroutines();
            return;
        }

        if(isLookPlayer)
        {
            float h = joyStick.inputHorizontal();//Input.GetAxisRaw("Horizontal");
            float v = joyStick.inputVertical();//Input.GetAxisRaw("Vertical");
            lookAtVector = new Vector3(h, 0, v) * 5f;
            transform.LookAt(target.position + lookAtVector);
        }
        else
        {
            nav.SetDestination(tauntVector);
        }
    }

    void StartPattern()
    {
        StartCoroutine(Co_MotionCheck());
    }

    IEnumerator Co_MotionCheck()
    {
        yield return StartCoroutine(Co_Delay(waitCheckMotionTime));
       // yield return new WaitForSeconds(waitCheckMotionTime);

        int randAction = Random.Range(0, 5);
        switch (randAction)
        {
            //미사일발사
            case 0:
            case 1:
                {
                    StartCoroutine(Co_MissileShot());
                }
                break;
            //돌 굴리기
            case 2:
            case 3:
                {

                    StartCoroutine(Co_RollingRock());
                }
                break;
            //점프 공격
            case 4:
                {
                    StartCoroutine(Co_Taunt());
                }
                break;
        }
    }

    IEnumerator Co_MissileShot()
    {
        anim.SetTrigger("doShot");

        yield return StartCoroutine(Co_Delay(.2f));
        //yield return new WaitForSeconds(0.2f);

        GameObject instantBullet_1 = ObjectPool.instance.PopFromPool("BossMissile", ObjectPool.instance.MonsterBulletPool);//Instantiate(Missile, MissilePosList_1.position, MissilePosList_1.rotation);
        instantBullet_1.transform.position = MissilePosList_1.position;
        instantBullet_1.transform.rotation = MissilePosList_1.rotation;
        instantBullet_1.SetActive(true);
        BossBullet bossBullet_1 = instantBullet_1.GetComponent<BossBullet>();
        bossBullet_1.SetNav();
        bossBullet_1.target = target;

        yield return StartCoroutine(Co_Delay(.3f));
        //yield return new WaitForSeconds(0.3f);

        GameObject instantBullet_2 = ObjectPool.instance.PopFromPool("BossMissile", ObjectPool.instance.MonsterBulletPool); //Instantiate(Missile, MissilePosList_2.position, MissilePosList_2.rotation);
        instantBullet_2.transform.position = MissilePosList_2.position;
        instantBullet_2.transform.rotation = MissilePosList_2.rotation;
        instantBullet_2.SetActive(true);
        BossBullet bossBullet_2 = instantBullet_2.GetComponent<BossBullet>();
        bossBullet_2.SetNav();
        bossBullet_2.target = target;

        yield return StartCoroutine(Co_Delay(2f));
        //yield return new WaitForSeconds(2f);

        StartCoroutine(Co_MotionCheck());
    }

    IEnumerator Co_RollingRock()
    {
        isLookPlayer = false;
        anim.SetTrigger("doBigShot");

        bullet = ObjectPool.instance.PopFromPool("BossRock", ObjectPool.instance.MonsterBulletPool);
        bullet.transform.position = transform.position;
        bullet.transform.rotation = transform.rotation;
        bullet.SetActive(true);
        //Instantiate(bullet, transform.position, transform.rotation);

        yield return StartCoroutine(Co_Delay(3f));
        //yield return new WaitForSeconds(3f);
        isLookPlayer = true;
        StartCoroutine(Co_MotionCheck());
    }

    IEnumerator Co_Taunt()
    {
        tauntVector = target.position + lookAtVector;

        isLookPlayer = false;
        nav.isStopped = false;
        boxCollider.enabled = false;
        anim.SetTrigger("doTaunt");
        yield return StartCoroutine(Co_Delay(1.5f));
        //yield return new WaitForSeconds(1.5f);
        MeleeArea.enabled = true;
        yield return StartCoroutine(Co_Delay(.5f));
       // yield return new WaitForSeconds(0.5f);
        MeleeArea.enabled = false;

        isLookPlayer = true;
        boxCollider.enabled = true;
        nav.isStopped = true;
        StartCoroutine(Co_MotionCheck());
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
}
