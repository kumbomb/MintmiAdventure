using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

[System.Serializable]
public class LevelInfo
{
    public int stageNo;
    public int[] waveMaxMonsterNum;
}

public class LevelManager : MonoBehaviour
{
    public NavMeshSurface surface;

    public LevelInfo[] levelInfoList;

    public Transform stagePos;

    [SerializeField] Transform[] MonsterRespawnPos;
    [SerializeField] GameObject NextStagePortal;
    int nowWaveNum = 0;

    string[] enemyTypeStr = {"EnemyA", "EnemyB", "EnemyC", "EnemyBoss"}; 
    
    public void InitLevel()
    {
        if (surface == null)
            surface = GameObject.Find("NavMesh").GetComponent<NavMeshSurface>();

        MonsterRespawnPos = null;
        GameObject SpawnParent = GameObject.Find("SpawnPoint");
        MonsterRespawnPos = new Transform[SpawnParent.transform.childCount];
        for (int i = 0; i < SpawnParent.transform.childCount; i++)
        {
            MonsterRespawnPos[i] = SpawnParent.transform.GetChild(i).transform;
        }
        stagePos = GameObject.Find("StageTile").transform;

        NextStagePortal = GameObject.Find("NextStagePortal");
        NextStagePortal.SetActive(false);

        RefreshSurface();
    }

    public void RefreshSurface()
    {       
        surface.BuildNavMesh();
    }

    public void SpawnMonster(int waveNum)
    {
        nowWaveNum = waveNum;
        StartCoroutine(Co_SpawnMonster());
    }

    IEnumerator Co_SpawnMonster()
    {
        for (int i = 0; i < levelInfoList[GameManager.instance.nowStageNum].waveMaxMonsterNum[nowWaveNum]; i++)
        {
            if (GameManager.instance.nowGameResultState != GameResultState.None)
                yield break;

            GameManager.instance.CreateAddMonster(1);
            int num;
            int pos;
            //Final Wave Boss Instantiate
            if (nowWaveNum == levelInfoList[GameManager.instance.nowStageNum].waveMaxMonsterNum.Length -1 && i == levelInfoList[GameManager.instance.nowStageNum].waveMaxMonsterNum[nowWaveNum] - 1)
            {
                num = 3;
                pos = MonsterRespawnPos.Length - 1;
            }
            else
            {
                switch (nowWaveNum)
                {
                    case 0:
                        num = Random.Range(0, 1);
                        break;
                    case 1:
                        num = Random.Range(0, 2);
                        break;
                    default:
                        num = Random.Range(0, 3);
                        break;
                }
                pos = Random.Range(0, MonsterRespawnPos.Length - 1);
            }

            GameObject monster = ObjectPool.instance.PopFromPool(enemyTypeStr[num], ObjectPool.instance.MonsterPool);
            monster.transform.position = MonsterRespawnPos[pos].position;
            monster.SetActive(true);
            monster.GetComponent<Enemy>().ResetEnemy();

            yield return new WaitForSeconds(0.5f);
        }
    }

    public void ShowNextStagePortal()
    {
        if (NextStagePortal == null)
            return;
        else
            NextStagePortal.SetActive(true);
    }

    public void SetStageTileRandomHeight()
    {

    }
}
