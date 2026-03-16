using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BossRock : Bullet
{
    [SerializeField] Rigidbody rigid;
    [SerializeField] float angularPower = 2f;
    [SerializeField] float scaleValue = 0.1f;
    [SerializeField] bool isShot;

    private void OnEnable()
    {
        isShot = false;
        angularPower = 2f;
        scaleValue = 0.1f;
        transform.localScale = Vector3.one * scaleValue;
        rigid.linearVelocity = Vector3.zero;
        rigid.angularVelocity = Vector3.zero;
        StopAllCoroutines();
        StartCoroutine(GainPowerTimer());
        StartCoroutine(GainPower());
    }

    private void OnDisable()
    {
        StopAllCoroutines();
        isShot = false;
        rigid.linearVelocity = Vector3.zero;
        rigid.angularVelocity = Vector3.zero;
    }

    IEnumerator GainPowerTimer()
    {
        yield return new WaitForSeconds(2.2f);
        isShot = true;
    }

    IEnumerator GainPower()
    {
        while (!isShot)
        {
            angularPower += 0.02f;
            scaleValue += 0.005f;
            transform.localScale = Vector3.one * scaleValue;
            rigid.AddTorque(transform.right * angularPower, ForceMode.Acceleration);
            yield return null;
        }
    }
}
