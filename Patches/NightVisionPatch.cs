using UnityEngine;
using HarmonyLib;
using System;
using GameNetcodeStuff;
using Wither.Inside;

namespace Wither.Patches;

[HarmonyPatch]
public class NightVisionPatches
{
    [HarmonyPatch(typeof(PlayerControllerB), nameof(PlayerControllerB.SetNightVisionEnabled))]
    [HarmonyPostfix]
    static void NightVisionUpdate(PlayerControllerB __instance)
    {
        if (InFactoryTrigger.isInFalseInterior)
        {
            __instance.nightVision.enabled = false;
        }
    }

    [HarmonyPatch(typeof(EntranceTeleport), nameof(EntranceTeleport.TeleportPlayer))]
    [HarmonyPostfix]
    static void EnterOrExitRealInterior(EntranceTeleport __instance)
    {
        InFactoryTrigger.isInFalseInterior = false;
    }
}