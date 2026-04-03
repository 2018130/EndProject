using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerSkill : MonoBehaviour
{
    private BaseSkill currentSkill;
    private PlayerInput playerInput;
    private PlayerNetwork playerNetwork;

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
    }

    private void OnDestroy()
    {
        if (playerInput != null)
            playerInput.OnSkillPerformed -= OnSkillPerformed;
    }

    private void OnSkillPerformed()
    {
        Debug.Log($"OnSkillPerformed »£√‚µ , currentSkill: {currentSkill}");
        currentSkill?.Execute(playerNetwork);
    }

    public void SetSkill(CardData card)
    {
        currentSkill = SkillFactory.Create(card);
    }
}
