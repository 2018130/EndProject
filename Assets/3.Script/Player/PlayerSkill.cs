using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerSkill : MonoBehaviour
{
    private BaseSkill currentSkill;
    private PlayerInput playerInput;
    private PlayerNetwork playerNetwork;
    private CardData currentCardData;
    private bool isHolding = false;

    [SerializeField] private float throwAngle = 0.3f; // ¹°Ç³¼± °¢µµ

    private void Awake()
    {
        TryGetComponent(out playerInput);
        TryGetComponent(out playerNetwork);
    }

    private void Start()
    {
        if (!playerNetwork.IsOwner)
        {
            enabled = false;
            return;
        }
        playerInput.OnSkillPerformed += OnSkillPerformed;
        playerInput.OnFirePerformed += OnFirePerformed;
        playerInput.OnLookPerformed += OnLookPerformed;
    }

    private void Update()
    {
        if (isHolding && currentSkill is WaterBalloonSkill balloonSkill)
        {
            Vector3 throwDir = playerNetwork.transform.forward + Vector3.up * throwAngle;
            throwDir.Normalize();
            balloonSkill.UpdateTrajectory(
                playerNetwork.transform.position + Vector3.up * 1.5f,
                throwDir,
                currentCardData.Speed
            );
        }
    }

    private void OnDestroy()
    {
        if (playerInput != null)
        {
            playerInput.OnSkillPerformed -= OnSkillPerformed;
            playerInput.OnFirePerformed -= OnFirePerformed;
            playerInput.OnLookPerformed -= OnLookPerformed;
        }

    }

    private void OnSkillPerformed()
    {
        if (currentSkill is WaterBalloonSkill balloonSkill)
        {
            isHolding = !isHolding;
            if (isHolding)
                balloonSkill.StartHolding(playerNetwork);
            else
                balloonSkill.StopHolding();
        }
        else
        {
            currentSkill?.Execute(playerNetwork);
        }
    }

    private void OnLookPerformed(Vector2 look)
    {
        if (!isHolding) return;
        throwAngle += look.y * 0.001f;
        throwAngle = Mathf.Clamp(throwAngle, 0f, 0.8f);
    }

    private void OnFirePerformed()
    {
        if (isHolding && currentSkill is WaterBalloonSkill balloonSkill)
        {
            balloonSkill.Throw(playerNetwork, throwAngle);
            isHolding = false;
        }
    }

    public void SetSkill(CardData card)
    {
        isHolding = false;
        currentCardData = card;
        currentSkill = SkillFactory.Create(card);
    }
}
