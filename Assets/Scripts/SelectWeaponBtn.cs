using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SelectWeaponBtn : MonoBehaviour
{
    [SerializeField] WeaponType thisType;
    [SerializeField] GameObject CheckMark;

    public void SelectWeapon()
    {
        GameManager.instance.curPlayerWeaponNum = (int)thisType;
    }

    public void ChangeWeaponState(bool state)
    {
        CheckMark.SetActive(state);
    }
}
