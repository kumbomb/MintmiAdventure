using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

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
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(this);
    }

    void Start()
    {
        isWaitStart = false;
        Application.targetFrameRate = 60;

        if (!Application.isEditor)
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
    }

    void Update()
    {
        switch (nowGameState)
        {
            case GameState.BattleReady:
                if (nowPlayerCharacter != null)
                {
                    if (isStartLevel || !isWaitStart)
                        return;

                    isStartLevel = true;
                    if (cam_Follow != null)
                        cam_Follow.SetActive(true);
                    StartCoroutine(Co_StartLevel());
                }
                else
                {
                    levelManager.InitLevel();
                    nowPlayerCharacter = Instantiate(playerPrefabList[0], Vector3.zero, Quaternion.identity);
                    nowPlayerCharacter.transform.parent = playerParentPos;
                    playerScript = nowPlayerCharacter.GetComponent<Player>();
                    cam_Follow.GetComponent<FollowCamera>().SetTarget(nowPlayerCharacter.transform);
                    playerScript.ChangeEquipWeapon(curPlayerWeaponNum, true);

                    subPlayerCharacter.Clear();
                    for (int i = 0; i < subPlayerIdxList.Count; i++)
                    {
                        subPlayerCharacter.Add(Instantiate(subPlayerObjList[subPlayerIdxList[i]], new Vector3(5f, 0f, 0f), Quaternion.identity));
                        subPlayerCharacter[i].transform.parent = playerParentPos;
                        subPlayerCharacter[i].GetComponent<SubPlayerParent>().supporterNum = i;
                    }
                }
                break;

            case GameState.Ready:
                if (!waveText.activeSelf)
                    waveText.SetActive(true);

                waveText.GetComponent<Text>().text = "Wait Next Wave\n" + (int)(maxIntervalTime - currentIntervalTime);
                currentIntervalTime += Time.deltaTime;

                if (currentIntervalTime >= maxIntervalTime)
                {
                    currentIntervalTime = 0f;
                    nowGameState = GameState.BattleStart;
                }
                break;

            case GameState.BattleStart:
                StartWave();
                break;

            case GameState.Battle:
                CheckWave();
                break;

            case GameState.BattleEnd:
                if (startNextWave)
                {
                    currentIntervalTime = 0f;
                    startNextWave = false;
                    nowGameState = GameState.Ready;
                }
                break;
        }

        UpdateUIText();

        if (Input.GetKeyDown(KeyCode.Escape)
            && SceneManager.GetActiveScene().name.Equals("Game")
            && nowGameResultState == GameResultState.None
            && PausePopup != null)
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
        if (nowWave < levelManager.levelInfoList[nowStageNum].waveMaxMonsterNum.Length)
        {
            levelManager.SpawnMonster(nowWave);
            nowWave++;
            waveText.GetComponent<Text>().text = nowWave != levelManager.levelInfoList[nowStageNum].waveMaxMonsterNum.Length
                ? "Wave " + nowWave + " Start"
                : "Final Wave Start";

            currentWave.text = "Current Wave : " + nowWave + " / " + levelManager.levelInfoList[nowStageNum].waveMaxMonsterNum.Length;
            waveText.SetActive(true);
        }

        nowGameState = GameState.Battle;
        Invoke(nameof(DelayHideText), 1.5f);
    }
    
    public void DelayHideText()
    {
        if (waveText != null)
            waveText.SetActive(false);
    }

    public void CheckWave()
    {
        if (nowMonsterCnt - killMonsterCnt != 0)
            return;

        if (nowWave >= levelManager.levelInfoList[nowStageNum].waveMaxMonsterNum.Length)
        {
            nowGameState = GameState.Result;
            isUsingRefreshUI = false;
            levelManager.ShowNextStagePortal();
            GameEnd(GameResultState.Win);
            nowStageNum++;
            return;
        }

        nowGameState = GameState.BattleEnd;
        waveText.SetActive(true);
        waveText.GetComponent<Text>().text = "Wave " + nowWave + " Clear";
        Invoke(nameof(StartNextWave), 1f);
    }

    public void StartNextWave()
    {
        startNextWave = true;
    }

    IEnumerator Co_StartLevel()
    {
        isUsingRefreshUI = true;
        yield return new WaitForSeconds(1f);
        nowGameState = GameState.Ready;
    }

    public void UpdateGameState(bool flag)
    {
        if (flag)
            killMonsterCnt++;
    }

    public void CreateAddMonster(int num)
    {
        nowMonsterCnt += num;
    }

    public void UpdateUIText()
    {
        if (isUsingRefreshUI && SceneManager.GetActiveScene().name.Equals("Game"))
        {
            CheckCurrentBulletCnt();
            CheckCurrentGold();
            CheckRemainEnemyCnt();
        }
    }

    public void CheckCurrentBulletCnt()
    {
        if (currentBullet == null)
            return;

        if (playerScript == null)
        {
            currentBullet.text = "Survival Skill : --";
            return;
        }

        currentBullet.text = playerScript.GetCombatStatusText();
    }
    
    public void CheckRemainEnemyCnt()
    {
        if (remainEnemyCnt == null || nowGameState == GameState.Result)
            return;

        remainEnemyCnt.text = "Remain Enemy : " + RetRemainEnemyCnt();
    }

    public int RetRemainEnemyCnt()
    {
        int cnt = 0;
        for (int i = 0; i < nowWave; i++)
            cnt += levelManager.levelInfoList[nowStageNum].waveMaxMonsterNum[i];

        return cnt - killMonsterCnt;
    }

    public void CheckCurrentGold()
    {
        if (currentGoldText != null)
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
        currentBullet = GameObject.Find("CurrentBulletText")?.GetComponent<Text>();
        currentGoldText = GameObject.Find("CurrentGoldText")?.GetComponent<Text>();
        currentWave = GameObject.Find("CurrentWaveText")?.GetComponent<Text>();
        remainEnemyCnt = GameObject.Find("EnemyCntText")?.GetComponent<Text>();
        ResultPopup = GameObject.Find("Canvas/ResultPopup");
        PausePopup = GameObject.Find("Canvas/PausePopup");
        dodgeBtn = GameObject.Find("DodgeBtn");
        pauseBtn = GameObject.Find("PauseBtn");
        atkBtn = GameObject.Find("AtkBtn_0");
        playerParentPos = GameObject.Find("PlayerPos")?.transform;

        if (ResultPopup != null)
            ResultPopup.SetActive(false);
        if (PausePopup != null)
            PausePopup.SetActive(false);

        Button dodgeButton = dodgeBtn != null ? dodgeBtn.GetComponent<Button>() : null;
        Button pauseButton = pauseBtn != null ? pauseBtn.GetComponent<Button>() : null;
        Button attackButton = atkBtn != null ? atkBtn.GetComponent<Button>() : null;
        if (dodgeButton != null)
        {
            dodgeButton.onClick.RemoveListener(DoPlayerDodge);
            dodgeButton.onClick.AddListener(DoPlayerDodge);
        }
        if (pauseButton != null)
        {
            pauseButton.onClick.RemoveListener(ShowPausePopup);
            pauseButton.onClick.AddListener(ShowPausePopup);
        }
        if (attackButton != null)
        {
            attackButton.onClick.RemoveListener(AttackPlayer);
            attackButton.onClick.AddListener(AttackPlayer);
        }

        if (flag)
        {
            DOVirtual.DelayedCall(0.1f, () =>
            {
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
        RegisterSceneLoadCallback();
        SceneManager.LoadSceneAsync("Game");
    }

    public void GameEnd(GameResultState gameResult)
    {
        nowGameResultState = gameResult;
        nowGameState = GameState.Result;
        if (ResultPopup == null)
            return;

        ResultPopup.GetComponent<Popup_Result>().SetDelegate();
        ResultPopup.SetActive(true);
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!scene.name.Equals("Game"))
            return;

        StageManager.Instance.ShowStage("Stage" + (nowStageNum + 1));
    }

    public void GetCoin(int value)
    {
        nowGetCoin += value;
    }

    public void GoGame()
    {
        RegisterSceneLoadCallback();
        SceneManager.LoadSceneAsync(2);
    }

    public void GoLobby()
    {
        if (ObjectPool.instance != null)
            ObjectPool.instance.AllHide();

        SetTimeScale(false);
        nowPlayerCharacter = null;
        playerScript = null;
        isUsingRefreshUI = false;
        nowGameResultState = GameResultState.None;
        nowGameState = GameState.None;
        SceneManager.LoadSceneAsync(1);
    }

    public void DoPlayerDodge()
    {
        if (playerScript == null)
            return;

        playerScript.ActivateSurvivalSkill();
    }

    public void ShowPausePopup()
    {
        if (PausePopup == null)
            return;

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
        if (hpCanvas == null)
            return;

        for (int i = 0; i < hpCanvas.transform.childCount; i++)
        {
            hpCanvas.transform.GetChild(i).gameObject.SetActive(!flag);
        }
    }

    public void AttackPlayer()
    {
        if (playerScript == null)
            return;

        playerScript.Attack();
    }

    void RegisterSceneLoadCallback()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }
}
