using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum ObjectType
{
    Player,
    SubPlayer,
    Monster,
    PlayerBullet,
    MonsterBullet,
    Tower,
    Item,
}

public enum ItemType
{
    GoldCoin,
    Block,
    Max
}

public enum BlockType
{
    Red,
    Yellow,
    Green,
    Blue,
    Purple,
    Max
}

public enum WeaponType
{
    None = -1,
    Hammer,
    HandGun,
    SubMachineGun,
    HandShotGun,
    Axe,
    AxeShort,
    Crossbow,
    Shield,
    LongShield,
    Shotgun,
    Spear,
    SpearShort,
    Staff,
    Wand,
    Sword,
    SwordShort,
    FreezeGrenade,
    BlazeGrenade,
    PoisonGrenade,
    Max
}

public enum WeaponBehaviour
{
    None = 0,

    Hand = 1,
    HandMagic = 2,

    Single = 3,
    SingleMagic = 4,
    SingleGun = 5,

    Double = 6,
    DoubleGun = 7,

    TwoHands = 8,
    TwoHandsMagic = 9,
    TwoHandsGun = 10
}

public enum AttackType
{
    //근거리
    Melee,
    //원거리
    Range
}

public enum MonsterType
{
    EnemyA,
    EnemyB,
    EnemyC,
    EnemyBoss
}

public enum TowerType
{
    Freeze,
    Blaze,
    Poison,
    Thunder,
    Heal,
    Max
}

public enum BuffType
{
    Slow,
    Burn,
    Addiction,
    ElectricShock,
    PlayerHeal,
    Max
}

//서포터 애니메이션 타입
public enum CharacterType
{
    _2L2H = 0,
    _2L = 1,
    _4L2H = 2,
    _4L = 3
}
// 한손에, 양손에 하나씩, 두손에 하나 
public enum ItemBehaviour
{
    None = 0,
    Single = 1,
    Double = 2,
    TwoHands = 3,
}

[System.Serializable]
public class WeaponData
{
    public AttackType attackType;
    public WeaponType weaponType;
    public int damage;
    public float rate;
    public float detectRadius;
    public float atkRadius;
    public BoxCollider meleeArea;
    public TrailRenderer trailEffect;
    public Transform bulletPos;
}

[System.Serializable]
public struct WeaponSetInfo
{
    public AttackType atkType
    {
        get
        {
            return attackType;
        }
    }

    public WeaponType weaponType
    {
        get
        {
            return _type;
        }
    }

    public WeaponBehaviour WeaponBehaviour
    {
        get
        {
            return weaponBehaviour;
        }
    }

    public float MinDamage
    {
        get
        {
            return minDamage;
        }
    }

    public int MaxDamage
    {
        get
        {
            return maxDamage;
        }
    }

    public int Damage
    {
        get
        {
            return Random.Range(minDamage, maxDamage);
        }
    }

    //public float AttackSpeed1
    //{
    //    get
    //    {
    //        return attackSpeed1;
    //    }
    //}

    //public float AttackSpeed2
    //{
    //    get
    //    {
    //        return attackSpeed2;
    //    }
    //}

    //public float AttackRange
    //{
    //    get
    //    {
    //        return attackRange;
    //    }
    //}

    //근거리 원거리 
    [SerializeField]
    public AttackType attackType;
    //무기 종류
    [SerializeField]
    public WeaponType _type;
    //무기별 애니메이션
    [SerializeField]
    public WeaponBehaviour weaponBehaviour;
    [SerializeField]
    //[Range(0f, 1000f)]
    public int minDamage;
    [SerializeField]
    //[Range(0f, 1000f)]
    public int maxDamage;
    //공격 딜레이 시간
    [SerializeField]
    [Range(0f, 1000f)]
    public float rate;
    [SerializeField]
    [Range(0f, 1000f)]
    public float detectRadius;
    [SerializeField]
    [Range(0f, 1000f)]
    public float atkRadius;

    [Header("근거리용")]
    //공격 영역 (= 근거리)
    [SerializeField]
    public BoxCollider meleeArea;
    //Trail 효과 (= 근거리)
    [SerializeField]
    public TrailRenderer trailEffect;
    //Bullet 발사 위치 ? ( = 원거리 )
    [Header("원거리용")]
    [SerializeField]
    public Transform bulletPos;

    //[SerializeField]
    //[Range(0.1f, 100f)]
    //public float attackSpeed1;
    //[SerializeField]
    //[Range(0.1f, 100f)]
    //public float attackSpeed2;
    //[SerializeField]
    //[Range(0.01f, 100f)]
    //public float attackRange;

