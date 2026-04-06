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

    public event Action<string[], ulong> OnCardSelectionRequested;

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

    public CardData GetCardData(string id)
    {
        if (cardDataDictionary.ContainsKey(id))
            return cardDataDictionary[id];
        return default;
    }

    public List<CardData> GetRandomCards(int count)
    {
        List<CardData> shuffled = new List<CardData>(cardDatas);

        for (int i = shuffled.Count - 1; i > 0; i--)
        {
            int j = UnityEngine.Random.Range(0, i + 1);
            CardData temp = shuffled[i];
            shuffled[i] = shuffled[j];
            shuffled[j] = temp;
        }

        return shuffled.GetRange(0, Mathf.Min(count, shuffled.Count));
    }

    public void StartCardSelection()
    {
        if (!IsServer) return;

        foreach (var client in NetworkManager.Singleton.ConnectedClients)
        {
            ulong clientId = client.Key;
            List<CardData> cards = GetRandomCards(3);

            string cardIds = string.Join(",", cards.ConvertAll(c => c.ID));
            ShowCardSelection_ClientRpc(cardIds, clientId);
        }
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void ShowCardSelection_ClientRpc(string cardIds, ulong targetClientId)
    {
        if (NetworkManager.Singleton.LocalClientId != targetClientId) return;

        string[] cardIdArray = cardIds.Split(',');
        OnCardSelectionRequested?.Invoke(cardIdArray, targetClientId);
    }

    [Rpc(SendTo.Server)]
    public void SelectCard_ServerRpc(string cardId, ulong clientId)
    {
        CardData card = GetCardData(cardId);
        Debug.Log($"[{clientId}] 카드 선택: {card.CardName}");
        ApplySkill_ClientRpc(cardId, clientId);
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void ApplySkill_ClientRpc(string cardId, ulong clientId)
    {
        if (NetworkManager.Singleton.LocalClientId != clientId) return;

        CardData card = GetCardData(cardId);
        PlayerSkill playerSkill = NetworkManager.Singleton.LocalClient
                                    .PlayerObject.GetComponent<PlayerSkill>();
        playerSkill.SetSkill(card);
    }
}
