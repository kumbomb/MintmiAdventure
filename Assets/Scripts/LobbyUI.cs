using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LobbyUI : MonoBehaviour
{
    [SerializeField] GameObject stageNum;
    [SerializeField] GameObject goldText;
    [SerializeField] GameObject[] weaponList;
    [SerializeField] GameObject[] apcList;

    private void Start()
    {
        SettingStageNum();
        SettingApcState();
        ChangeWeaponBtn();
        UpdateGold();
    }

    public void ChangeWeaponBtn()
    {
        for (int i = 0; i < weaponList.Length; i++)
            weaponList[i].GetComponent<SelectWeaponBtn>().ChangeWeaponState(false);

        weaponList[GameManager.instance.curPlayerWeaponNum].GetComponent<SelectWeaponBtn>().ChangeWeaponState(true);
    }

    public void SettingStageNum()
    {
        int maxStageCount = GetStageCount();
        string txt = GameManager.instance.nowStageNum < maxStageCount
            ? "Stage : " + (GameManager.instance.nowStageNum + 1)
            : "Stage Complete";

        stageNum.GetComponent<Text>().text = txt;
    }

    int GetStageCount()
    {
        TextAsset stageData = Resources.Load<TextAsset>("ResourcesData/Stage");
        if (stageData == null)
            return 0;

        string[] lines = stageData.text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        return lines.Length;
    }

    public void GoStage()
    {
        GameManager.instance.GoGame();
    }

    public void UpdateGold()
    {
        goldText.GetComponent<Text>().text = GameManager.instance.nowGetCoin.ToString();
    }

    public void SettingApcState()
    {
        for (int i = 0; i < apcList.Length; i++)
        {
            apcList[i].GetComponent<SelectSubPlayerBtn>().InitState();
        }
    }
}
