using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Tower : MonoBehaviour
{
    [SerializeField] TowerType thisTowerType;
    [SerializeField] GameObject towerObj;
    [SerializeField] GameObject towerRange;
    [SerializeField] ParticleSystem[] towerEffect;
    [SerializeField] ParticleSystem spawnEffect;

    bool isStart = false;

    public float lifeTime = 5f;
    float nowTime = 0f;
    
    private void Start()
    {
        isStart = false;
    }

    private void OnDisable()
    {
        nowTime = 0f;
        towerObj.transform.localScale = Vector3.zero;
        //LeanTween.cancelAll();
    }

    public void SettingTower()
    {
        ActiveSpawnEffect();
        towerObj.transform.localScale = Vector3.zero;
        LeanTween.scale(towerObj, Vector3.one, .5f);
        LeanTween.delayedCall(.6f, ActiveAttackEffect);
    }

    void ActiveSpawnEffect()
    {
        spawnEffect.Play();
        towerRange.SetActive(false);
        for (int i = 0; i < towerEffect.Length; i++)
            towerEffect[i].Stop();
    }
    
    void ActiveAttackEffect()
    {
        for (int i = 0; i < towerEffect.Length; i++)
            towerEffect[i].Play();
        spawnEffect.Stop();
        isStart = true;
        towerRange.SetActive(true);
    }

    void DestroyTower()
    {
        ActiveSpawnEffect();
        LeanTween.scale(towerObj, Vector3.zero, 0.5f);
        Destroy(this.gameObject, 0.6f);
    }

    private void Update()
    {
        if(isStart)
        {
            nowTime += Time.deltaTime;
            if(nowTime >= lifeTime)
            {
                isStart = false;
                DestroyTower();
            }
        }
        else
        {
            nowTime = 0f;
        }
    }
}
