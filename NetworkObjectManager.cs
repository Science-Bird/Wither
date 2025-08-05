using System;
using HarmonyLib;
using Unity.Netcode;
using UnityEngine;

namespace Wither;

[HarmonyPatch]
public class NetworkObjectManager
{

    [HarmonyPostfix, HarmonyPatch(typeof(GameNetworkManager), nameof(GameNetworkManager.Start))]
    public static void Init()
    {
        if (networkPrefab != null)
            return;

        networkPrefab = (GameObject)Wither.ExtraAssets.LoadAsset("WitherNetworkHandler");
        networkPrefab.AddComponent<NetworkHandler>();

        NetworkManager.Singleton.AddNetworkPrefab(networkPrefab);
    }

    [HarmonyPostfix, HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.Awake))]
    static void SpawnNetworkHandler()
    {
        if (NetworkManager.Singleton.IsHost || NetworkManager.Singleton.IsServer)
        {
            var networkHandlerHost = UnityEngine.Object.Instantiate(networkPrefab, Vector3.zero, Quaternion.identity);
            networkHandlerHost.GetComponent<NetworkObject>().Spawn();
        }
    }

    static GameObject networkPrefab;
}