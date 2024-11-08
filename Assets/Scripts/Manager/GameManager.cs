using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System;

public enum GameState
{
    None,
    Ready,
    BattleReady,
    BattleStart,
    Battle,
    BattleEnd,
    Pause,
    Result,
}

public enum GameResultState
{
    None,
    Win,
    Lose,
}

public class GameManager : MonoBehaviour
{
    [Header("=======Manager=======")]
    public static GameManager instance;
    public LevelManager levelManager;

    [Header("=======Camera=======")]
    //[SerializeField] GameObject cam_MiniMap;
    [SerializeField] GameObject cam_Follow;

    [Header("=======Canvas=======")]
    [SerializeField] GameObject hpCanvas;
    [SerializeField] GameObject waveText;
    [SerializeField] GameObject ResultPopup;
    [SerializeField] GameObject PausePopup;
    [SerializeField] Text currentBullet;
    [SerializeField] Text currentGoldText;
    [SerializeField] Text currentWave;
    [SerializeField] Text remainEnemyCnt;
    [SerializeField] GameObject dodgeBtn;
    [SerializeField] GameObject pauseBtn;
    [SerializeField] GameObject atkBtn;

    [Header("=======Player=======")]
    [SerializeField] Transform playerParentPos;
    public GameObject[] playerPrefabList;
    public GameObject nowPlayerCharacter;
    public Player playerScript;
    public GameObject[] subPlayerObjList;
    public List<int> subPlayerIdxList = new List<int>();
    public List<GameObject> subPlayerCharacter = new List<GameObject>();

    [Header("=======Game State=======")]
    public GameState nowGameState = GameState.None;
    public GameResultState nowGameResultState = GameResultState.None;

    public bool isStartLevel;
    public bool isWaitStart;
    public bool startNextWave = false;
    public bool isUsingRefreshUI = false;
    public bool isPause = false;

    [SerializeField] float currentIntervalTime = 0f;
    [SerializeField] int nowWave = 0;
    [SerializeField] float maxIntervalTime = 5f;

    public int nowStageNum = 0;
    public int nowMonsterCnt = 0;
    public int killMonsterCnt = 0;

    public int nowGetCoin = 0;

    public int curPlayerWeaponNum = 0;

    private void Awake()
    {
        instance = this;
        DontDestroyOnLoad(this);
    }

    void Start()
    {
        isWaitStart = false;
        Application.targetFrameRate = 60;

        if (!Application.isEditor)
            Screen.sleepTimeout = SleepTimeout.NeverSleep;

        //cam_Follow.SetActive(false);
        //waveText.SetActive(false);
        //ResetData();
    }

    // Update is called once per frame
    void Update()
    {
        switch (nowGameState)
        {
            case GameState.None:
                {
                   
                }
                break;
                //인게임 씬으로 넘어올때 상태값
            case GameState.BattleReady:
                {
                    if (nowPlayerCharacter != null)
                    {
                        if (isStartLevel || !isWaitStart)
                            return;
                        //ResetData();
                        isStartLevel = true;
                        cam_Follow.SetActive(true);
                        StartCoroutine(Co_StartLevel());
                    }
                    else
                    {
                        levelManager.InitLevel();
                        nowPlayerCharacter = Instantiate(playerPrefabList[0], Vector3.zero, Quaternion.identity);
                        nowPlayerCharacter.transform.parent = playerParentPos;
                        if (playerScript == null)
                            playerScript = nowPlayerCharacter.GetComponent<Player>();
                        cam_Follow.GetComponent<FollowCamera>().SetTarget(nowPlayerCharacter.transform);
                        playerScript.ChangeEquipWeapon(curPlayerWeaponNum, true);

                        subPlayerCharacter.Clear();

                        for (int i = 0; i < subPlayerIdxList.Count; i++)
                        {
                            subPlayerCharacter.Add(
                                Instantiate(subPlayerObjList[subPlayerIdxList[i]], 
                                new Vector3(5f, 0f, 0f), 
                                Quaternion.identity));
                            subPlayerCharacter[i].transform.parent = playerParentPos;
                            subPlayerCharacter[i].GetComponent<SubPlayerParent>().supporterNum = i;
                        }
                    }
                }
                break;
            case GameState.Ready:
                {
                    if(!waveText.activeSelf)
                    {
                        waveText.SetActive(true);
                    }

                    waveText.GetComponent<Text>().text = "Wait Next Wave" + "\n" +(int)(maxIntervalTime - currentIntervalTime);

                    currentIntervalTime += Time.deltaTime;

                    if (currentIntervalTime >= maxIntervalTime)
                    {
                        currentIntervalTime = 0f;
                        nowGameState = GameState.BattleStart;
                    }
                }
                break;
            case GameState.BattleStart:
                {
                    StartWave();
                }
                break;
            case GameState.Battle:
                {
                    CheckWave();
                }
                break;
            case GameState.BattleEnd:
                {
                    if(startNextWave)
                    {
                        currentIntervalTime = 0f;
                        startNextWave = false;
                        nowGameState = GameState.Ready;
                    }
                }
                break;
            case GameState.Result:
                {

                }
                break;
        }
        UpdateUIText();

        if (Input.GetKeyDown(KeyCode.Escape) 
            && SceneManager.GetActiveScene().name.Equals("Game") 
            && nowGameResultState == GameResultState.None)
        {
            if (PausePopup.activeSelf)
            {
                SetTimeScale(false);
                PausePopup.SetActive(false);
            }
            else
            {
                SetTimeScale(true);
                PausePopup.SetActive(true);
            }
        }
    }

