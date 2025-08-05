using System.Reflection;
using GameNetcodeStuff;
using HarmonyLib;
using UnityEngine;
using Wither.Inside;

namespace Wither.Patches;

[HarmonyPatch]
public class FalseInteriorPatches
{
    public static ManualCameraRenderer twoRadarCam;

    // this ended up being a bunch of patches related to the radar, since I need to do a lot of juggling to have the false interior radar display as expected

    static bool TargetPlayerOutsideTrueInterior(ManualCameraRenderer radarCam)// we still want some radar cam patches to apply in false interior like they do outside, so this only returns false in the actual interior
    {
        int radarIndex = radarCam.targetTransformIndex;// index of player in radar cam's array (used by the radar cams)
        int playerIndex = radarCam.targetedPlayer != null ? (int)radarCam.targetedPlayer.playerClientId : -1;// index of player in StartOfRound allPlayerScripts array (used to track players in false interior)

        // player is outside OR player is inside false interior OR player is a radar booster which is outside
        return GameNetworkManager.Instance.localPlayerController != null && ((playerIndex != -1 && (!radarCam.targetedPlayer.isInsideFactory || TargetPlayerInFalseInterior(radarCam))) || (radarCam.radarTargets[radarIndex].isNonPlayer && (bool)radarCam.radarTargets[radarIndex].transform.GetComponent<RadarBoosterItem>() && !radarCam.radarTargets[radarIndex].transform.GetComponent<RadarBoosterItem>().isInFactory));
    }

    static bool TargetPlayerInFalseInterior(ManualCameraRenderer radarCam)
    {
        int radarIndex = radarCam.targetTransformIndex;
        int playerIndex = radarCam.targetedPlayer != null ? (int)radarCam.targetedPlayer.playerClientId : -1;

        // not a radar booster AND either player is themselves and in false interior or player is another player and in false interior
        return playerIndex != -1 && InFactoryTrigger.playersInFalseInterior != null && !radarCam.radarTargets[radarIndex].isNonPlayer && ((InFactoryTrigger.isInFalseInterior && GameNetworkManager.Instance.localPlayerController == radarCam.targetedPlayer) || InFactoryTrigger.playersInFalseInterior.Contains(playerIndex));
    }

    static bool ValidCam(ManualCameraRenderer radarCam)
    {
        return ScenePatches.onWither && radarCam.cam == radarCam.mapCamera && !radarCam.overrideCameraForOtherUse;
    }

    [HarmonyPatch(typeof(ManualCameraRenderer), nameof(ManualCameraRenderer.SetLineToExitFromRadarTarget))]
    [HarmonyPrefix]
    public static bool ExitLinePatch(ManualCameraRenderer __instance)// disable line to exit when in false interior (applies to both radar cams)
    {
        if (ValidCam(__instance) && TargetPlayerInFalseInterior(__instance))
        {
            __instance.lineFromRadarTargetToExit.enabled = false;
            return false;
        }
        return true;
    }

    [HarmonyPatch(typeof(ManualCameraRenderer), nameof(ManualCameraRenderer.MapCameraFocusOnPosition))]
    [HarmonyPostfix]
    static void ClippingPatch(ManualCameraRenderer __instance)// slightly increase camera clipping distance on Wither outside true interior
    {
        if (ValidCam(__instance) && TargetPlayerOutsideTrueInterior(__instance))
        {
            __instance.mapCamera.farClipPlane += 5;
        }
    }

