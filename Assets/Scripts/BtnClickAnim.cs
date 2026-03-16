using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
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
        if (obj == null)
            return;

        obj.transform.DOKill();
        Sequence sequence = DOTween.Sequence();
        sequence.Append(obj.transform.DOScale(scaleValue, animTime / 2f));
        sequence.Append(obj.transform.DOScale(Vector3.one, animTime / 2f));
    }
}
