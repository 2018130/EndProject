using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class WeaponController : NetworkBehaviour
{
    [SerializeField] private AimRigController aimRigController;
    private List<BaseWeapon> _weapons = new List<BaseWeapon>();

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

    private NetworkObject _equippedMalrangBongNetObj;

    private PlayerInput _playerInput;
    private AimController _aimController;

    public BaseWeapon CurrentWeapon => (_weapons.Count > 0 && _currentWeaponIndex.Value < _weapons.Count && !_isMalrangbongActive.Value)
        ? _weapons[_currentWeaponIndex.Value] : null;
    //public BaseWeapon CurrentWeapon => (_weapons.Count > 0 && _currentWeaponIndex.Value < _weapons.Count) ? _weapons[_currentWeaponIndex.Value] : null;

    public bool IsMalrangBongActive => _isMalrangbongActive.Value;

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
        {
            CurrentWeapon.Attack();
        }

        if (_playerInput.isFiring && _isMalrangbongActive.Value)
        {
            if (_spawnedmalrangBongRef.Value.TryGet(out NetworkObject no))
            {
                if (no.TryGetComponent(out MalangBong mb))
                {
                    mb.RequestAttack();
                }
            }
        }
    }

    public void RegisterWeapon(BaseWeapon weapon)
    {
        _weapons.Add(weapon);

        // ★ GunAlignToHand가 있으면 handBone 주입
        // handBone은 AimRigController가 OnNetworkSpawn에서 이미 찾아둠
        if (aimRigController != null)
        {
            var align = weapon.GetComponent<GunAlignToHand>();
            if (align != null)
                align.SetHandBone(aimRigController.HandBone);
        }

        PlayerNetwork playerNetwork = GetComponent<PlayerNetwork>();
        if(playerNetwork != null && playerNetwork.WeaponPivot != null)
        {
            weapon.SetFollowTarget(playerNetwork.WeaponPivot);
        }

        //if (_weapons.Count == 1)
        //    weapon.gameObject.SetActive(true);
        //else
        //    weapon.gameObject.SetActive(false);

        if (_weapons.Count == _expectedWeaponCount)
        {
            if (IsOwner && _weapons[0] is RangedWeapon rangedWeapon)
                rangedWeapon.InitializeAfterEquip();
        }

        UpdateWeaponVisibility(_currentWeaponIndex.Value, _isMalrangbongActive.Value);
    }

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
            // 이미 Despawn된 오브젝트에 Despawn() 재호출 시 예외 방지
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
        //Debug.Log($"OnWeaponChanged - prev:{prev}, current:{current}, IsOwner:{IsOwner}, 무기수:{_weapons.Count}");
        //if (prev < _weapons.Count)
        //{
        //    if (IsOwner && _weapons[prev] is RangedWeapon prevRanged)
        //        prevRanged.UnsubscribeInput();
        //    _weapons[prev].gameObject.SetActive(false);
        //}

        //if (current < _weapons.Count)
        //{
        //    _weapons[current].gameObject.SetActive(true);
        //    if (IsOwner && _weapons[current] is RangedWeapon rangedWeapon)
        //        rangedWeapon.InitializeAfterEquip();
        //}

        UpdateWeaponVisibility(current, _isMalrangbongActive.Value);
    }

    private void OnMalrangBongChanged(bool prev, bool current)
    {
        UpdateWeaponVisibility(_currentWeaponIndex.Value, current);

        // 말랑봉 해제(스왑)될 때 오너 클라이언트에서 쿨타임 시작
        if (prev && !current && IsOwner)
        {
            GetComponent<PlayerSkill>()?.StartMalrangBongCooldown();
        }
    }

    private void UpdateWeaponVisibility(int slotIndex, bool isMalrangActive)
    {
        //if (_weapons.Count == 0) return;

        for (int i = 0; i < _weapons.Count; i++)
        {
            if (_weapons[i] == null) continue;
            //if (IsOwner && _weapons[i] is RangedWeapon rw) rw.UnsubscribeInput();
            if (_weapons[i] is RangedWeapon rw) rw.UnsubscribeInput();
            _weapons[i].gameObject.SetActive(false);
        }

        if (!isMalrangActive && slotIndex < _weapons.Count)
        {
            _weapons[slotIndex].gameObject.SetActive(true);
            //if (IsOwner && _weapons[slotIndex] is RangedWeapon rw2)
            if (_weapons[slotIndex] is RangedWeapon rw2)
            {
                rw2.InitializeAfterEquip();
            }
        }
    }
}
