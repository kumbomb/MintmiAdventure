using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class Item : MonoBehaviour
{
    public ItemType thisItemType;
    public WeaponType thisWeaponType;
    public int value;

    [SerializeField] Rigidbody rigid;
    [SerializeField] Transform target;
    [SerializeField] SphereCollider sphereCollider;
    [SerializeField] ParticleSystem[] DropEffect;

    bool isStartChase = false;

    float velocity;
    float accelaration;
    Vector3 direction;

    private void Start()
    {
        rigid = GetComponent<Rigidbody>();
        sphereCollider.enabled = false;
    }

    private void OnDisable()
    {
        isStartChase = false;
        velocity = 0f;
        sphereCollider.enabled = false;
    }

    public void SettingItem()
    {
        for (int i = 0; i < DropEffect.Length; i++)
            DropEffect[i].Play();

        if (thisItemType == ItemType.GoldCoin)
            value = Random.Range(1, 11);

        if (GameManager.instance.nowPlayerCharacter != null)
            target = GameManager.instance.nowPlayerCharacter.transform;

        DOVirtual.DelayedCall(0.5f, () =>
        {
            isStartChase = true;
            sphereCollider.enabled = true;
        });
    }

    private void MoveToPlayer()
    {
        if (isStartChase)
        {
            if (GameManager.instance.nowPlayerCharacter == null)
                return;

            target = GameManager.instance.nowPlayerCharacter.transform;
            if (target == null)
                return;

            direction = (target.position - transform.position).normalized;
            accelaration = 3f;
            velocity = velocity + accelaration * Time.deltaTime;
            transform.position = new Vector3(
                transform.position.x + direction.x * velocity,
                transform.position.y + direction.y * velocity,
                transform.position.z + direction.z * velocity);
        }
    }

    private void Update()
    {
        transform.Rotate(Vector3.up * 360 * Time.deltaTime);
        MoveToPlayer();
    }
}
