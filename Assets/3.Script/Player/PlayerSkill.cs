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

    private SkillSlotUI skillSlotUI;

    // 물풍선
    private bool isHolding = false;
    [SerializeField] private float throwAngle = 0.3f; // 물풍선 각도

    // 스킬 쿨타임
    private float coolTimeRemaining = 0f;
    private bool isOnCooldown = false;

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
        if (isOnCooldown)
        {
            coolTimeRemaining -= Time.deltaTime;
            skillSlotUI?.UpdateCoolTime(coolTimeRemaining, currentCardData.Cooldown);

            if (coolTimeRemaining <= 0)
            {
                isOnCooldown = false;
                coolTimeRemaining = 0f;
            }
        }

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
        if (isOnCooldown) return;

        if (currentSkill is WaterBalloonSkill balloonSkill)
        {
            isHolding = !isHolding;
            if (isHolding)
                balloonSkill.StartHolding(playerNetwork);
            else
                balloonSkill.StopHolding();
        }
        else if (currentSkill is MalrangBongSkill)
        {
            // 말랑봉은 장착 시 쿨타임 없음 - 다른 무기로 스왑할 때 쿨타임 시작
            currentSkill.Execute(playerNetwork);
        }
        else
        {
            currentSkill?.Execute(playerNetwork);
            StartCooldown();
        }
    }

    // WeaponController에서 말랑봉 → 일반무기 스왑 시 호출
    public void StartMalrangBongCooldown()
    {
        if (currentSkill is MalrangBongSkill)// && currentCardData != null)
        {
            StartCooldown();
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
            throwAngle = 0.3f;
            StartCooldown();
        }
    }
    private void StartCooldown()
    {
        coolTimeRemaining = currentCardData.Cooldown;
        isOnCooldown = true;
    }


    public void SetSkill(CardData card)
    {
        isHolding = false;
        currentCardData = card;
        currentSkill = SkillFactory.Create(card);

        if (skillSlotUI == null)
            skillSlotUI = FindAnyObjectByType<SkillSlotUI>();

        skillSlotUI?.SetSkill(card);
    }
}