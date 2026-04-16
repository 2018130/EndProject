using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Cinemachine;
using Unity.Netcode;
using UnityEngine;

public class PlayerNetwork : NetworkBehaviour
{
    private Rigidbody rb;   // RigidBody
    private Animator animator;      // 애니메이터
    private Vector2 moveInput;      // 이동벡터

    [SerializeField] private float moveSpeed = 5f; // 이동 스피드
    [SerializeField] private float jumpForce = 5f; // 점프
    [SerializeField] private float jetpackForce = 8f; // 제트팩
    [SerializeField] private float dashForce = 10f; // 대쉬
    [SerializeField] private float dashDuration = 0.3f; // 대쉬 지속 시간
    [SerializeField] private float dashCooldown = 1f; //대쉬 쿨타임

    private float lastDashTime; // 마지막 대쉬한 시간
    private float jumpPressTime; //점프버튼을 누른 시간

    private bool isJetpacking; // 제트팩 사용 중인지
    private bool isDashing;    // 대쉬 중인지

    private ZoneInteraction currentZone; // 살리기, 처형


    private void Awake()
    {
        TryGetComponent<Rigidbody>(out rb);
        TryGetComponent<Animator>(out animator);
    }

    public override void OnNetworkSpawn()
    {
        if (!IsOwner) return;

        SpawnPlayerCall_Rpc(OwnerClientId);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // 시네머신 카메라 연결
        CinemachineCamera virtualCam = FindAnyObjectByType<CinemachineCamera>();
        if (virtualCam != null)
        {
            virtualCam.Target.TrackingTarget = transform;
            virtualCam.Target.LookAtTarget = transform;
        }

        PlayerInput playerInput = GetComponent<PlayerInput>();
        if (playerInput != null)
        {
            playerInput.OnRevivePerformed += () => currentZone?.TryRevive();
            playerInput.OnExecutePerformed += () => currentZone?.TryExecute();
        }

    }

    [Rpc(SendTo.Server)]
    private void SpawnPlayerCall_Rpc(ulong clientId)
    {
        Debug.Log($"{clientId} player가 클라이언트에 스폰되었습니다");

        GameManager.Instance.SpawnPlayerCharacter(clientId);
    }

    [ClientRpc]
    public void SetPassengerMode_ClientRpc(bool isPassenger)
    {
        if (!IsOwner) return;
        var playerInput = GetComponent<PlayerInput>();
        if (playerInput != null)
            playerInput.IsPassenger = isPassenger;
    }

    public override void OnDestroy()
    {
        base.OnDestroy();
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void Update()
    {
        if (!IsOwner) return;

        // jumpPressTime이 0보다 크고 0.4초 지났으면 제트팩 발동
        if (jumpPressTime > 0 && Time.time - jumpPressTime > 0.4f)
        {
            isJetpacking = true;
        }

        if (isJetpacking)
        {
            UseJetpackWaterServerRpc();
        }
    }

    private void FixedUpdate()
    {
        if (!IsOwner) return;

        Vector3 camForward = Camera.main.transform.forward;
        Vector3 camRight = Camera.main.transform.right;
        camForward.y = 0; camForward.Normalize();
        camRight.y = 0; camRight.Normalize();
        Vector3 move = camForward * moveInput.y + camRight * moveInput.x;


        //Vector3 move = new Vector3(moveInput.x, 0, moveInput.y);
        rb.MovePosition(rb.position + move * moveSpeed * Time.fixedDeltaTime);

        // 이동 방향으로 회전
        if (move.magnitude > 0.1f)
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
            Quaternion.LookRotation(camForward),
                0.15f
            );

        // 애니메이션
        Vector3 localMove = transform.InverseTransformDirection(move);
        animator.SetFloat("X", localMove.x);  // 좌우
        animator.SetFloat("Y", localMove.z);  // 앞뒤

        if (isJetpacking)
        {
            rb.AddForce(-Physics.gravity * rb.mass, ForceMode.Force);
            rb.AddForce(Vector3.up * jetpackForce, ForceMode.Force);
        }
    }

    [ServerRpc]
    private void UseJetpackWaterServerRpc()
    {
        PlayerWater playerWater = GetComponent<PlayerWater>();
        if (playerWater == null) return;

        if (!playerWater.HasWater())
        {
            // 물 없으면 클라이언트한테 제트팩 끄라고 알려줌
            StopJetpackClientRpc();
            return;
        }

        playerWater.UseWaterForJetpack();
    }

    [ClientRpc]
    private void StopJetpackClientRpc()
    {
        isJetpacking = false;
        jumpPressTime = 0;
    }

    public NetworkVariable<Vector2> netMoveInput = new NetworkVariable<Vector2>();

