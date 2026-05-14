using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EndingController : MonoBehaviour
{
    [SerializeField]
    private List<Transform> playerSpawnPoint = new List<Transform>();
    [SerializeField]
    private Vector3 endingCamOffset;
    [SerializeField]
    private float endingFov;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        GameManager.Instance.OnEndGame += SetPlayerPosition;
        GameManager.Instance.OnEndGame += SetCameraSetting;
    }

    private void SetPlayerPosition(Faction faction)
    {
        PlayerNetwork[] playerCharacters = FindObjectsByType<PlayerNetwork>(FindObjectsSortMode.None);
        int idx = 1;

        foreach(var playerCharacter in playerCharacters)
        {
            if(playerCharacter.GetComponent<PlayerHealth>().PlayerFactionInt.Value == (int)faction)
            {
                if (playerCharacter.IsOwner)
                {
                    playerCharacter.transform.position = playerSpawnPoint[0].position;

                    Vector3 targetPos = Camera.main.transform.position;
                    targetPos.y = playerCharacter.transform.position.y; // Y축 고정
                    //playerCharacter.transform.LookAt(targetPos);
                    playerCharacter.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
                }
                else
                {
                    if (idx >= playerSpawnPoint.Count)
                    {
                        Debug.LogWarning($"배열 범위를 초과했습니다.");
                        return;
                    }

                    playerCharacter.transform.position = playerSpawnPoint[idx++].position;
                }

            }
        }

    }

    private void SetCameraSetting(Faction faction)
    {
        // 1. 모든 PlayerCameraController를 찾습니다.
        PlayerCameraController[] camControllers = FindObjectsByType<PlayerCameraController>(FindObjectsSortMode.None);
        PlayerCameraController localCamController = null;

        // 2. 순회하면서 실제 내 소유(Owner)인 카메라만 찾아냅니다.
        foreach (var cam in camControllers)
        {
            if (cam.IsOwner)
            {
                localCamController = cam;
                break; // 찾았으면 반복문 종료
            }
        }

        // 3. 내 카메라를 찾았다면 엔딩 세팅을 적용합니다.
        if (localCamController != null)
        {
            localCamController.SetLookAtTarget(playerSpawnPoint[0]);
            localCamController.SetCameraOffset(endingCamOffset);
            localCamController.SetCameraFOV(endingFov);
            localCamController.SetCameraRotate(false);
        }
        else
        {
            Debug.LogWarning("로컬 플레이어의 PlayerCameraController를 찾을 수 없습니다.");
        }

    }
}
