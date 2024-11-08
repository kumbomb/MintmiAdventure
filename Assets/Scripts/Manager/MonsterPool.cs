using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MonsterPool : MonoBehaviour
{
    [SerializeField] int[] eachWaveMonCnt;

    [SerializeField] GameObject[] MonsterPrefabList;
    [SerializeField] Transform[] MonsterRespawnPos;

    int nowWaveNum = 0;
    // Start is called before the first frame update
    void Start()
    {
        //Pooling System 이라서 원래 미리 만들어 둔 오브젝트들을 키고 끄는 역할
    }    
    public void SpawnMonster(int waveNum)
    {
        if(MonsterRespawnPos == null ||  MonsterRespawnPos.Length == 0)
        {
            GameObject SpawnParent = GameObject.Find("SpawnPoint");
            MonsterRespawnPos = SpawnParent.transform.GetChild(0).GetComponentsInChildren<Transform>();
        }
        nowWaveNum = waveNum;
        StartCoroutine(Co_SpawnMonster());
    }

    IEnumerator Co_SpawnMonster()
    {
        for (int i = 0; i < eachWaveMonCnt[nowWaveNum]/3; i++)
        {
            GameManager.instance.CreateAddMonster(3);

            GameObject monster_A = Instantiate(MonsterPrefabList[Random.Range(0, 3)], MonsterRespawnPos[0].position, MonsterRespawnPos[0].rotation);
            monster_A.transform.parent = this.gameObject.transform;

            GameObject monster_B = Instantiate(MonsterPrefabList[Random.Range(0, 3)], MonsterRespawnPos[1].position, MonsterRespawnPos[1].rotation);
            monster_B.transform.parent = this.gameObject.transform;

            GameObject monster_C = Instantiate(MonsterPrefabList[Random.Range(0, 3)], MonsterRespawnPos[2].position, MonsterRespawnPos[2].rotation);
            monster_C.transform.parent = this.gameObject.transform;

            yield return new WaitForSeconds(2.5f);
        }       
    }
}