    // PlayerInput에서 호출
    public void SendMoveInput(Vector2 input)
    {
        moveInput = input;
        Debug.Log(moveInput);
        if (IsOwner)
        {
            SendMoveInputServerRpc(input);
        }
    }

    [ServerRpc]
    private void SendMoveInputServerRpc(Vector2 input)
    {
        netMoveInput.Value = input;
    }

    public Vector2 GetMoveInput()
    {
        return netMoveInput.Value;
    }

    public void SendJumpInput()
    {
        jumpPressTime = Time.time;
        if (IsGrounded())
        {
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        }
    }

    public void SendJetpackInput(bool active)
    {
        if (!active)
        {
            isJetpacking = false;
            jumpPressTime = 0; // 리셋
            return;
        }
    }

    public void SendDashInput()
    {
        //쿨타임 체크
        if (isDashing) return;
        if (Time.time - lastDashTime < dashCooldown) return;


        PlayerWater playerWater = GetComponent<PlayerWater>();

        Debug.Log($"대쉬 시도 - 물 충분: {playerWater?.HasEnoughWater(25f)}, 현재 물: {playerWater?.Water.Value}");
        if (playerWater == null || !playerWater.HasEnoughWater(25f)) return;

        Vector3 dashDir;
        if (moveInput != Vector2.zero)
        {
            Vector3 camForward = Camera.main.transform.forward;
            Vector3 camRight = Camera.main.transform.right;
            camForward.y = 0; camForward.Normalize();
            camRight.y = 0; camRight.Normalize();
            dashDir = camForward * moveInput.y + camRight * moveInput.x;

            //dashDir = new Vector3(moveInput.x, 0, moveInput.y);
        }
        else
        {
            dashDir = transform.forward;
        }
        // 기본 속도 초기화 후 대시
        rb.linearVelocity = new Vector3(0, rb.linearVelocity.y, 0);
        rb.AddForce(dashDir.normalized * dashForce, ForceMode.Impulse);

        // 마지막에 대쉬한 시간 기록
        lastDashTime = Time.time;
        StartCoroutine(Dash_Co());

        UseDashWater_ServerRpc();

    }

    private IEnumerator Dash_Co()
    {
        isDashing = true;
        yield return new WaitForSeconds(dashDuration);
        isDashing = false;
    }


    public bool IsGrounded()
    {
        int layerMask = ~LayerMask.GetMask("Player");

        Vector3 origin = transform.position + Vector3.up * 1.3f;

        bool grounded = Physics.SphereCast(
            origin,
            0.3f,
            Vector3.down,
            out RaycastHit hit,
            1.3f,
            layerMask
        );

        Debug.Log($"IsGrounded: {grounded} / 거리: {hit.distance}");
        return grounded;
    }

    [ServerRpc]
    private void UseDashWater_ServerRpc()
    {
        PlayerWater playerWater = GetComponent<PlayerWater>();
        if (playerWater == null) return;

        bool result = playerWater.UseWaterForShot(25f);
    }

    [ServerRpc]
    public void UseSkill_ServerRpc(string cardId)
    {
        CardData card = GameManager.Instance.SceneContext
                            .GameDataManager.GetCardData(cardId);
        Debug.Log($"UseSkill_ServerRpc 호출됨: {cardId}");

        Faction myFaction = (Faction)GetComponent<PlayerHealth>().PlayerFactionInt.Value;

        switch (card.CardType)
        {
            case CardType.CatGun:
                // 고양이 머신건 스폰
                Debug.Log($"CatGun SkillPrefab: {card.SkillPrefab}");
                GameObject catGunObj = Instantiate(card.SkillPrefab, transform.position, Quaternion.identity);
                catGunObj.GetComponent<NetworkObject>().Spawn();
                catGunObj.GetComponent<CatGunObject>().Initialize(card.Duration, card.Damage, OwnerClientId, myFaction);
                break;
            case CardType.BubbleGun:
                // 버블건
                GameObject bubbleObj = Instantiate(card.SkillPrefab, transform.position + Vector3.up, transform.rotation);
                NetworkObject bubbleNo = bubbleObj.GetComponent<NetworkObject>();
                bubbleNo.Spawn();
                BubbleProjectile bubble = bubbleObj.GetComponent<BubbleProjectile>();
                bubble.Initialize(card.Speed, OwnerClientId, transform.forward);
                break;
            case CardType.PenguinCharge:
                // 펭귄 돌진 처리
                GameObject penguinObj = Instantiate(card.SkillPrefab, transform.position + Vector3.up * 0.5f, transform.rotation);
                NetworkObject penguinNo = penguinObj.GetComponent<NetworkObject>();
                penguinNo.Spawn();
                PenguinChargeObject penguin = penguinObj.GetComponent<PenguinChargeObject>();
                penguin.Initialize(card.Speed, card.Damage, transform.forward, OwnerClientId, myFaction);
                break;
            case CardType.DuckTube:
                GameObject duckTube = Instantiate(card.SkillPrefab, transform.position + Vector3.up * 0.5f, transform.rotation);
                NetworkObject duckNo = duckTube.GetComponent<NetworkObject>();
                duckNo.Spawn();
                ShipDuckNotSsipDuck duck = duckTube.GetComponent<ShipDuckNotSsipDuck>();
                duck.Initialize(card.Duration, card.Speed, this);
                break;
            case CardType.SharkTube:
                GameObject sharkTube = Instantiate(card.SkillPrefab, transform.position + Vector3.up * 0.5f, transform.rotation);
                NetworkObject sharkNo = sharkTube.GetComponent<NetworkObject>();
                sharkNo.Spawn();
                SharkTube shark = sharkTube.GetComponent<SharkTube>();
                shark.Initialize(card.Duration, card.Speed, this);
                break;
            case CardType.GoatDisinfectant:
                GameObject goatDispenser = Instantiate(card.SkillPrefab, transform.position + Vector3.up * 0.5f, transform.rotation);
                NetworkObject goatNo = goatDispenser.GetComponent<NetworkObject>();
                goatNo.Spawn();
                GoatMilkDispenser goat = goatDispenser.GetComponent<GoatMilkDispenser>();
                goat.Initialize(card.Duration, card.Damage, card.Range);
                break;
            case CardType.MalrangBong:
                break;
                // ...
        }
    }

