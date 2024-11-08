using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectPool : MonoBehaviour
{
    public static ObjectPool instance;
    public List<ObjectPoolDataClass> objectPoolList = new List<ObjectPoolDataClass>();

    public Transform MonsterPool;
    public Transform PlayerBulletPool;
    public Transform MonsterBulletPool;
    public Transform TowerPool;
    public Transform ItemPool;
    
    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(this);
        }
        
        for (int i = 0; i < objectPoolList.Count; i++)
        {
            if (objectPoolList[i].poolitemType == ObjectType.Monster)
                objectPoolList[i].Initialize(MonsterPool);
            else if (objectPoolList[i].poolitemType == ObjectType.PlayerBullet)
                objectPoolList[i].Initialize(PlayerBulletPool);
            else if (objectPoolList[i].poolitemType == ObjectType.MonsterBullet)
                objectPoolList[i].Initialize(MonsterBulletPool);
            else if (objectPoolList[i].poolitemType == ObjectType.Tower)
                objectPoolList[i].Initialize(TowerPool);
            else if (objectPoolList[i].poolitemType == ObjectType.Item)
                objectPoolList[i].Initialize(ItemPool);
        }
    }

    public bool PushToPool(string itemName, GameObject gObj, Transform parent = null)
    {
        ObjectPoolDataClass pool = GetPoolItem(itemName);
        if (pool == null)
            return false;

        if (pool.poolitemType == ObjectType.Monster)
            pool.Push(gObj, parent == null ? MonsterPool : parent);
        else if (pool.poolitemType == ObjectType.MonsterBullet)
            pool.Push(gObj, parent == null ? MonsterBulletPool : parent);
        else if (pool.poolitemType == ObjectType.PlayerBullet)
            pool.Push(gObj, parent == null ? PlayerBulletPool : parent);
        else if (pool.poolitemType == ObjectType.Tower)
            pool.Push(gObj, parent == null ? TowerPool : parent);
        else if (pool.poolitemType == ObjectType.Item)
            pool.Push(gObj, parent == null ? ItemPool : parent);

        return true;
    }

    public GameObject PopFromPool(string itemName, Transform parent = null)
    {
        ObjectPoolDataClass pool = GetPoolItem(itemName);
        if (pool == null)
            return null;
        return pool.Pop(parent);
    }

    ObjectPoolDataClass GetPoolItem(string itemName)
    {
        for (int i = 0; i < objectPoolList.Count; ++i)
    {
            if (objectPoolList[i].poolItemName.Equals(itemName))
                return objectPoolList[i];
        }

        Debug.LogWarning("There's no matched pool list.");
        return null;
    }

    public void AllHide()
    {
        for (int i = 0; i < MonsterPool.childCount; i++)
        {
            if (MonsterPool.GetChild(i).gameObject.activeSelf)
                MonsterPool.GetChild(i).gameObject.SetActive(false);
        }
        for (int i = 0; i < PlayerBulletPool.childCount; i++)
        {
            if (PlayerBulletPool.GetChild(i).gameObject.activeSelf)
                PlayerBulletPool.GetChild(i).gameObject.SetActive(false);
        }
        for (int i = 0; i < MonsterBulletPool.childCount; i++)
        {
            if (MonsterBulletPool.GetChild(i).gameObject.activeSelf)
                MonsterBulletPool.GetChild(i).gameObject.SetActive(false);
        }
        for (int i = 0; i < TowerPool.childCount; i++)
        {
            if (TowerPool.GetChild(i).gameObject.activeSelf)
                TowerPool.GetChild(i).gameObject.SetActive(false);
        }
        for (int i = 0; i < ItemPool.childCount; i++)
        {
            if (ItemPool.GetChild(i).gameObject.activeSelf)
                ItemPool.GetChild(i).gameObject.SetActive(false);
        }
    }

    //public 
}
