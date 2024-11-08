using System.Collections;
using System.Collections.Generic;
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

        target = GameManager.instance.nowPlayerCharacter.transform;
        LeanTween.delayedCall(0.5f, () => { isStartChase = true; sphereCollider.enabled = true; });
    }

    private void MoveToPlayer()
    {
        if (isStartChase)
        {
            target = GameManager.instance.nowPlayerCharacter.transform;
            if (target == null)
                return;

            //이동 방향 구하고
            direction = (target.position - transform.position).normalized;
            //가속도
            accelaration = 3f;
            velocity = (velocity + accelaration * Time.deltaTime);
            //객체간 거리 계산 => 제한 거리를 두고 싶을 경우 사용
            //float dist = Vector3.Distance(target.position, transform.position);
            //이동
            this.transform.position = new Vector3(transform.position.x + (direction.x * velocity),
                                                    transform.position.y + (direction.y * velocity),
                                                    transform.position.z + (direction.z * velocity));                    
        }
    }

    private void Update()
    {
        transform.Rotate(Vector3.up * 360 * Time.deltaTime);
        MoveToPlayer();
    }
}
