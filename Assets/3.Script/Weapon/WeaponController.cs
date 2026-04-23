using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class WeaponController : NetworkBehaviour
{
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

    private PlayerInput _playerInput;
    private AimController _aimController;


    public BaseWeapon CurrentWeapon => (_weapons.Count > 0 && _currentWeaponIndex.Value < _weapons.Count && !_isMalrangbongActive.Value)
        ? _weapons[_currentWeaponIndex.Value] : null;

    //public BaseWeapon CurrentWeapon => (_weapons.Count > 0 && _currentWeaponIndex.Value < _weapons.Count) ? _weapons[_currentWeaponIndex.Value] : null;

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

        Debug.Log($"RegisterWeapon 호출됨 - 무기: {weapon.gameObject.name}, 총 무기 수: {_weapons.Count}");

        if (_weapons.Count == 1)
            weapon.gameObject.SetActive(true);
        else
            weapon.gameObject.SetActive(false);

        // 모든 무기가 등록됐을 때 한 번만 초기화
        if (_weapons.Count == _expectedWeaponCount)
        {
            if (IsOwner && _weapons[0] is RangedWeapon rangedWeapon)
                rangedWeapon.InitializeAfterEquip();
        }

    }

    [ServerRpc]
    public void EquipMalrangBong_ServerRpc(NetworkObjectReference mbRef)
    {
        _spawnedmalrangBongRef.Value = mbRef;
        _isMalrangbongActive.Value = true;
    }

    public void DespawnMalrangBongOnServer()
    {
        if (!IsServer) return;

        if(_isMalrangbongActive.Value)
        {
            _isMalrangbongActive.Value = false;
            if(_spawnedmalrangBongRef.Value.TryGet(out NetworkObject no))
            {
                no.Despawn();
            }
        }
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
    }

    private void UpdateWeaponVisibility(int slotIndex, bool isMalrangActive)
    {
        for (int i = 0; i < _weapons.Count; i++)
        {
            if (IsOwner && _weapons[i] is RangedWeapon rw) rw.UnsubscribeInput();
            _weapons[i].gameObject.SetActive(false);
        }

        if (!isMalrangActive && slotIndex < _weapons.Count)
        {
            _weapons[slotIndex].gameObject.SetActive(true);
            if (IsOwner && _weapons[slotIndex] is RangedWeapon rw)
            {
                rw.InitializeAfterEquip();
            }
        }
    }
}