    public void StartWave()
    {
        if(nowWave < levelManager.levelInfoList[nowStageNum].waveMaxMonsterNum.Length)
        {
            levelManager.SpawnMonster(nowWave);
            nowWave++;
            if (nowWave != levelManager.levelInfoList[nowStageNum].waveMaxMonsterNum.Length)
                waveText.GetComponent<Text>().text = "Wave " + nowWave + " Start";
            else
                waveText.GetComponent<Text>().text = "Final Wave Start";

            currentWave.text = "Current Wave : " + nowWave + " / " + levelManager.levelInfoList[nowStageNum].waveMaxMonsterNum.Length;
            waveText.SetActive(true);
        }
        nowGameState = GameState.Battle;
        Invoke("DelayHideText", 1.5f);
    }
    
    public void DelayHideText()
    {
        waveText.SetActive(false);
    }

    public void CheckWave()
    {
        if (nowMonsterCnt - killMonsterCnt == 0)
        {
            //웨이브 끝 
            //대기시간으로
            if (nowWave >= levelManager.levelInfoList[nowStageNum].waveMaxMonsterNum.Length)
            {
                //몬스터를 다잡았는지?
                //다잡았으면 
                nowGameState = GameState.Result;
                isUsingRefreshUI = false;
                levelManager.ShowNextStagePortal();
                GameEnd(GameResultState.Win);
                nowStageNum++;
                return;
            }
            else
            {
                nowGameState = GameState.BattleEnd;
                waveText.SetActive(true);
                waveText.GetComponent<Text>().text = "Wave " + nowWave + " Clear";
                Invoke("StartNextWave", 1f);
                //StartSettingUI();
                return;
            }
        }       
    }

    public void StartNextWave()
    {
        startNextWave = true;
        //levelManager.RefreshSurface();
    }

    IEnumerator Co_StartLevel()
    {
        isUsingRefreshUI = true;
        yield return new WaitForSeconds(1f);
        nowGameState = GameState.Ready;
    }

    public void UpdateGameState(bool flag)
    {
        //몬스터 처치시 
        if(flag)
            killMonsterCnt++;
    }

    public void CreateAddMonster(int num)
    {
        nowMonsterCnt += num;
    }

    public void UpdateUIText()
    {
        if(isUsingRefreshUI && SceneManager.GetActiveScene().name.Equals("Game"))
        {
            //if (nowPlayerCharacter != null)
            //    playerScript = playerPrefabList[0].GetComponent<Player>();

            CheckCurrentBulletCnt();
            CheckCurrentGold();
            //CheckPlayerHP();
            CheckRemainEnemyCnt();
        }
    }

    public void CheckCurrentBulletCnt()
    {
        if(playerScript == null)
        {
            currentBullet.text = "CurrentBullet : 0 / 0"; 
        }
        else
        {
            int currentAmmo = playerScript.nowEquipWeapon.currentAmmo;
            int maxAmmo = playerScript.nowEquipWeapon.maxAmmo;

            if (playerScript.nowEquipWeapon.weaponSetInfo.attackType == AttackType.Melee)
                currentBullet.text = "CurrentBullet : Max / Max";
            else
            {
                if(currentAmmo == 0 )
                    currentBullet.text = "===== Reloading =====";
                else
                    currentBullet.text = "CurrentBullet : " + currentAmmo + " / " + maxAmmo;
            }
        }
    }
    
