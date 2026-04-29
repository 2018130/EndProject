using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OutBoundChecker : MonoBehaviour
{
    class PlayerTimer
    {
        public PlayerHealth PlayerHealth;
        public DateTime BoundedTime;
    }

    [SerializeField]
    private float damage = 10f;
    [SerializeField]
    private float damageTick = 0.5f;

    private static List<PlayerTimer> boundedPlayers = new List<PlayerTimer>();

    private void OnTriggerEnter(Collider other)
    {
        if(other.TryGetComponent(out PlayerHealth player))
        {
            AddPlayer(player);
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if(other.TryGetComponent(out PlayerHealth player))
        {
            PlayerTimer playerTimer = FindPlayer(player);

            if(playerTimer != null &&
                (DateTime.Now - playerTimer.BoundedTime).TotalSeconds >= damageTick)
            {
                playerTimer.PlayerHealth.TakeDamage(damage);
                playerTimer.BoundedTime = DateTime.Now;
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if(other.TryGetComponent(out PlayerHealth player))
        {
            RemovePlayer(player);
        }
    }

    private void AddPlayer(PlayerHealth player)
    {
        for(int i = 0; i < boundedPlayers.Count; i++)
        {
            if(boundedPlayers[i].PlayerHealth == player)
            {
                return;
            }
        }

        boundedPlayers.Add(new PlayerTimer() { PlayerHealth = player, BoundedTime = DateTime.Now }) ;
    }

    private void RemovePlayer(PlayerHealth player)
    {
        for(int i = 0; i < boundedPlayers.Count; i++)
        {
            if(boundedPlayers[i].PlayerHealth == player)
            {
                boundedPlayers.Remove(boundedPlayers[i]);
                return;
            }
        }
    }

    private PlayerTimer FindPlayer(PlayerHealth player)
    {
        for(int i = 0; i < boundedPlayers.Count; i++)
        {
            if(boundedPlayers[i].PlayerHealth == player)
            {
                return boundedPlayers[i];
            }
        }

        return null;
    }
}
