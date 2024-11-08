using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bullet : MonoBehaviour
{
    public int damage;
    public bool isMelee;
    public bool isRock;

    public float remainMaxTime = 3f;

    public float remainTime = 0f;

    private void OnDisable()
    {
        CancelInvoke();
        remainTime = 0f;
    }

    private void Update()
    {
        if(!isMelee)
        {
            remainTime += Time.deltaTime;
            if (remainTime >= remainMaxTime)
            {
                DelayToHide(0f);
                remainTime = 0f;
            }
        }      
    }

    private void OnCollisionEnter(Collision collision)
    {
        if(!isRock && collision.gameObject.CompareTag("Floor"))
        {
            //Destroy(this.gameObject, 1.5f);
            DelayToHide(1.5f);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!isMelee && other.gameObject.CompareTag("Wall"))
        {
            //Destroy(this.gameObject);
            DelayToHide(0f);
        }
    }

    public void DelayToHide(float delayTime = 0f)
    {
        Invoke("HideObject", delayTime);
    }
    public void HideObject()
    {
        gameObject.SetActive(false);
    }
}
