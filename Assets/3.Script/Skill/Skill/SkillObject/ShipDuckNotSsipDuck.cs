using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShipDuckNotSsipDuck : MonoBehaviour
{
    [SerializeField] GameObject[] seats;
    [SerializeField] GameObject prefab;

    private Rigidbody rb;
    private Vector2 moveInput;
    private float moveSpeed;
    private int seatNumb;
    private int duration;
    private PlayerNetwork player;

    private void OnEnable()
    {
        //moveSpeed = CardData.Speed;
        //duration = CardData.duration;
        seatNumb = 0;
        StartCoroutine(StopSkill());
        TryGetComponent(out player);
    }

    private void OnDisable()
    {
        //터져서 모든 승객들 날라감 
        if(!player.IsGrounded())
        {
            //공격 불가 판정
        }
    }

    private void FixedUpdate()
    {
        Vector3 move = new Vector3(moveInput.x, 0, moveInput.y);
        rb.MovePosition(rb.position + move * moveSpeed * Time.fixedDeltaTime);

        if (move.magnitude > 0.1f)
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                Quaternion.LookRotation(move),
                0.15f
            );

        //animator.SetBool("IsMoving", move != Vector3.zero);
    }

    private void TakePassengers()
    {
        //범위 안의 애들 스킬 사용시 싣는지
        //애들 정보 저장
        //승객들 state = down 으로 변경 -> down은 조금씩 움직임 가능해서 새로운 state를 만들거나 움직임을 막는 방향으로 가기
    }

    private IEnumerator StopSkill()
    {
        yield return new WaitForSeconds(duration);
        Destroy(gameObject);
    }
}