    [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.SwitchMapMonitorPurpose))]
    [HarmonyPostfix]
    static void OnRadarEnable(StartOfRound __instance, bool displayInfo)// radar setup when first arriving (applies to both radar cams)
    {
        if (Wither.zaggyPresent && twoRadarCam == null)
        {
            twoRadarCam = Object.FindObjectOfType<Terminal>().GetComponent<ManualCameraRenderer>();
        }

        if (!displayInfo)// general patch to fix contour map/radar sprites getting stuck
        {
            FieldInfo field = AccessTools.Field(typeof(ManualCameraRenderer), "checkedForContourMap");
            if (field != null)
            {
                field.SetValue(__instance.mapScreen, false);
            }
        }

        if (!ScenePatches.onWither) { return; }

        ToggleShipIcon(__instance.mapScreen, displayInfo);

        if (twoRadarCam != null)
        {
            ToggleShipIcon(twoRadarCam, displayInfo);
        }
    }

    static void ToggleShipIcon(ManualCameraRenderer radarCam, bool show)// turn off the ship icon when on Wither (since it appears due to extended clip plane), turn it back on when leaving
    {
        if (radarCam.shipArrowUI != null)
        {
            Transform shipUI = radarCam.shipArrowUI.transform.Find("ShipIcon");
            if (shipUI != null)
            {
                if (!show)
                {
                    shipUI.gameObject.SetActive(false);
                }
                else
                {
                    shipUI.gameObject.SetActive(true);
                }
            }
        }
    }

    [HarmonyPatch(typeof(ManualCameraRenderer), nameof(ManualCameraRenderer.Update))]
    [HarmonyPostfix]
    static void ContourPosShipArrowUpdatePatch(ManualCameraRenderer __instance)
    {
        if (ValidCam(__instance))
        {
            ManualCameraRenderer mainRadarCam = StartOfRound.Instance.mapScreen;// the cam with the contour map
            if (mainRadarCam.contourMap != null)
            {
                // this is just borrowed from vanilla code so no custom functions here
                if ((__instance.targetedPlayer != null && !__instance.targetedPlayer.isInsideFactory) || (__instance.targetedPlayer == null && __instance.headMountedCamTarget.transform.position.y > -80f))
                {
                    // lower the position of the contour map on wither to give more room for radar sprites (applies to both radar cams)
                    mainRadarCam.contourMap.transform.position = new Vector3(mainRadarCam.contourMap.transform.position.x, __instance.headMountedCamTarget.transform.position.y - 4f, mainRadarCam.contourMap.transform.position.z);
                }
            }
            // if in false interior, disable arrow to ship (won't work for radar boosters)
            // for some reason vanilla checks player elevation to determine this (as opposed to isInsideFactory) so the false interior still shows the ship arrow normally since it's not as low down as the true interior
            if (TargetPlayerInFalseInterior(__instance))
            {
                __instance.shipArrowUI.SetActive(value: false);
            }
        }
    }

    [HarmonyPatch(typeof(PlayerControllerB), nameof(PlayerControllerB.SetNightVisionEnabled))]
    [HarmonyPostfix]
    static void NightVisionUpdate(PlayerControllerB __instance)// disable interior night vision effect when inside false interior
    {
        if (InFactoryTrigger.isInFalseInterior)
        {
            __instance.nightVision.enabled = false;
        }
    }

    [HarmonyPatch(typeof(PlayerControllerB), nameof(PlayerControllerB.TeleportPlayer))]
    [HarmonyPostfix]
    static void OnTeleport(PlayerControllerB __instance)// update false interior information when a player teleports
    {
        if (!ScenePatches.onWither || StartOfRound.Instance == null || InFactoryTrigger.playersInFalseInterior == null || StartOfRound.Instance.inShipPhase) { return; }

        // this will always set a player as not in the false interior, but if they're being teleported to the false interior, the flag is set after the teleport so this just gets overriden
        if (__instance == GameNetworkManager.Instance.localPlayerController)
        {
            InFactoryTrigger.isInFalseInterior = false;
        }
        else
        {
            int playerIndex = (int)__instance.playerClientId;
            if (playerIndex >= 0 && InFactoryTrigger.playersInFalseInterior.Contains(playerIndex))
            {
                //Wither.Logger.LogDebug($"CLEARING LIST: {playerIndex}");
                InFactoryTrigger.playersInFalseInterior.Remove(playerIndex);
            }
        }
    }
}