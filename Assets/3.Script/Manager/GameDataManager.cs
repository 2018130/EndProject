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

    [Header("Card Datas")]
    public List<CardData> cardDatas = new List<CardData>();

    private Dictionary<string, CardData> cardDataDictionary = new Dictionary<string, CardData>();

    private void Awake()
    {
        foreach(WeaponData weaponData in weaponDatas)
        {
            weaponDataDictionary.Add(weaponData.ID, weaponData);
        }
        foreach(CardData cardData in cardDatas)
        {
            cardDataDictionary.Add(cardData.ID, cardData);
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
            weaponNO.SpawnWithOwnership(clientId);

            if (NetworkManager.Singleton.ConnectedClients
                .TryGetValue(clientId, out NetworkClient client))
            {

                weaponNO.TrySetParent(client.PlayerObject, false);

                EquipWeapon_ClientRpc(weaponNO.NetworkObjectId, clientId);
            }
        }
        else
        {
            Debug.Log($"{weaponId}인 Weapon Data가 존재하지 않습니다.");
        }


    }

    [Rpc(SendTo.Everyone)]
    private void EquipWeapon_ClientRpc(ulong weaponNetworkId, ulong clientId)
    {
        if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects
            .TryGetValue(weaponNetworkId, out NetworkObject weaponNO)) return;

        if (!NetworkManager.Singleton.ConnectedClients
            .TryGetValue(clientId, out NetworkClient client)) return;

        BaseWeapon weapon = weaponNO.GetComponent<BaseWeapon>();
        weapon.transform.localPosition = new Vector3(0.7f, 0.7f, 0f);
        weapon.transform.localRotation = Quaternion.identity;

        if (weapon is RangedWeapon rangedWeapon)
            rangedWeapon.InitializeAfterEquip();
    }
}
