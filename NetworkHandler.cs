using System;
using Unity.Netcode;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Wither;

public class NetworkHandler : NetworkBehaviour
{
    public override void OnNetworkSpawn()
    {
        LevelEvent = null;

        if (NetworkManager.Singleton.IsHost || NetworkManager.Singleton.IsServer)
        {
            if (Instance != null)
            {
                Instance.gameObject?.GetComponent<NetworkObject>()?.Despawn();
            }
            else
            {
                Wither.Logger.LogWarning("Null instance! Network object despawn failed.");
            }
        }
            
        Instance = this;

        base.OnNetworkSpawn();
    }

    [ClientRpc]
    public void EventClientRpc(string eventName)
    {
        LevelEvent?.Invoke(eventName); // If the event has subscribers (does not equal null), invoke the event
    }

    public static event Action<String> LevelEvent;

    public static NetworkHandler Instance { get; private set; }
}