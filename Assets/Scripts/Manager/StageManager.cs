using System.Collections.Generic;
using UnityEngine;

public class StageManager : MonoBehaviour
{
    public static StageManager Instance;
    [SerializeField] TextAsset StageText;
    [SerializeField] Dictionary<string, GameObject> stagePrefabList = new Dictionary<string, GameObject>();

    GameObject currentStageObject;

    private void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        SettingStageDictionary();
    }

    void SettingStageDictionary()
    {
        string[] lines = StageText.text.Split('\n');
        if (lines.Length == 0)
        {
            Debug.Log("No Files");
            return;
        }

        for (int i = 0; i < lines.Length; i++)
        {
            lines[i] = lines[i].Replace("\r", "");
            string[] words = lines[i].Split(',');
            if (words.Length < 3)
                continue;

            if (!int.TryParse(words[0], out int stageIdx))
                continue;

            string key = words[1];
            string path = words[2];
            if (!stagePrefabList.ContainsKey(key))
                stagePrefabList.Add(key, Resources.Load<GameObject>(path));

            Debug.LogFormat("key : {0} / path : {1}", key, path);
        }
    }

    public bool HasStage(string stageKey)
    {
        return stagePrefabList.ContainsKey(stageKey) && stagePrefabList[stageKey] != null;
    }

    public void ShowStage(string stageKey, bool resetGame = true)
    {
        Transform parent = GameObject.Find("StagePos").transform;

        if (currentStageObject != null)
            Destroy(currentStageObject);

        if (!HasStage(stageKey))
        {
            Debug.LogWarning("Stage not found : " + stageKey);
            return;
        }

        currentStageObject = Instantiate(stagePrefabList[stageKey], parent);

        if (resetGame)
            GameManager.instance.ResetData(true);
        else
            GameManager.instance.OnStageReplaced();
    }

    public void AllHideStage()
    {
        if (currentStageObject != null)
            Destroy(currentStageObject);
        currentStageObject = null;
    }
}