    [ServerRpc]
    public void UseSkillWithDir_ServerRpc(string cardId, Vector3 throwDir)
    {
        Debug.Log($"UseSkillWithDir_ServerRpc 호출됨: {cardId}, 방향: {throwDir}");

        CardData card = GameManager.Instance.SceneContext
                            .GameDataManager.GetCardData(cardId);

        switch (card.CardType)
        {
            case CardType.WaterBalloon:
                Vector3 spawnPos = transform.position + Vector3.up * 1.5f + transform.forward * 1f;
                GameObject balloonObj = Instantiate(card.SkillPrefab, spawnPos, transform.rotation);
                NetworkObject balloonNo = balloonObj.GetComponent<NetworkObject>();
                balloonNo.Spawn();

                Faction myFaction = (Faction)GetComponent<PlayerHealth>().PlayerFactionInt.Value;
                WaterBalloonObject balloon = balloonObj.GetComponent<WaterBalloonObject>();
                balloon.Initialize(card.Range, card.Damage, OwnerClientId, myFaction);
                balloon.Throw(throwDir, card.Speed);
                break;
        }
    }

    [ClientRpc]
    public void ApplyKnockback_ClientRpc(Vector3 force)
    {
        if (!IsOwner) return;

        rb.isKinematic = true;

        Collider col = GetComponent<Collider>();

        if (col != null) col.isTrigger = false;

        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        //rb.AddForce(force, ForceMode.Impulse);

        StartCoroutine(ResetPhysics_Co(force));
    }

    private IEnumerator ResetPhysics_Co(Vector3 force)
    {
        yield return null;

        if (rb != null)
        {
            rb.isKinematic = false;

            if (force.sqrMagnitude > 0.001f)
            {
                rb.AddForce(force, ForceMode.Impulse);
            }
        }
    }

    [ClientRpc]
    public void ApplyBubbleEffect_ClientRpc(float duration)
    {
        if (!IsOwner) return;
        // BubbleEffectUI 띄우기
        BubbleEffectUI.Instance.Show(duration);
    }

    [ServerRpc]
    public void ReviveAlly_ServerRpc(ulong targetClientId)
    {
        if (!NetworkManager.Singleton.ConnectedClients
            .TryGetValue(targetClientId, out Unity.Netcode.NetworkClient client)) return;

        PlayerHealth targetHealth = client.PlayerObject.GetComponent<PlayerHealth>();
        if (targetHealth == null) return;
        if (targetHealth.State.Value != PlayerState.Down) return;

        targetHealth.Revive();
    }

    [ServerRpc]
    public void ExecuteEnemy_ServerRpc(ulong targetClientId)
    {
        if (!NetworkManager.Singleton.ConnectedClients
            .TryGetValue(targetClientId, out Unity.Netcode.NetworkClient client)) return;

        PlayerHealth targetHealth = client.PlayerObject.GetComponent<PlayerHealth>();
        if (targetHealth == null) return;
        if (targetHealth.State.Value != PlayerState.Down) return;

        targetHealth.Kill();
    }

    public void SetCurrentZone(ZoneInteraction zone)
    {
        currentZone = zone;
    }

    public void ClearCurrentZone(ZoneInteraction zone)
    {
        if (currentZone == zone)
            currentZone = null;
    }

}
