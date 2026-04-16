using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CrosshairUI : MonoBehaviour
{
    [Header("Crosshair Settings")]
    [SerializeField] private Image crosshairImage;
    [SerializeField] private RectTransform crosshairRect;

    [Header("Status Colors")]
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color aimColor = Color.red;
    [SerializeField] private Color zoomColor = Color.green;

    [Header("Size Settings")]
    [SerializeField] private float normalSize = 50f;
    [SerializeField] private float aimSize = 35f;
    [SerializeField] private float zoomSize = 20f;
    [SerializeField] private float transitionSpeed = 10f;

    private AimController targetAimController;
    private float currentTargetSize;
    private Color targetColor;


    private void Start()
    {
        currentTargetSize = normalSize;
        targetColor = normalColor;
    }

    private void Update()
    {
        if (targetAimController == null)
        {
            FindLocalPlayerController();
            return;
        }

        UpdateCrosshairState();
        ApplySmoothTransitions();
    }

    private void FindLocalPlayerController()
    {
        // 씬 내의 모든 PlayerHealth 중 내 것(IsOwner)을 찾음
        var players = FindObjectsByType<PlayerHealth>(FindObjectsSortMode.None);
        foreach (var player in players)
        {
            if (player.IsOwner)
            {
                targetAimController = player.GetComponent<AimController>();
                break;
            }
        }
    }

    private void UpdateCrosshairState()
    {
        if (targetAimController == null) return;

        // AimController의 상태에 따른 분기
        if (targetAimController.IsZooming)
        {
            currentTargetSize = zoomSize;
            targetColor = zoomColor;
        }
        else if (targetAimController.IsAiming)
        {
            currentTargetSize = aimSize;
            targetColor = aimColor;
        }
        else
        {
            currentTargetSize = normalSize;
            targetColor = normalColor;
        }
    }

    private void ApplySmoothTransitions()
    {
        // 크기 변경 부드럽게
        if (crosshairRect != null)
        {
            float size = Mathf.Lerp(crosshairRect.sizeDelta.x, currentTargetSize, Time.deltaTime * transitionSpeed);
            crosshairRect.sizeDelta = new Vector2(size, size);
        }

        // 색상 변경 부드럽게
        if (crosshairImage != null)
        {
            crosshairImage.color = Color.Lerp(crosshairImage.color, targetColor, Time.deltaTime * transitionSpeed);
        }
    }
}
