using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum JumpPadType
{
    Vertical,   // 수직형 - 90도, 12~15f
    Diagonal    // 대각선형 - 45도, 수직형의 70~80%
}

public class JumpPad : MonoBehaviour
{
    [SerializeField] private JumpPadType padType;
    [SerializeField] private float verticalForce = 13f;  // 12~15f 중간값

    private void OnCollisionEnter(Collision collision)
    {
        if (!collision.gameObject.CompareTag("Player")) return;

        PlayerNetwork player = collision.gameObject.GetComponent<PlayerNetwork>();
        if (player == null) return;

        Vector3 jumpDir;
        float force;

        switch (padType)
        {
            case JumpPadType.Vertical:
                jumpDir = Vector3.up;
                force = verticalForce;
                break;
            case JumpPadType.Diagonal:
                // 45도 - 발판이 바라보는 방향으로 대각선
                jumpDir = (Vector3.up + transform.forward).normalized;
                force = verticalForce * 0.75f;  // 70~80% 중간값
                break;
            default:
                return;
        }

        player.ApplyJumpPad(jumpDir, force);
    }
}