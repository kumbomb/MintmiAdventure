using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HpBar : MonoBehaviour
{
    [SerializeField] Camera uiCamera;
    [SerializeField] Canvas canvas;
    [SerializeField] Image frontBar;
    [SerializeField] RectTransform rectParent;
    [SerializeField] RectTransform rectHp;

    //캐릭터에서 떨어져서 표현할 위치값
    [HideInInspector] public Vector3 offset = Vector3.zero;
    [HideInInspector] public Transform targetTr;
    public bool setHpBar;

    // Start is called before the first frame update
    void Start()
    {
        canvas = GetComponentInParent<Canvas>();
        uiCamera = canvas.worldCamera;
        rectParent = canvas.GetComponent<RectTransform>();
        rectHp = GetComponent<RectTransform>();
    }

    public void UpdateHp(int current, int max)
    {
        frontBar.fillAmount = (float)current / (float)max;
    }

    private void LateUpdate()
    {
        if(!setHpBar)
            return;

        //3d 좌표를 2d 좌표로 변경
        var screenPos = Camera.main.WorldToScreenPoint(targetTr.position + offset);

        if(screenPos.z < 0.0f)
            screenPos.z *= -1.0f;

        var localPos = Vector2.zero;
        //스크린좌표 -> ui canvas 좌표로 변경
        RectTransformUtility.ScreenPointToLocalPointInRectangle(rectParent, screenPos, uiCamera, out localPos);

        rectHp.localPosition = localPos;
    }
}
