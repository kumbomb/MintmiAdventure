using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SelectSubPlayerBtn : MonoBehaviour
{
    [SerializeField] int thisApcNum = 0;
    [SerializeField] GameObject CheckMark;

    public void InitState()
    {
        if(GameManager.instance.subPlayerIdxList == null || GameManager.instance.subPlayerIdxList.Count <= 0)
            ToggleCheckMark(false);
        else
        {
            ToggleCheckMark(GameManager.instance.subPlayerIdxList.Contains(thisApcNum));
        }
    }

    public void SelectApc()
    {
        if (!GameManager.instance.subPlayerIdxList.Contains(thisApcNum))
        {
            GameManager.instance.subPlayerIdxList.Add(thisApcNum);
            ToggleCheckMark(true);
        }
        else
        {
            GameManager.instance.subPlayerIdxList.RemoveAt(GameManager.instance.subPlayerIdxList.FindIndex(t => t == thisApcNum));
            ToggleCheckMark(false);
        }
    }

    void ToggleCheckMark(bool flag)
    {
        CheckMark.SetActive(flag);
    }
}
