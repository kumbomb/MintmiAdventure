using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.SceneManagement;

public class TitleUI : MonoBehaviour
{
    void Start()
    {
        DOVirtual.DelayedCall(1f, () => { GameManager.instance.GoLobby(); });
    }
}
