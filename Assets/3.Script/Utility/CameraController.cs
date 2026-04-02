using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    [SerializeField] private Transform target;  // 플레이어
    Vector3 offset = new Vector3(2f,2f, -5f);   // 카메라 오프셋
    private float followCameraSpeed = 5f;

    private void LateUpdate()
    {

        if (target == null) return;

        // 목표 위치 = 플레이어 위치 + 오프셋
        Vector3 targetPos = target.position + offset;

        // 부드럽게 카메라 이동
        transform.position = Vector3.Lerp(
            transform.position,
            targetPos,
            followCameraSpeed * Time.deltaTime
            );

        Vector3 lookTarget = target.position + Vector3.up + new Vector3(2f, 1f, 0);

        transform.LookAt(lookTarget);
    }
}
