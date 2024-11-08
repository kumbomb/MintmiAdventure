using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class Joystick : MonoBehaviour, IDragHandler, IPointerDownHandler, IPointerUpHandler
{
    [SerializeField] GameObject JoyStickChildObj;
    [SerializeField] Image joystickBgImg;
    [SerializeField] Image joystickImg;
    [SerializeField] Camera cam;
    Vector2 posInput;
    
    public bool isTapDodge = false;
    public bool isTapJump = false;
    public bool isThrowGrenade = false;

    // Start is called before the first frame update
    void Start()
    {
        JoyStickChildObj.SetActive(false);
        cam = GameObject.Find("UI_Cam").GetComponent<Camera>();
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            joystickBgImg.rectTransform,
            eventData.position,
            eventData.pressEventCamera,
            out posInput))
        {
            posInput.x = posInput.x / (joystickBgImg.rectTransform.sizeDelta.x);
            posInput.y = posInput.y / (joystickBgImg.rectTransform.sizeDelta.y);

            //normalized
            if(posInput.magnitude > 1.0f)
            {
                posInput = posInput.normalized;
            }

            //move
            joystickImg.rectTransform.anchoredPosition = new Vector2(
                posInput.x * (joystickBgImg.rectTransform.sizeDelta.x / 2),
                posInput.y * (joystickBgImg.rectTransform.sizeDelta.y / 2));
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        var pos = new Vector3(eventData.pressPosition.x, eventData.pressPosition.y, 100f);
        pos = cam.ScreenToWorldPoint(pos);

        JoyStickChildObj.transform.position = pos;//eventData.position;
        JoyStickChildObj.SetActive(true);
        OnDrag(eventData);
    }

    //pos reset 
    public void OnPointerUp(PointerEventData eventData)
    {
        posInput = Vector2.zero;
        joystickImg.rectTransform.anchoredPosition = Vector2.zero;
        JoyStickChildObj.SetActive(false);
    }

    public float inputHorizontal()
    {
        if (posInput.x != 0)
            return posInput.x;
        else
            return Input.GetAxis("Horizontal");
    }
    public float inputVertical()
    {
        if (posInput.y != 0)
            return posInput.y;
        else
            return Input.GetAxis("Vertical");
    }

    public void Dodge()
    {
        if (!isTapDodge)
        {
            isTapDodge = true;
            Invoke("ReverseDodge", 0.1f);
        }
    }

    void ReverseDodge()
    {
        isTapDodge = !isTapDodge;
    }
    void ReverseThrow()
    {
        isThrowGrenade = !isThrowGrenade;
    }
    public void Throw()
    {
        if (!isThrowGrenade)
        {
            isThrowGrenade = true;
            Invoke("ReverseThrow", 0.1f);
        }
    }
    
    bool checkExistPlayer()
    {
        return true;
    }
    
    #region DoubleTap 처리 (일단 패스
    //public Vector3 retTapPos()
    //{
    //    return tapPos;
    //}
    //private void Update()
    //{
    //    if (doubleTapCheck)
    //    {
    //        nowTapTime += Time.deltaTime;
    //        if (tapCnt == 1)
    //        {
    //            if (nowTapTime > maxTapTime)
    //            {
    //                isDoubleTap = false;
    //                doubleTapCheck = false;
    //                tapCnt = 0;
    //                nowTapTime = 0f;
    //            }
    //        }
    //        else if (tapCnt == 2)
    //        {
    //            if (nowTapTime > maxTapTime)
    //            {
    //                isDoubleTap = false;
    //                doubleTapCheck = false;
    //                tapCnt = 0;
    //                nowTapTime = 0f;
    //            }
    //            else
    //            {
    //                isDoubleTap = true;
    //                tapCnt = 0;
    //            }
    //        }
    //        else if (tapCnt == 0)
    //        {
    //            tapCnt = 0;
    //            nowTapTime = 0f;
    //            doubleTapCheck = false;
    //        }
    //    }
    //    else
    //    {
    //        isDoubleTap = false;
    //    }
    //}
    #endregion
}
