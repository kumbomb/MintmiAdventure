using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OrbitGrenades : MonoBehaviour
{
    [SerializeField] Transform target;
    [SerializeField] float orbitSpeed;
    Vector3 offset;

    // Start is called before the first frame update
    void Start()
    {
        //수류탄 - 현재 플레이어 위치
        offset = transform.position - target.position;
    }

    // Update is called once per frame
    void Update()
    {
        transform.position = target.position + offset;

        //대상을 주위로 회전하는 함수
        //목표가 움직이면 위치가 일그러짐
        //위 아래 값으로 위치 보정
        transform.RotateAround(target.position, Vector3.up, orbitSpeed * Time.deltaTime);

        offset = transform.position - target.position;
    }
}
