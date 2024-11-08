using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BtnClickAnim : MonoBehaviour
{
    [SerializeField] GameObject obj;
    [SerializeField] Vector3 scaleValue;
    [SerializeField] float animTime;

    private void Start()
    {
        if (obj == null)
            obj = transform.gameObject;
    }
    public void StartClickAnim()
    {
        LeanTween.scale(obj, scaleValue, animTime / 2f);
        LeanTween.delayedCall(animTime / 2f, 
            () => {
                LeanTween.scale(obj, Vector3.one, animTime / 2f);
            });

    }
}
