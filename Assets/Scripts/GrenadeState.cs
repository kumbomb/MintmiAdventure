using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GrenadeState : MonoBehaviour
{
    public Button thisButton;
    public int equipPos;
    [SerializeField] GameObject ChargingEffectObj;
    public ParticleSystem[] effectList;
    [SerializeField] Slider btnSlider;
    [SerializeField] Image itemImg;
    [SerializeField] float coolTime = 6f;
    [SerializeField] float nowTime = 0f;

    void Start()
    {
        ChargingEffectObj.SetActive(false);
        thisButton.onClick.AddListener(ThrowGrenade);
        btnSlider.value = 1f;
        StopCoroutine("Co_CheckCoolTime");
    }

    public void ThrowGrenade()
    {
        if (GameManager.instance.playerScript.RetThrowState() != -1
            || GameManager.instance.playerScript.isSwap
            || GameManager.instance.playerScript.isThrow)
            return;

        thisButton.interactable = false;
        StartCoroutine("Co_CheckCoolTime");
        GameManager.instance.playerScript.ToggleThrow(equipPos);
    }

    IEnumerator Co_CheckCoolTime()
    {
        btnSlider.value = 0f;
        yield return null;

        while (nowTime <= coolTime)
        {
            nowTime += Time.deltaTime;
            btnSlider.value = nowTime / coolTime;
            yield return null;
        }
        ChargingEffectObj.SetActive(true);

        for (int i = 0; i < effectList.Length; i++)
            effectList[i].Play();

        nowTime = 0f;
        btnSlider.value = 1f;
        thisButton.interactable = true;
    }
}
