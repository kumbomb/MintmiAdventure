using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;

public class StageManager : MonoBehaviour
{
    public static StageManager Instance;
    [SerializeField] TextAsset StageText;
    [SerializeField] Dictionary<string, GameObject> stagePrefabList = new Dictionary<string, GameObject>();

    // Start is called before the first frame update
    private void Awake()
    {
        Instance = this;
    }
    void Start()
    {
        // ÆÄ½Ì ¹× ¼¼ÆÃ
        SettingStageDictionary();
    }

    void SettingStageDictionary()
    {
        string[] lines = StageText.text.Split('\n');
        if(lines.Length == 0)
        {
            Debug.Log("No Files");
        }
        else
        {
            for (int i = 0; i < lines.Length; i++)
            {
                lines[i] = lines[i].Replace("\r", "");
                string[] words = lines[i].Split(',');

                if(int.TryParse(words[0], out int stageIdx))
                {
                    string key = words[1];
                    string path = words[2];

                    stagePrefabList.Add(words[1], Resources.Load<GameObject>(path));

                    Debug.LogFormat("key : {0} / path : {1}",key,path);
                }
            }
        }
    }

    public void ShowStage(string stageKey)
    {
        Transform parent = GameObject.Find("StagePos").transform;

        GameObject stagePrefab = Instantiate(stagePrefabList[stageKey], parent);

        GameManager.instance.ResetData(true);
    }

    public void AllHideStage()
    {

    }
}
