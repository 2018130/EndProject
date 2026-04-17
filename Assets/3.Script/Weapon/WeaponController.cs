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

    private PlayerInput _playerInput;
    private AimController _aimController;

    public BaseWeapon CurrentWeapon => (_weapons.Count > 0 && _currentWeaponIndex.Value < _weapons.Count) ? _weapons[_currentWeaponIndex.Value] : null;

    private int _expectedWeaponCount = 3;

    public override void OnNetworkSpawn()
    {
        _currentWeaponIndex.OnValueChanged += OnWeaponChanged;

        if (!IsOwner) return;

        _playerInput = GetComponent<PlayerInput>();
        _playerInput.OnWeaponSwap += HandleWeaponSwap;
    }

    public override void OnNetworkDespawn()
    {
        _currentWeaponIndex.OnValueChanged -= OnWeaponChanged;

        if (_playerInput != null)
            _playerInput.OnWeaponSwap -= HandleWeaponSwap;
    }

    private void Update()
    {
        if (!IsOwner || _playerInput == null) return;

        if(_playerInput.isFirePressed && CurrentWeapon != null)
        {
            CurrentWeapon.Attack();
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

    private void HandleWeaponSwap(int index)
    {
        if (index == _currentWeaponIndex.Value) return;
        if (index < 0 || index >= _weapons.Count) return;
        RequestSwapServerRpc(index);
    }

    [ServerRpc]
    private void RequestSwapServerRpc(int index)
    {
        _currentWeaponIndex.Value = index;
    }

    private void OnWeaponChanged(int prev, int current)
    {
        Debug.Log($"OnWeaponChanged - prev:{prev}, current:{current}, IsOwner:{IsOwner}, 무기수:{_weapons.Count}");
        if (prev < _weapons.Count)
        {
            if (IsOwner && _weapons[prev] is RangedWeapon prevRanged)
                prevRanged.UnsubscribeInput();
            _weapons[prev].gameObject.SetActive(false);
        }

        if (current < _weapons.Count)
        {
            _weapons[current].gameObject.SetActive(true);
            if (IsOwner && _weapons[current] is RangedWeapon rangedWeapon)
                rangedWeapon.InitializeAfterEquip();
        }
    }

}
