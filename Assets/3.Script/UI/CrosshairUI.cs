using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CrosshairUI : MonoBehaviour
{
    [Header("Crosshair Settings")]
    [SerializeField] private Image crosshairImage;
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color aimColor = Color.red;
    [SerializeField] private Color zoomColor = Color.green;

    [Header("Size Settings")]
    [SerializeField] private float normalSize = 40f;
    [SerializeField] private float aimSize = 35f;
    [SerializeField] private float zoomSize = 5f;
    [SerializeField] private float sizeTransitionSpeed = 10f;

    private AimController aimController;
    private WeaponController weaponController;
    private PlayerInput playerInput;
    private RectTransform rectTransform;

    private float targetSize;
    private float currentSize;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        currentSize = normalSize;
    }

    private void Update()
    {
        if (aimController == null)
        {
            FindLocalPlayer();
            return;
        }

        UpdateCrosshairState();
        UpdateCrosshairSize();
    }

    private void FindLocalPlayer()
    {
        var players = GameObject.FindGameObjectsWithTag("Player");
        foreach (var p in players)
        {
            var netObj = p.GetComponent<Unity.Netcode.NetworkObject>();
            if (netObj != null && netObj.IsOwner)
            {
                aimController = p.GetComponent<AimController>();
                weaponController = p.GetComponent<WeaponController>();
                playerInput = p.GetComponent<PlayerInput>();
                break;
            }
        }
    }

    private void UpdateCrosshairState()
    {
        if (playerInput == null) return;

        bool isZooming = playerInput.isZooming;
        bool isAiming = aimController.GetIsAiming();

        if (isZooming)
        {
            targetSize = zoomSize;
            if (crosshairImage != null) crosshairImage.color = zoomColor;
        }
        else if (isAiming)
        {
            targetSize = aimSize;
            if (crosshairImage != null) crosshairImage.color = aimColor;
        }
        else
        {
            targetSize = normalSize;
            if (crosshairImage != null) crosshairImage.color = normalColor;
        }
    }

    private void UpdateCrosshairSize()
    {
        currentSize = Mathf.Lerp(currentSize, targetSize, sizeTransitionSpeed * Time.deltaTime);
        if (rectTransform != null)
            rectTransform.sizeDelta = new Vector2(currentSize, currentSize);
    }
}
