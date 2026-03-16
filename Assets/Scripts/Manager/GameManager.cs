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
    [SerializeField] GameObject miniMapCam;

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
    [SerializeField] float maxIntervalTime = 1.5f;

    public int nowStageNum = 0;
    public int nowMonsterCnt = 0;
    public int killMonsterCnt = 0;

    public int nowGetCoin = 0;
    public int curPlayerWeaponNum = 0;

    void Awake()
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
                HandleBattleReady();
                break;
            case GameState.Ready:
                HandleStageIntro();
                break;
            case GameState.BattleStart:
                StartStage();
                break;
            case GameState.Battle:
                levelManager?.TickStage();
                CheckStageClear();
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

    void HandleBattleReady()
    {
        if (nowPlayerCharacter != null)
        {
            if (isStartLevel || !isWaitStart)
                return;

            isStartLevel = true;
            if (cam_Follow != null)
                cam_Follow.SetActive(true);
            StartCoroutine(Co_StartLevel());
            return;
        }

        levelManager?.InitLevel();
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

    void HandleStageIntro()
    {
        if (waveText == null)
        {
            nowGameState = GameState.BattleStart;
            return;
        }

        if (!waveText.activeSelf)
            waveText.SetActive(true);

        waveText.GetComponent<Text>().text = "Stage " + (nowStageNum + 1) + " Start";
        currentIntervalTime += Time.deltaTime;
        if (currentIntervalTime >= maxIntervalTime)
        {
            currentIntervalTime = 0f;
            nowGameState = GameState.BattleStart;
        }
    }

    void StartStage()
    {
        levelManager?.StartStage(nowStageNum);
        nowGameState = GameState.Battle;
        DelayHideText();
    }

    void CheckStageClear()
    {
        if (nowGameResultState != GameResultState.None || levelManager == null)
            return;

        if (!levelManager.IsStageCleared)
            return;

        isUsingRefreshUI = false;
        levelManager.ShowNextStagePortal();
        GameEnd(GameResultState.Win);
        nowStageNum++;
    }

    public void DelayHideText()
    {
        if (waveText != null)
            waveText.SetActive(false);
    }

    void AlignMiniMapCamera()
    {
        if (miniMapCam == null)
            return;

        miniMapCam.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
    }

    IEnumerator Co_StartLevel()
    {
        isUsingRefreshUI = true;
        yield return new WaitForSeconds(1f);
        nowGameState = GameState.Ready;
    }

    public void RegisterEnemyKill(MonsterType monsterType, int clusterId)
    {
        killMonsterCnt++;
        levelManager?.NotifyEnemyKilled(monsterType, clusterId);
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
            CheckStageStatus();
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
        return Mathf.Max(0, nowMonsterCnt - killMonsterCnt);
    }

    void CheckStageStatus()
    {
        if (currentWave == null)
            return;

        if (levelManager == null)
        {
            currentWave.text = "Stage : " + (nowStageNum + 1);
            return;
        }

        currentWave.text = levelManager.GetStageStatusText(nowStageNum);
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
        currentIntervalTime = 0f;
        startNextWave = false;
        isStartLevel = false;

        hpCanvas = GameObject.Find("Canvas_HP");
        cam_Follow = GameObject.Find("Follow_Cam");
        miniMapCam = GameObject.Find("MiniMap_Cam");
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

        AlignMiniMapCamera();

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

        levelManager?.ResetStageRuntime();

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

        Time.timeScale = 1f;
        isPause = false;
        ToggleHpbar(true);
        nowPlayerCharacter = null;
        playerScript = null;
        isUsingRefreshUI = false;
        nowGameResultState = GameResultState.None;
        nowGameState = GameState.None;
        levelManager?.ResetStageRuntime();
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
            hpCanvas.transform.GetChild(i).gameObject.SetActive(!flag);
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
