using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Popup_Pause : MonoBehaviour
{
    [SerializeField] GameObject retBtn;
    [SerializeField] GameObject lobbyBtn;
    
    public void ClickRetBtn()
    {
        GameManager.instance.SetTimeScale(false);
        this.gameObject.SetActive(false);
    }
    public void ClickLobbyBtn()
    {
        GameManager.instance.SetTimeScale(false);
        GameManager.instance.GoLobby();
    }
}
