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

        // 첫 번째 무기만 활성화
        if (_weapons.Count == 1)
            weapon.gameObject.SetActive(true);
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
            _weapons[prev].gameObject.SetActive(false);

        if (current < _weapons.Count)
            _weapons[current].gameObject.SetActive(true);
    }
}