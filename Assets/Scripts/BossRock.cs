using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BossRock : Bullet
{
    [SerializeField] Rigidbody rigid;
    [SerializeField] float angularPower = 2;
    [SerializeField] float scaleValue = 0.1f;
    [SerializeField] bool isShot;

    // Start is called before the first frame update
    void Start()
    {
        StartCoroutine(GainPowerTimer());
        StartCoroutine(GainPower());
    }
    IEnumerator GainPowerTimer()
    {
        yield return new WaitForSeconds(2.2f);
        isShot = true;
    }
    IEnumerator GainPower()
    {
        while(!isShot)
        {
            angularPower += 0.02f;
            scaleValue += 0.005f;
            transform.localScale = Vector3.one;
            rigid.AddTorque(transform.right * angularPower, ForceMode.Acceleration);
            yield return null;
        }
    }
}
