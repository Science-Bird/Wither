using UnityEngine;
using HarmonyLib;
using System;
using GameNetcodeStuff;

namespace Wither.Patches;

[HarmonyPatch]
public class RotationPatches
{
    public static bool initialSet = true;

    public static Vector3 rotLastFrame = new Vector3(0f, 0f, 0f);

    [HarmonyPatch(typeof(GrabbableObject), nameof(GrabbableObject.LateUpdate))]
    [HarmonyPostfix]
    static void UpdateRotation(GrabbableObject __instance)
    {
        if (!__instance.grabbable && !__instance.isHeld && (__instance.itemProperties.itemName.Contains("Apparatus") || __instance.itemProperties.itemName.Contains("apparatus")) && !__instance.itemProperties.itemName.Contains("concept"))
        {
            if (Mathf.Abs(__instance.gameObject.transform.eulerAngles.y - 193f) > 0.1f && __instance.gameObject.transform.eulerAngles == rotLastFrame)
            {
                __instance.transform.eulerAngles = new Vector3(0f, 193f, 0f);
            }
            rotLastFrame = __instance.gameObject.transform.eulerAngles;
        }
    }
}