using GameNetcodeStuff;
using HarmonyLib;
using UnityEngine;

namespace Wither.Patches;

[HarmonyPatch]
public class FootstepsPatch
{
    public static bool swappedSurface = false;
    public static AudioClip[] sandClips;
    public static AudioClip sandHitClip;
    public static AudioClip[] gravelClips;
    public static AudioClip gravelHitClip;

    public static bool doStepChange;
    public static bool goingOutside;
    public static bool spectated;

    public static void LoadAssets()
    {
        AudioClip clip1 = (AudioClip)Wither.ExtraAssets.LoadAsset("sand1");
        AudioClip clip2 = (AudioClip)Wither.ExtraAssets.LoadAsset("sand2");
        AudioClip clip3 = (AudioClip)Wither.ExtraAssets.LoadAsset("sand3");
        AudioClip clip4 = (AudioClip)Wither.ExtraAssets.LoadAsset("sand4");
        sandHitClip = (AudioClip)Wither.ExtraAssets.LoadAsset("HitSand");
        sandClips = [clip1, clip2, clip3, clip4];
    }

    static void ChangeSandySteps(bool sand)
    {
        if (sand && ScenePatches.onWither && !swappedSurface)// replace gravel surface sounds with custom sand sounds
        {
            for (int i = 0; i < StartOfRound.Instance.footstepSurfaces.Length; i++)
            {
                if (StartOfRound.Instance.footstepSurfaces[i].surfaceTag == "Gravel")
                {
                    if (gravelHitClip == null)
                    {
                        gravelClips = StartOfRound.Instance.footstepSurfaces[i].clips;
                        gravelHitClip = StartOfRound.Instance.footstepSurfaces[i].hitSurfaceSFX;
                    }
                    StartOfRound.Instance.footstepSurfaces[i].clips = sandClips;
                    StartOfRound.Instance.footstepSurfaces[i].hitSurfaceSFX = sandHitClip;
                    swappedSurface = true;
                }
            }
        }
        else if (!sand && swappedSurface)// reset gravel sounds back to normal
        {
            for (int i = 0; i < StartOfRound.Instance.footstepSurfaces.Length; i++)
            {
                if (StartOfRound.Instance.footstepSurfaces[i].surfaceTag == "Gravel")
                {
                    StartOfRound.Instance.footstepSurfaces[i].clips = gravelClips;
                    StartOfRound.Instance.footstepSurfaces[i].hitSurfaceSFX = gravelHitClip;
                    swappedSurface = false;
                }
            }
        }
    }

    [HarmonyPatch(typeof(RoundManager), nameof(RoundManager.SyncScrapValuesClientRpc))]
    [HarmonyPostfix]
    static void OnSync(RoundManager __instance)
    {
        ChangeSandySteps(true);
    }

    [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.ShipLeave))]
    [HarmonyPrefix]
    static void OnLeave(StartOfRound __instance)
    {
        ChangeSandySteps(false);
    }

    [HarmonyPatch(typeof(EntranceTeleport), nameof(EntranceTeleport.TeleportPlayer))]
    [HarmonyPrefix]
    static void InteriorTP(EntranceTeleport __instance)
    {
        PlayerControllerB localPlayer = GameNetworkManager.Instance.localPlayerController;
        if (ScenePatches.onWither && !localPlayer.isPlayerDead)
        {
            doStepChange = true;
            goingOutside = !__instance.isEntranceToBuilding;
            spectated = false;
        }
    }

    [HarmonyPatch(typeof(EntranceTeleport), nameof(EntranceTeleport.TeleportPlayerClientRpc))]
    [HarmonyPrefix]
    static void InteriorTPClients(EntranceTeleport __instance, int playerObj)
    {
        PlayerControllerB player = StartOfRound.Instance.allPlayerScripts[playerObj];
        if (player != GameNetworkManager.Instance.localPlayerController && PlayerSelfOrSpectated(player) && ScenePatches.onWither && !player.isPlayerDead)// only spectators
        {
            doStepChange = true;
            goingOutside = !__instance.isEntranceToBuilding;
            spectated = true;
        }
    }

    [HarmonyPatch(typeof(ShipTeleporter), nameof(ShipTeleporter.TeleportPlayerOutWithInverseTeleporter))]
    [HarmonyPrefix]
    static void InverseTP(ShipTeleporter __instance, int playerObj)
    {
        PlayerControllerB player = StartOfRound.Instance.allPlayerScripts[playerObj];
        if (PlayerSelfOrSpectated(player) && ScenePatches.onWither && !player.isPlayerDead)
        {
            doStepChange = true;
            goingOutside = false;
            spectated = player != GameNetworkManager.Instance.localPlayerController;
        }
    }

    static bool PlayerSelfOrSpectated(PlayerControllerB targetPlayer)
    {
        PlayerControllerB localPlayer = GameNetworkManager.Instance.localPlayerController;
        return targetPlayer == localPlayer || (localPlayer != null && localPlayer.isPlayerDead && localPlayer.spectatedPlayerScript != null && localPlayer.spectatedPlayerScript == targetPlayer);
    }

    [HarmonyPatch(typeof(PlayerControllerB), nameof(PlayerControllerB.TeleportPlayer))]
    [HarmonyPostfix]
    static void OnTP(PlayerControllerB __instance)
    {
        if (ScenePatches.onWither && !__instance.isPlayerDead && (spectated || PlayerSelfOrSpectated(__instance)))
        {
            if (doStepChange)// ship inverse and entrance TP
            {
                ChangeSandySteps(goingOutside);
            }
            else// regular ship TP
            {
                ChangeSandySteps(true);
            }
            doStepChange = false;
            spectated = false;
        }
    }

    [HarmonyPatch(typeof(PlayerControllerB), nameof(PlayerControllerB.SpectateNextPlayer))]
    [HarmonyPostfix]
    static void OnSpecSwitch(PlayerControllerB __instance)
    {
        if (__instance.IsOwner && ScenePatches.onWither && __instance.isPlayerDead && __instance.spectatedPlayerScript != null)
            ChangeSandySteps(!__instance.spectatedPlayerScript.isInsideFactory);
    }
}