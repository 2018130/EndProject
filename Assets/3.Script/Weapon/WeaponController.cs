using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class WeaponController : NetworkBehaviour
{
    [SerializeField] private AimRigController aimRigController; // ★ HEAD 브랜치

    private List<BaseWeapon> _weapons = new List<BaseWeapon>();

    public event System.Action<BaseWeapon> OnWeaponRegistered; // ★ HEAD 브랜치

    private NetworkVariable<int> _currentWeaponIndex = new NetworkVariable<int>(
        0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    private NetworkVariable<bool> _isMalrangbongActive = new NetworkVariable<bool>(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    private NetworkVariable<NetworkObjectReference> _spawnedmalrangBongRef = new NetworkVariable<NetworkObjectReference>();

    private NetworkObject _equippedMalrangBongNetObj; // ★ main 브랜치

    private PlayerInput _playerInput;
    private AimController _aimController;

    public BaseWeapon CurrentWeapon => (_weapons.Count > 0 && _currentWeaponIndex.Value < _weapons.Count && !_isMalrangbongActive.Value)
        ? _weapons[_currentWeaponIndex.Value] : null;

    public bool IsMalrangBongActive => _isMalrangbongActive.Value; // ★ main 브랜치

    private int _expectedWeaponCount = 3;

    public override void OnNetworkSpawn()
    {
        _currentWeaponIndex.OnValueChanged += OnWeaponChanged;
        _isMalrangbongActive.OnValueChanged += OnMalrangBongChanged;

        if (!IsOwner) return;

        _playerInput = GetComponent<PlayerInput>();
        _playerInput.OnWeaponSwap += HandleWeaponSwap;
    }

    public override void OnNetworkDespawn()
    {
        _currentWeaponIndex.OnValueChanged -= OnWeaponChanged;
        _isMalrangbongActive.OnValueChanged -= OnMalrangBongChanged;

        if (_playerInput != null)
            _playerInput.OnWeaponSwap -= HandleWeaponSwap;
    }

    private void Update()
    {
        if (!IsOwner || _playerInput == null) return;

        if (_playerInput.isFiring && CurrentWeapon != null)
            CurrentWeapon.Attack();

        if (_playerInput.isFiring && _isMalrangbongActive.Value)
        {
            if (_spawnedmalrangBongRef.Value.TryGet(out NetworkObject no))
            {
                if (no.TryGetComponent(out MalangBong mb))
                    mb.RequestAttack();
            }
        }
    }

    public void RegisterWeapon(BaseWeapon weapon)
    {
        _weapons.Add(weapon);

        // ★ HEAD: GunAlignToHand에 HandBone 주입
        if (aimRigController != null)
        {
            var align = weapon.GetComponent<GunAlignToHand>();
            if (align != null)
                align.SetHandBone(aimRigController.HandBone);
        }

        // ★ main: WeaponPivot 추적 설정 (총이 몸에 박히지 않는 핵심)
        PlayerNetwork playerNetwork = GetComponent<PlayerNetwork>();
        if (playerNetwork != null && playerNetwork.WeaponPivot != null)
            weapon.SetFollowTarget(playerNetwork.WeaponPivot);

        if (_weapons.Count == _expectedWeaponCount)
        {
            if (IsOwner && _weapons[0] is RangedWeapon rangedWeapon)
                rangedWeapon.InitializeAfterEquip();
        }

        // ★ main: 등록 시 가시성 즉시 갱신
        UpdateWeaponVisibility(_currentWeaponIndex.Value, _isMalrangbongActive.Value);

        // ★ HEAD: AimRigController에 GripTargetAim 주입
        OnWeaponRegistered?.Invoke(weapon);
    }

    // ★ main 브랜치 추가 메서드
    public void SetMalrangBongEquipped(NetworkObject mbNetObj)
    {
        if (!IsServer) return;

        _equippedMalrangBongNetObj = mbNetObj;
        _spawnedmalrangBongRef.Value = mbNetObj;
        _isMalrangbongActive.Value = true;

        ForceWeaponVisibility_Rpc(true);
    }

    public void DespawnMalrangBongOnServer()
    {
        if (!IsServer) return;

        if (_equippedMalrangBongNetObj != null)
        {
            if (_equippedMalrangBongNetObj.IsSpawned)
                _equippedMalrangBongNetObj.Despawn();

            _equippedMalrangBongNetObj = null;
        }

        _isMalrangbongActive.Value = false;
    }

    [Rpc(SendTo.Everyone)]
    private void ForceWeaponVisibility_Rpc(bool isMalrangActive)
    {
        UpdateWeaponVisibility(_currentWeaponIndex.Value, isMalrangActive);
    }

    [ServerRpc]
    public void EquipMalrangBong_ServerRpc(NetworkObjectReference mbRef)
    {
        if (mbRef.TryGet(out NetworkObject no)) SetMalrangBongEquipped(no);
    }

    private void HandleWeaponSwap(int index)
    {
        if (index == _currentWeaponIndex.Value && !_isMalrangbongActive.Value) return;
        if (index < 0 || index >= _weapons.Count) return;
        RequestSwapServerRpc(index);
    }

    [ServerRpc]
    private void RequestSwapServerRpc(int index)
    {
        DespawnMalrangBongOnServer();
        _currentWeaponIndex.Value = index;
    }

    private void OnWeaponChanged(int prev, int current)
    {
        UpdateWeaponVisibility(current, _isMalrangbongActive.Value);
    }

    private void OnMalrangBongChanged(bool prev, bool current)
    {
        UpdateWeaponVisibility(_currentWeaponIndex.Value, current);

        // ★ main: 말랑봉 해제 시 쿨타임 시작
        if (prev && !current && IsOwner)
            GetComponent<PlayerSkill>()?.StartMalrangBongCooldown();
    }

    private void UpdateWeaponVisibility(int slotIndex, bool isMalrangActive)
    {
        for (int i = 0; i < _weapons.Count; i++)
        {
            if (_weapons[i] == null) continue;
            if (_weapons[i] is RangedWeapon rw) rw.UnsubscribeInput();
            _weapons[i].gameObject.SetActive(false);
        }

        if (!isMalrangActive && slotIndex < _weapons.Count)
        {
            _weapons[slotIndex].gameObject.SetActive(true);
            if (_weapons[slotIndex] is RangedWeapon rw2)
                rw2.InitializeAfterEquip();
        }
    }
}