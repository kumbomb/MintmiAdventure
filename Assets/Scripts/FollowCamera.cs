using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FollowCamera : MonoBehaviour
{
    [SerializeField] Transform target;
    //고정값
    [SerializeField] Vector3 offset;
    // Update is called once per frame
    public void SetTarget(Transform _target)
    {
        target = _target;
    }
    void Update()
    {
        if (target == null)
            return;
        transform.position = target.position + offset;
    }
}
