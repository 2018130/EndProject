using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using Unity.Cinemachine;

public class PlayerCameraController : NetworkBehaviour
{
    private PlayerInput input;
    private CinemachineCameraOffset cameraOffset;

    [Header("Cinemachine Virtual Cameras")]
    [SerializeField] private CinemachineCamera cinemachineCam; 
    [SerializeField] private CinemachineBasicMultiChannelPerlin multiChannelPerlin;

    //[Header("Priority")]
    //[SerializeField] private int activePriority = 20;
    //[SerializeField] private int inactivePriority = 5;

    [Header("Zoom Setting")]
    [SerializeField] private bool overrideFOV = true;
    [SerializeField] private float normalFOV = 60f;
    [SerializeField] private float zoomFOV = 40f;

    [Space]
    [SerializeField] private Vector3 normalOffset = new Vector3(0, -0.5f, -1);
    [SerializeField] private Vector3 zoomOffset = new Vector3(1f, -1.5f, -1f);

    [SerializeField] private float lerpSpeed = 8f;

    private bool isZooming;
    //public bool IsZooming => isZooming;

    public override void OnNetworkSpawn()
    {
        if (!IsOwner)
        {
            enabled = false;
            return;
        }

        TryGetComponent(out input);

        if (cinemachineCam == null)
        {
            cinemachineCam = GameObject.FindAnyObjectByType<CinemachineCamera>();
            multiChannelPerlin = cinemachineCam.GetComponent<CinemachineBasicMultiChannelPerlin>();
            multiChannelPerlin.AmplitudeGain = 0f;
        }

        if (cinemachineCam != null)
        {
            cameraOffset = cinemachineCam.GetComponent<CinemachineCameraOffset>();
        }
    }

    private void Update()
    {
        if (input == null) return;
        isZooming = input.isZooming;
    }

    private void LateUpdate()
    {
        if (cinemachineCam == null) return;

        if (overrideFOV)
        {
            float targetFOV = isZooming ? zoomFOV : normalFOV;

            LensSettings lens = cinemachineCam.Lens;
            lens.FieldOfView = Mathf.Lerp(lens.FieldOfView, targetFOV, lerpSpeed * Time.deltaTime);
            cinemachineCam.Lens = lens;
        }

        if (cameraOffset != null)
        {
            Vector3 targetOffset = isZooming ? zoomOffset : normalOffset;
            cameraOffset.Offset = Vector3.Lerp(cameraOffset.Offset, targetOffset, lerpSpeed * Time.deltaTime);
        }
    }

    public void Shake(float intensity, float time)
    {
        if (multiChannelPerlin == null) return;

        multiChannelPerlin.AmplitudeGain = intensity;

        StartCoroutine(StopShake(time));
    }

    private IEnumerator StopShake(float time)
    {
        yield return new WaitForSeconds(time);

        multiChannelPerlin.AmplitudeGain = 0f;
    }
}
