using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CrosshairUI : MonoBehaviour
{
    [Header("Crosshair Settings")]
    [SerializeField] private Image zoomOutDot;
    [SerializeField] private Image zoomInDot;

    [Header("Size Settings")]
    [SerializeField] private float normalSize = 40f;
    [SerializeField] private float aimSize = 35f;
    [SerializeField] private float zoomSize = 5f;
    [SerializeField] private float sizeTransitionSpeed = 10f;
    [SerializeField] private float alphaTransitionSpeed = 8f;

    private AimController aimController;
    //private WeaponController weaponController;
    private PlayerInput playerInput;
    private RectTransform rectTransform;

    private float targetSize;
    private float currentSize;
    private float targetOutAlpha;
    private float targetInAlpha;


    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        currentSize = normalSize;
        targetSize = normalSize;
        targetOutAlpha = 1f;
        targetInAlpha = 0f;
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
                //weaponController = p.GetComponent<WeaponController>();
                playerInput = p.GetComponent<PlayerInput>();
                break;
            }
        }
    }

    private void UpdateCrosshairState()
    {
        if (playerInput == null) return;

        bool isZooming = playerInput.isZooming;
        bool isAiming = !isZooming && aimController.GetIsAiming();

        if (isZooming)
        {
            targetSize = zoomSize;
            targetOutAlpha = 0f;
            targetInAlpha = 1f;
        }
        else if (isAiming)
        {
            targetSize = aimSize;
            targetOutAlpha = 0f;
            targetInAlpha = 1f;
        }
        else
        {
            targetSize = normalSize;
            targetOutAlpha = 1f;
            targetInAlpha = 0f;
        }
    }

    private void UpdateCrosshairSize()
    {
        currentSize = Mathf.Lerp(currentSize, targetSize, sizeTransitionSpeed * Time.deltaTime);
        if (rectTransform != null)
            rectTransform.sizeDelta = new Vector2(currentSize, currentSize);

        // ¾ËÆÄ Lerp
        if (zoomOutDot != null)
        {
            Color c = zoomOutDot.color;
            c.a = Mathf.Lerp(c.a, targetOutAlpha, alphaTransitionSpeed * Time.deltaTime);
            zoomOutDot.color = c;
        }

        if (zoomInDot != null)
        {
            Color c = zoomInDot.color;
            c.a = Mathf.Lerp(c.a, targetInAlpha, alphaTransitionSpeed * Time.deltaTime);
            zoomInDot.color = c;
        }
    }
}
