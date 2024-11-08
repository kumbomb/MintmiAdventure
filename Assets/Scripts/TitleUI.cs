using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
public class TitleUI : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        LeanTween.delayedCall(1f, () => { GameManager.instance.GoLobby(); });
    }
}
