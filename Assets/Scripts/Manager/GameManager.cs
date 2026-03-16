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

public enum LevelUpRewardType
{
    MaxHealth,
    MoveSpeed,
    WeaponPower,
    AddSupporter,
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

    const int ExpPerKill = 10;
    const float StageClearPopupDuration = 2f;

    int currentLevel = 1;
    int currentExp;
    int expToNextLevel = 40;
    int pendingLevelUpCount;
    bool isLevelUpSelecting;
    bool isStageTransitioning;

    Canvas mainCanvas;
    GameObject rogueHudRoot;
    Text levelText;
    Slider expSlider;
    Text expText;
    GameObject levelUpPopup;
    readonly List<Button> levelUpButtons = new List<Button>();
    readonly List<Text> levelUpButtonTexts = new List<Text>();
    readonly List<LevelUpRewardType> currentRewardOptions = new List<LevelUpRewardType>();

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

        if (isLevelUpSelecting)
            return;

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
            GameObject supporter = Instantiate(subPlayerObjList[subPlayerIdxList[i]], new Vector3(5f, 0f, 0f), Quaternion.identity);
            supporter.transform.parent = playerParentPos;
            supporter.GetComponent<SubPlayerParent>().supporterNum = i;
            subPlayerCharacter.Add(supporter);
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
        if (nowGameResultState != GameResultState.None || levelManager == null || isStageTransitioning)
            return;

        if (!levelManager.IsStageCleared)
            return;

