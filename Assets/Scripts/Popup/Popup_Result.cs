using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Popup_Result : MonoBehaviour
{    
    [SerializeField] Image popupBg;
    [SerializeField] GameObject clearObj;
    [SerializeField] Text clearText;
    [SerializeField] GameObject failObj;
    [SerializeField] Text failText;
    [SerializeField] GameObject LobbyBtn;

    System.Action callBack;

    public void SetDelegate(bool flag = false)
    {
        callBack = ShowPopup;
    }

    private void OnEnable()
    {
        callBack?.Invoke();
        if (callBack == null)
        {
            failObj.SetActive(false);
            clearObj.SetActive(false);
            LobbyBtn.SetActive(false);
        }
    }

    private void OnDisable()
    {
        callBack = null;
    }

    public void ShowPopup()
    {
        GameManager.instance.ToggleHpbar(true);
        switch (GameManager.instance.nowGameResultState)
        {
            case GameResultState.None:
                {
                    failObj.SetActive(false);
                    clearObj.SetActive(false);
                    LobbyBtn.SetActive(false);
                }
                break;

            case GameResultState.Win:
                {
                    failObj.SetActive(false);
                    clearObj.SetActive(true);
                    LobbyBtn.SetActive(false);
                    LeanTween.delayedCall(2f, 
                        () => 
                        {
                            this.gameObject.SetActive(false);
                            GameManager.instance.ToggleHpbar(false);
                        });
                }
                break;

            case GameResultState.Lose:
                {
                    failObj.SetActive(true);
                    clearObj.SetActive(false);
                    LobbyBtn.SetActive(true);
                }
                break;
        }
    }

    public void ClickLobbyBtn()
    {
        GameManager.instance.GoLobby();
    }
}
