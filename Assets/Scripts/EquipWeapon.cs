using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EquipWeapon : MonoBehaviour
{
    public virtual string ItemName
    {
        get
        {
            return itemName;
        }
    }

    public virtual ItemBehaviour ItemBehaviourID
    {
        get
        {
            return itemBehaviourID;
        }
    }

    [SerializeField]
    private string itemName;
    [SerializeField]
    ItemBehaviour itemBehaviourID;

}