        StartCoroutine(Co_AdvanceToNextStage());
    }

    IEnumerator Co_AdvanceToNextStage()
    {
        isStageTransitioning = true;
        isUsingRefreshUI = false;
        GameEnd(GameResultState.Win);
        yield return new WaitForSecondsRealtime(StageClearPopupDuration);

        nowStageNum++;
        StartNextStageOnCurrentMap();
    }

    void StartNextStageOnCurrentMap()
    {
        if (ObjectPool.instance != null)
            ObjectPool.instance.AllHide();

        nowMonsterCnt = 0;
        killMonsterCnt = 0;
        nowGameResultState = GameResultState.None;
        nowGameState = GameState.BattleReady;
        currentIntervalTime = 0f;
        isStartLevel = false;
        isWaitStart = true;
        isUsingRefreshUI = false;
        isStageTransitioning = false;
        Time.timeScale = 1f;
        isPause = false;

        if (ResultPopup != null)
            ResultPopup.SetActive(false);
        if (PausePopup != null)
            PausePopup.SetActive(false);

        levelManager?.ResetStageRuntime();
        levelManager?.InitLevel();
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

        if (monsterType != MonsterType.EnemyBoss)
            AddExperience(ExpPerKill);
    }

    void AddExperience(int amount)
    {
        if (amount <= 0)
            return;

        currentExp += amount;
        while (currentExp >= expToNextLevel)
        {
            currentExp -= expToNextLevel;
            currentLevel++;
            expToNextLevel = GetNextLevelRequirement(currentLevel);
            pendingLevelUpCount++;
        }

        UpdateRogueUI();
        if (pendingLevelUpCount > 0 && !isLevelUpSelecting)
            ShowLevelUpPopup();
    }

    int GetNextLevelRequirement(int level)
    {
        return 40 + Mathf.Max(0, level - 1) * 20;
    }

    void ShowLevelUpPopup()
    {
        if (levelUpPopup == null)
            return;

        BuildRewardOptions();
        for (int i = 0; i < levelUpButtonTexts.Count; i++)
            levelUpButtonTexts[i].text = GetRewardLabel(currentRewardOptions[i]);

        isLevelUpSelecting = true;
        Time.timeScale = 0f;
        levelUpPopup.SetActive(true);
    }

    void BuildRewardOptions()
    {
        currentRewardOptions.Clear();
        List<LevelUpRewardType> candidateRewards = new List<LevelUpRewardType>
        {
            LevelUpRewardType.MaxHealth,
            LevelUpRewardType.MoveSpeed,
            LevelUpRewardType.WeaponPower,
        };

        if (CanAddSupporter())
            candidateRewards.Add(LevelUpRewardType.AddSupporter);

        while (currentRewardOptions.Count < 3)
        {
            if (candidateRewards.Count == 0)
                candidateRewards.Add(LevelUpRewardType.WeaponPower);

            int randIdx = Random.Range(0, candidateRewards.Count);
            LevelUpRewardType reward = candidateRewards[randIdx];
            candidateRewards.RemoveAt(randIdx);
            currentRewardOptions.Add(reward);
        }
    }

    string GetRewardLabel(LevelUpRewardType rewardType)
    {
        switch (rewardType)
        {
            case LevelUpRewardType.MaxHealth:
                return "Vital Boost\nMax HP +25\nHeal +25";
            case LevelUpRewardType.MoveSpeed:
                return "Quick Step\nMove Speed +2";
            case LevelUpRewardType.WeaponPower:
                return "Arms Upgrade\nWeapon Damage Up";
            case LevelUpRewardType.AddSupporter:
                return "Companion Call\nAdd 1 Supporter";
            default:
                return "Upgrade";
        }
    }

    public void SelectLevelUpReward(int optionIndex)
    {
        if (optionIndex < 0 || optionIndex >= currentRewardOptions.Count)
            return;

        ApplyReward(currentRewardOptions[optionIndex]);
        pendingLevelUpCount = Mathf.Max(0, pendingLevelUpCount - 1);
        levelUpPopup.SetActive(false);

        if (pendingLevelUpCount > 0)
        {
            ShowLevelUpPopup();
            return;
        }

        isLevelUpSelecting = false;
        Time.timeScale = isPause ? 0f : 1f;
        UpdateRogueUI();
    }

    void ApplyReward(LevelUpRewardType rewardType)
    {
        switch (rewardType)
        {
            case LevelUpRewardType.MaxHealth:
                playerScript?.ApplyMaxHealthUpgrade(25);
                break;
            case LevelUpRewardType.MoveSpeed:
                playerScript?.ApplyMoveSpeedUpgrade(2f);
                break;
            case LevelUpRewardType.WeaponPower:
                UpgradeWeapons();
                break;
            case LevelUpRewardType.AddSupporter:
                AddSupporterRuntime();
                break;
        }
    }

    void UpgradeWeapons()
    {
        if (playerScript == null || playerScript.weapons == null)
            return;

        for (int i = 0; i < playerScript.weapons.Length; i++)
        {
            Weapon weapon = playerScript.weapons[i] != null ? playerScript.weapons[i].GetComponent<Weapon>() : null;
            if (weapon == null)
                continue;

            weapon.weaponSetInfo.minDamage += 2;
            weapon.weaponSetInfo.maxDamage += 4;
            weapon.weaponSetInfo.rate = Mathf.Max(0.08f, weapon.weaponSetInfo.rate * 0.92f);
        }
    }

    bool CanAddSupporter()
    {
        return subPlayerObjList != null && subPlayerIdxList.Count < subPlayerObjList.Length;
    }

    void AddSupporterRuntime()
    {
        if (!CanAddSupporter())
        {
            UpgradeWeapons();
            return;
        }

        int nextIndex = 0;
        while (subPlayerIdxList.Contains(nextIndex) && nextIndex < subPlayerObjList.Length)
            nextIndex++;

        if (nextIndex >= subPlayerObjList.Length)
        {
            UpgradeWeapons();
            return;
        }

        subPlayerIdxList.Add(nextIndex);
        if (nowPlayerCharacter == null || playerParentPos == null)
            return;

        GameObject supporter = Instantiate(subPlayerObjList[nextIndex], nowPlayerCharacter.transform.position + new Vector3(2f, 0f, 0f), Quaternion.identity);
        supporter.transform.parent = playerParentPos;
        supporter.GetComponent<SubPlayerParent>().supporterNum = subPlayerCharacter.Count;
        subPlayerCharacter.Add(supporter);
    }

    public void CreateAddMonster(int num)
    {
        nowMonsterCnt += num;
    }

    public void UpdateUIText()
    {
        if (SceneManager.GetActiveScene().name.Equals("Game"))
        {
            UpdateRogueUI();
            if (isUsingRefreshUI)
            {
                CheckCurrentBulletCnt();
                CheckCurrentGold();
                CheckRemainEnemyCnt();
                CheckStageStatus();
            }
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
        isStageTransitioning = false;
        isPause = false;
        Time.timeScale = 1f;

        RefreshSceneBindings();
        levelManager?.ResetStageRuntime();

        if (flag)
        {
            ResetRunProgression();
            DOVirtual.DelayedCall(0.1f, () =>
            {
                nowGameState = GameState.BattleReady;
                SceneManager.sceneLoaded -= OnSceneLoaded;
            });
        }

        isWaitStart = true;
    }

    void RefreshSceneBindings()
    {
        hpCanvas = GameObject.Find("Canvas_HP");
        cam_Follow = GameObject.Find("Follow_Cam");
        miniMapCam = GameObject.Find("MiniMap_Cam");
        waveText = GameObject.Find("Wave Text");
        currentBullet = GameObject.Find("CurrentBulletText")?.GetComponent<Text>();
        currentGoldText = GameObject.Find("CurrentGoldText")?.GetComponent<Text>();
        currentWave = GameObject.Find("CurrentWaveText")?.GetComponent<Text>();
        remainEnemyCnt = GameObject.Find("EnemyCntText")?.GetComponent<Text>();
        dodgeBtn = GameObject.Find("DodgeBtn");
        pauseBtn = GameObject.Find("PauseBtn");
        atkBtn = GameObject.Find("AtkBtn_0");
        playerParentPos = GameObject.Find("PlayerPos")?.transform;
        mainCanvas = GameObject.Find("Canvas")?.GetComponent<Canvas>();
        ResultPopup = mainCanvas != null ? mainCanvas.transform.Find("ResultPopup")?.gameObject : null;
        PausePopup = mainCanvas != null ? mainCanvas.transform.Find("PausePopup")?.gameObject : null;

        if (ResultPopup != null)
            ResultPopup.SetActive(false);
        if (PausePopup != null)
            PausePopup.SetActive(false);

        AlignMiniMapCamera();
        BindButtons();
        EnsureRogueUI();
    }

    void BindButtons()
    {
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
    }

    void ResetRunProgression()
    {
        currentLevel = 1;
        currentExp = 0;
        expToNextLevel = 40;
        pendingLevelUpCount = 0;
        isLevelUpSelecting = false;
        UpdateRogueUI();
        if (levelUpPopup != null)
            levelUpPopup.SetActive(false);
    }

    Font GetRuntimeFont()
    {
        if (currentGoldText != null && currentGoldText.font != null)
            return currentGoldText.font;
        return Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
    }

    void EnsureRogueUI()
    {
        if (mainCanvas == null)
            return;

        Transform uiParent = GameObject.Find("InGamePanel")?.transform;
        if (uiParent == null)
            uiParent = mainCanvas.transform;

        Font font = GetRuntimeFont();
        if (rogueHudRoot == null)
        {
            rogueHudRoot = new GameObject("RogueHud", typeof(RectTransform));
            rogueHudRoot.transform.SetParent(uiParent, false);
            RectTransform rootRect = rogueHudRoot.GetComponent<RectTransform>();
            rootRect.anchorMin = new Vector2(0.5f, 1f);
            rootRect.anchorMax = new Vector2(0.5f, 1f);
            rootRect.pivot = new Vector2(0.5f, 1f);
            rootRect.anchoredPosition = new Vector2(0f, -88f);
            rootRect.sizeDelta = new Vector2(360f, 110f);

            levelText = CreateText("LevelText", rogueHudRoot.transform, font, 28, TextAnchor.MiddleLeft);
            RectTransform levelRect = levelText.rectTransform;
            levelRect.anchorMin = new Vector2(0.5f, 1f);
            levelRect.anchorMax = new Vector2(0.5f, 1f);
            levelRect.pivot = new Vector2(0.5f, 1f);
            levelRect.anchoredPosition = Vector2.zero;
            levelRect.sizeDelta = new Vector2(220f, 32f);

            GameObject sliderObject = new GameObject("ExpSlider", typeof(RectTransform), typeof(Image), typeof(Slider));
            sliderObject.transform.SetParent(rogueHudRoot.transform, false);
            RectTransform sliderRect = sliderObject.GetComponent<RectTransform>();
            sliderRect.anchorMin = new Vector2(0.5f, 1f);
            sliderRect.anchorMax = new Vector2(0.5f, 1f);
            sliderRect.pivot = new Vector2(0.5f, 1f);
            sliderRect.anchoredPosition = new Vector2(0f, -40f);
            sliderRect.sizeDelta = new Vector2(260f, 24f);
            Image sliderBg = sliderObject.GetComponent<Image>();
            sliderBg.color = new Color(0.12f, 0.12f, 0.12f, 0.9f);
            expSlider = sliderObject.GetComponent<Slider>();
            expSlider.targetGraphic = sliderBg;
            expSlider.minValue = 0f;
            expSlider.maxValue = 1f;

            GameObject fillArea = new GameObject("FillArea", typeof(RectTransform));
            fillArea.transform.SetParent(sliderObject.transform, false);
            RectTransform fillAreaRect = fillArea.GetComponent<RectTransform>();
            fillAreaRect.anchorMin = new Vector2(0f, 0f);
            fillAreaRect.anchorMax = new Vector2(1f, 1f);
            fillAreaRect.offsetMin = new Vector2(4f, 4f);
            fillAreaRect.offsetMax = new Vector2(-4f, -4f);

            GameObject fill = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fill.transform.SetParent(fillArea.transform, false);
            Image fillImage = fill.GetComponent<Image>();
            fillImage.color = new Color(0.96f, 0.78f, 0.22f, 1f);
            RectTransform fillRect = fill.GetComponent<RectTransform>();
            fillRect.anchorMin = new Vector2(0f, 0f);
            fillRect.anchorMax = new Vector2(1f, 1f);
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;
            expSlider.fillRect = fillRect;
            expSlider.handleRect = null;
            expSlider.direction = Slider.Direction.LeftToRight;

            expText = CreateText("ExpText", rogueHudRoot.transform, font, 20, TextAnchor.MiddleLeft);
            RectTransform expRect = expText.rectTransform;
            expRect.anchorMin = new Vector2(0.5f, 1f);
            expRect.anchorMax = new Vector2(0.5f, 1f);
            expRect.pivot = new Vector2(0.5f, 1f);
            expRect.anchoredPosition = new Vector2(0f, -72f);
            expRect.sizeDelta = new Vector2(240f, 24f);
        }

        if (levelUpPopup == null)
            CreateLevelUpPopup(font);

        UpdateRogueUI();
    }

    Text CreateText(string name, Transform parent, Font font, int fontSize, TextAnchor anchor)
    {
        GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(Text));
        textObject.transform.SetParent(parent, false);
        Text text = textObject.GetComponent<Text>();
        text.font = font;
        text.fontSize = fontSize;
        text.alignment = anchor;
        text.color = Color.white;
        return text;
    }

    void CreateLevelUpPopup(Font font)
    {
        levelUpPopup = new GameObject("LevelUpPopup", typeof(RectTransform), typeof(Image));
        levelUpPopup.transform.SetParent(mainCanvas.transform, false);
        RectTransform popupRect = levelUpPopup.GetComponent<RectTransform>();
        popupRect.anchorMin = Vector2.zero;
        popupRect.anchorMax = Vector2.one;
        popupRect.offsetMin = Vector2.zero;
        popupRect.offsetMax = Vector2.zero;
        Image popupBg = levelUpPopup.GetComponent<Image>();
        popupBg.color = new Color(0f, 0f, 0f, 0.75f);

        GameObject panel = new GameObject("Panel", typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(levelUpPopup.transform, false);
        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = new Vector2(420f, 760f);
        Image panelImage = panel.GetComponent<Image>();
        panelImage.color = new Color(0.15f, 0.12f, 0.08f, 0.96f);

        Text title = CreateText("Title", panel.transform, font, 30, TextAnchor.MiddleCenter);
        title.text = "LEVEL UP - Choose One";
        RectTransform titleRect = title.rectTransform;
        titleRect.anchorMin = new Vector2(0.5f, 1f);
        titleRect.anchorMax = new Vector2(0.5f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.anchoredPosition = new Vector2(0f, -28f);
        titleRect.sizeDelta = new Vector2(320f, 56f);

        levelUpButtons.Clear();
        levelUpButtonTexts.Clear();
        for (int i = 0; i < 3; i++)
        {
            GameObject buttonObject = new GameObject("RewardBtn_" + i, typeof(RectTransform), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(panel.transform, false);
            RectTransform buttonRect = buttonObject.GetComponent<RectTransform>();
            buttonRect.anchorMin = new Vector2(0.5f, 1f);
            buttonRect.anchorMax = new Vector2(0.5f, 1f);
            buttonRect.pivot = new Vector2(0.5f, 1f);
            buttonRect.anchoredPosition = new Vector2(0f, -108f - (i * 198f));
            buttonRect.sizeDelta = new Vector2(340f, 168f);
            Image buttonImage = buttonObject.GetComponent<Image>();
            buttonImage.color = new Color(0.62f, 0.21f, 0.14f, 0.95f);

            int capturedIndex = i;
            Button button = buttonObject.GetComponent<Button>();
            button.onClick.AddListener(() => SelectLevelUpReward(capturedIndex));
            levelUpButtons.Add(button);

            Text buttonText = CreateText("Text", buttonObject.transform, font, 24, TextAnchor.MiddleCenter);
            buttonText.horizontalOverflow = HorizontalWrapMode.Wrap;
            buttonText.verticalOverflow = VerticalWrapMode.Overflow;
            RectTransform buttonTextRect = buttonText.rectTransform;
            buttonTextRect.anchorMin = new Vector2(0.5f, 0.5f);
            buttonTextRect.anchorMax = new Vector2(0.5f, 0.5f);
            buttonTextRect.pivot = new Vector2(0.5f, 0.5f);
            buttonTextRect.sizeDelta = new Vector2(280f, 124f);
            levelUpButtonTexts.Add(buttonText);
        }

        levelUpPopup.SetActive(false);
    }

    void UpdateRogueUI()
    {
        if (levelText == null || expSlider == null || expText == null)
            return;

        levelText.text = "Lv. " + currentLevel;
        expSlider.value = expToNextLevel <= 0 ? 0f : (float)currentExp / expToNextLevel;
        expText.text = "EXP " + currentExp + " / " + expToNextLevel;
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
        Time.timeScale = 1f;
        isPause = false;

        if (gameResult == GameResultState.Lose)
            ToggleHpbar(true);

        if (ResultPopup == null)
            RefreshSceneBindings();

        if (ResultPopup == null)
            return;

        ResultPopup.GetComponent<Popup_Result>().SetDelegate();
        ResultPopup.SetActive(true);
    }

    public void OnStageReplaced()
    {
        if (ObjectPool.instance != null)
            ObjectPool.instance.AllHide();

        nowMonsterCnt = 0;
        killMonsterCnt = 0;
        nowGameResultState = GameResultState.None;
        nowGameState = GameState.BattleReady;
        currentIntervalTime = 0f;
        isStartLevel = false;
        isWaitStart = true;
        isUsingRefreshUI = false;
        isStageTransitioning = false;
        Time.timeScale = 1f;

        RefreshSceneBindings();
        levelManager?.ResetStageRuntime();
        levelManager?.InitLevel();

        if (nowPlayerCharacter != null && playerParentPos != null)
        {
            nowPlayerCharacter.transform.SetParent(playerParentPos);
            nowPlayerCharacter.transform.position = playerParentPos.position;
            nowPlayerCharacter.transform.rotation = Quaternion.identity;
        }

        for (int i = 0; i < subPlayerCharacter.Count; i++)
        {
            if (subPlayerCharacter[i] == null || playerParentPos == null)
                continue;

            subPlayerCharacter[i].transform.SetParent(playerParentPos);
            subPlayerCharacter[i].transform.position = playerParentPos.position + new Vector3(1.5f + i, 0f, 0f);
        }
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
        if (StageManager.Instance != null)
            StageManager.Instance.AllHideStage();

        Time.timeScale = 1f;
        isPause = false;
        ToggleHpbar(true);
        nowPlayerCharacter = null;
        playerScript = null;
        isUsingRefreshUI = false;
        nowGameResultState = GameResultState.None;
        nowGameState = GameState.None;
        isLevelUpSelecting = false;
        isStageTransitioning = false;
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
        if (PausePopup == null || isLevelUpSelecting)
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