    public void Default()
    {
        _type = WeaponType.None;
        weaponBehaviour = WeaponBehaviour.Hand;
        minDamage = 5;
        maxDamage = 6;
        //attackSpeed1 = 4;
        //attackSpeed2 = 3;
        //attackRange = 1;
    }
}
[System.Serializable]
public struct BehaviourSetting
{
    public CharacterType characterType;
    [Range(0.01f, 10f)]
    public float RunSpeed;
    [Range(0.01f, 10f)]
    public float WalkSpeed;
    [Range(0f, 30f)]
    public int JumpPower;
    [Tooltip("How many times can this character jump before landing. ")]
    [Range(0, 5)]
    public int JumpCount;
    [Range(1f, 40f)]
    public float DashSpeed;
    [Range(0f, 10f)]
    public float DashDistance;
    [Range(0.1f, 2f)]
    public float DashGap;
    //public WeaponSetInfo DefaultHand;

    public bool Is2L
    {
        get
        {
            return characterType == CharacterType._2L;
        }
    }
    public bool Is2L2H
    {
        get
        {
            return characterType == CharacterType._2L2H;
        }
    }
    public bool Is4L
    {
        get
        {
            return characterType == CharacterType._4L;
        }
    }
    public bool Is4L2H
    {
        get
        {
            return characterType == CharacterType._4L2H;
        }
    }

    public bool HasHand
    {
        get
        {
            return Is2L2H || Is4L2H;
        }
    }
    public bool Has2L
    {
        get
        {
            return Is2L || Is2L2H;
        }
    }
    public bool Has4L
    {
        get
        {
            return Is4L || Is4L2H;
        }
    }
}

[System.Serializable]
public struct ItemHolder
{
    public Transform HeadHolder, ArmLHolder, ArmRHolder, HandLHolder, HandRHolder;
    public Transform BodyHolder, LegLHolder, LegRHolder, FootLHolder, FootRHolder;
    public Transform Leg2LHolder, Leg2RHolder;
    public Transform Foot2LHolder, Foot2RHolder;
    public Transform TailHolder;

    //무기 양손 초기화
    public void ClearHand()
    {
        ClearHandL();
        ClearHandR();
    }
    public void ClearHand(bool left)
    {
        if (left)
            ClearHandL();
        else
            ClearHandR();
    }
    public void ClearHandL()
    {
        int len = HandLHolder.childCount;
        for (int i = 0; i < len; i++)
        {
            HandLHolder.GetChild(i).gameObject.SetActive(false);
            //Item.DespawnItem(HandLHolder.GetChild(0).GetComponent<Item>());
        }
    }
    public void ClearHandR()
    {
        int len = HandRHolder.childCount;
        for (int i = 0; i < len; i++)
        {
            HandLHolder.GetChild(i).gameObject.SetActive(false);
            //Item.DespawnItem(HandRHolder.GetChild(0).GetComponent<Item>());
        }
    }

}

[System.Serializable]
public struct CharacterModel
{
    public Transform HeadModel, ArmLModel, ArmRModel, HandLModel, HandRModel;
    public Transform BodyModel, LegLModel, LegRModel, FootLModel, FootRModel;

    public Transform Leg2LModel, Leg2RModel;
    public Transform Foot2LModel, Foot2RModel;
    public Transform TailModel;
}

[System.Serializable]
public struct CharacterOrgan
{
    public Transform Head, Body, ArmL, ArmR, HandL, HandR, LegL, LegR, FootL, FootR, Tail;
    public Transform Leg2L, Leg2R;
    public Transform Foot2L, Foot2R;
}


[System.Serializable]
public class ObjectPoolDataClass
{
    public ObjectType poolitemType;
    public string poolItemName = string.Empty;
    public GameObject prefab = null;
    public int poolCount = 0;
    public int nowCnt = 0;
    [SerializeField] List<GameObject> poolList = new List<GameObject>();

    public void Initialize(Transform parent = null)
    {
        nowCnt = 0;
        for (int i = 0; i < poolCount; i++)
        {
            GameObject gobj = CreateItem(parent);
            gobj.name = poolItemName + i;
            poolList.Add(gobj);
        }
    }
    public void Push(GameObject item, Transform parent = null)
    {
        item.transform.SetParent(parent);
        item.SetActive(false);
        poolList.Add(item);
    }
    public GameObject Pop(Transform parent = null)
    {
        if (poolList.Count == 0)
            poolList.Add(CreateItem(parent));
        GameObject item = poolList[nowCnt%poolCount];
        if (nowCnt == poolCount - 1)
            nowCnt = 0;
        else
            nowCnt++;
        return item;
    }
    private GameObject CreateItem(Transform parent = null)
    {
        GameObject item = Object.Instantiate(prefab) as GameObject;
        item.name = poolItemName ;
        item.transform.SetParent(parent);
        item.SetActive(false);
        return item;
    }

}



