using System;
using System.Collections;
using Unity.Netcode;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GameDataManager : NetworkBehaviour
{
    [SerializeField]
    private List<WeaponData> weaponDatas = new List<WeaponData>();

    private Dictionary<string, WeaponData> weaponDataDictionary = new Dictionary<string, WeaponData>();

    private void Awake()
    {
        foreach(WeaponData weaponData in weaponDatas)
        {
            weaponDataDictionary.Add(weaponData.ID, weaponData);
        }
    }

    public WeaponData GetWeaponData(string id)
    {
        if(weaponDataDictionary.ContainsKey(id))
        {
            return weaponDataDictionary[id];
        }
        else
        {
            return default;
        }
    }

    [Rpc(SendTo.Server)]
    public void SpawnWeapon_ServerRpc(string weaponId, ulong clientId)
    {
        if(weaponDataDictionary.TryGetValue(weaponId, out WeaponData weaponData))
        {
            BaseWeapon baseWeapon = Instantiate(weaponData.WeaponPrefab).GetComponent<BaseWeapon>();
            NetworkObject weaponNO = baseWeapon.GetComponent<NetworkObject>();
            weaponNO.transform.position = new Vector3(clientId, 0, 0);
            weaponNO.SpawnWithOwnership(clientId);
        }
        else
        {
            Debug.Log($"{weaponId}인 Weapon Data가 존재하지 않습니다.");
        }
    }
}
