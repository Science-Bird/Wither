using HarmonyLib;
using UnityEngine;
using Unity.Netcode;
using Wither.Scripts;

namespace Wither.Patches;

[HarmonyPatch]
public class SaveDataSyncPatches
{
    public static GameObject saveSyncPrefab;
    public static DataSync saveSyncScript;

    [HarmonyPatch(typeof(GameNetworkManager), nameof(GameNetworkManager.Start))]
    [HarmonyPostfix]
    public static void InitializeAssets()
    {
        saveSyncPrefab = (GameObject)Wither.ExtraAssets.LoadAsset("SaveDataSync");
        NetworkManager.Singleton.AddNetworkPrefab(saveSyncPrefab);
    }

    [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.Start))]
    [HarmonyPostfix]
    static void SpawnSyncScript(StartOfRound __instance)
    {
        Wither.Logger.LogDebug($"Wither entry unlocked: {TerminalEntryPatches.unlocked}, Wither entry unread: {TerminalEntryPatches.unread}");
        if (saveSyncScript == null && __instance.IsServer)
        {
            GameObject saveSyncScriptObj = UnityEngine.Object.Instantiate(saveSyncPrefab, Vector3.zero, Quaternion.identity);
            saveSyncScriptObj.GetComponent<NetworkObject>().Spawn();
            saveSyncScript = saveSyncScriptObj.GetComponent<DataSync>();
        }
    }

    [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.OnClientConnect))]
    [HarmonyPostfix]
    static void OnConnectionServer(StartOfRound __instance)// server sends save data to clients using sync script (LethalModDataLib will save data to the file, but only the host has access to it)
    {
        if (!__instance.IsServer) { return; }
        Wither.Logger.LogDebug($"Wither entry unlocked: {TerminalEntryPatches.unlocked}, Wither entry unread: {TerminalEntryPatches.unread}");
        if (saveSyncScript == null)
        {
            if (DataSync.Instance != null)
            {
                saveSyncScript = DataSync.Instance;
            }
            else
            {
                saveSyncScript = Object.FindObjectOfType<DataSync>();
            }
        }
        if (saveSyncScript != null)
        {
            saveSyncScript.UpdateTerminalPatchClientRpc(TerminalEntryPatches.unlocked, TerminalEntryPatches.unread);
        }
        else
        {
            Wither.Logger.LogWarning("Host unable to find save sync script and sync saved values to clients.");
        }
    }
}