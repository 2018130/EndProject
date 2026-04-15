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

    public void RegisterWeapon(BaseWeapon weapon)
    {
        _weapons.Add(weapon);

        Debug.Log($"RegisterWeapon 호출됨 - 무기: {weapon.gameObject.name}, 총 무기 수: {_weapons.Count}");

        // 첫 번째 무기만 활성화
        if (_weapons.Count == 1)
        {
            weapon.gameObject.SetActive(true);

            if (IsOwner && weapon is RangedWeapon rangedWeapon)
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
        if (prev < _weapons.Count)
        {
            _weapons[prev].gameObject.SetActive(false);
            if (IsOwner && _weapons[prev] is RangedWeapon prevRanged)
                prevRanged.UnsubscribeInput();
        }

        if (current < _weapons.Count)
        {
            _weapons[current].gameObject.SetActive(true);

            if (IsOwner && _weapons[current] is RangedWeapon rangedWeapon)
                rangedWeapon.InitializeAfterEquip();
        }
    }
}