    public void CheckRemainEnemyCnt()
    {
        if (nowGameState == GameState.Result)
            return;
        else
        {
            remainEnemyCnt.text = "Remain Enemy : " + RetRemainEnemyCnt().ToString(); 
        }
    }
    public int RetRemainEnemyCnt()
    {
        int cnt = 0;
        for (int i = 0; i < nowWave; i++)
            cnt += levelManager.levelInfoList[nowStageNum].waveMaxMonsterNum[i];//waveMaxMonsterNum[i];

        return (cnt - killMonsterCnt);
    }

    public void CheckCurrentGold()
    {
        currentGoldText.text = "Current Gold : " + nowGetCoin;
    }

    public void ResetData(bool flag = false)
    {
        nowMonsterCnt = 0;
        killMonsterCnt = 0;
        nowGameResultState = GameResultState.None;
        nowWave = 0;
        currentIntervalTime = 0f;
        startNextWave = false;
        isStartLevel = false;
        hpCanvas = GameObject.Find("Canvas_HP");
        cam_Follow = GameObject.Find("Follow_Cam");
        waveText = GameObject.Find("Wave Text");
        currentBullet = GameObject.Find("CurrentBulletText").GetComponent<Text>();
        currentGoldText = GameObject.Find("CurrentGoldText").GetComponent<Text>();
        currentWave = GameObject.Find("CurrentWaveText").GetComponent<Text>();
        remainEnemyCnt = GameObject.Find("EnemyCntText").GetComponent<Text>();
        ResultPopup = GameObject.Find("Canvas/ResultPopup");
        PausePopup = GameObject.Find("Canvas/PausePopup");
        dodgeBtn = GameObject.Find("DodgeBtn");
        pauseBtn = GameObject.Find("PauseBtn");
        atkBtn = GameObject.Find("AtkBtn_0");
        playerParentPos = GameObject.Find("PlayerPos").transform;
        ResultPopup.SetActive(false);
        PausePopup.SetActive(false);
        dodgeBtn.GetComponent<Button>().onClick.AddListener(DoPlayerDodge);
        pauseBtn.GetComponent<Button>().onClick.AddListener(ShowPausePopup);
        atkBtn.GetComponent<Button>().onClick.AddListener(AttackPlayer);
        if (flag)
        {
            LeanTween.delayedCall(0.1f, () => {
                nowGameState = GameState.BattleReady;
                SceneManager.sceneLoaded -= OnSceneLoaded;
            });

        }
        isWaitStart = true;
    }

    public void GoNextLevel()
    {
        isWaitStart = false;
        nowStageNum++;
        SceneManager.sceneLoaded += OnSceneLoaded;
        SceneManager.LoadSceneAsync("Game");
    }

    public void GameEnd(GameResultState _gState)
    {
        nowGameResultState = _gState;
        nowGameState = GameState.Result;
        ResultPopup.GetComponent<Popup_Result>().SetDelegate();
        ResultPopup.SetActive(true);
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        //LeanTween.delayedCall(.1f, ()=> { ResetData(true); });
        StageManager.Instance.ShowStage("Stage"+(nowStageNum+1));               
    }

    public void GetCoin(int value)
    {
        nowGetCoin += value;
    }

    public void GoGame(/*int i*/)
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        SceneManager.LoadSceneAsync(2);
    }

    public void GoLobby()
    {
        ObjectPool.instance.AllHide();

        isUsingRefreshUI = false;
         nowGameResultState = GameResultState.None;
        nowGameState = GameState.None;
        SceneManager.LoadSceneAsync(1);
    }

    public void DoPlayerDodge()
    {
        playerScript.ToggleDodge();
    }

    public void ShowPausePopup()
    {
        PausePopup.SetActive(true);
        SetTimeScale(true);
    }
    public void SetTimeScale(bool pauseFlag)
    {
        Time.timeScale = pauseFlag ? 0f : 1f;
        isPause = pauseFlag;
        ToggleHpbar(pauseFlag);
    }

    public void ToggleHpbar(bool flag)
    {
        for (int i = 0; i < hpCanvas.transform.childCount; i++)
        {
            hpCanvas.transform.GetChild(i).gameObject.SetActive(!flag);
        }
    }

    public void AttackPlayer()
    {
        playerScript.Attack();
    }
}
