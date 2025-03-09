using UnityEngine;
using HarmonyLib;
using System;
using GameNetcodeStuff;
using Unity.Netcode;
using Wither.Mechanics;

namespace Wither.Patches;

[HarmonyPatch]
public class TooltipPatch
{
    private RaycastHit hit;

    private static bool flag = false;

    [HarmonyPatch(typeof(PlayerControllerB), nameof(PlayerControllerB.SetHoverTipAndCurrentInteractTrigger))]
    [HarmonyPrefix]
    static void TooltipDetectApparatus(PlayerControllerB __instance)
    {
        if (InsertApparatus.isInserted || InsertApparatus.doingInsertion)
        {
            if (!__instance.isGrabbingObjectAnimation && !__instance.inSpecialMenu && !__instance.quickMenuManager.isMenuOpen)
            {
                Ray interactRay = new Ray(__instance.gameplayCamera.transform.position, __instance.gameplayCamera.transform.forward);
                RaycastHit hit;
                if (Physics.Raycast(interactRay, out hit, __instance.grabDistance, __instance.interactableObjectsMask) && hit.collider.gameObject.layer != 8 && hit.collider.gameObject.layer != 30)
                {
                    string text = hit.collider.tag;
                    if (text == "PhysicsProp")
                    {
                        GrabbableObject component = hit.collider.gameObject.GetComponent<GrabbableObject>();
                        if (component != null && component == InsertApparatus.insertedApparatus)
                        {
                            flag = true;
                        }
                    }
                }
            }
        }
    }

    [HarmonyPatch(typeof(PlayerControllerB), nameof(PlayerControllerB.SetHoverTipAndCurrentInteractTrigger))]
    [HarmonyPostfix]
    [HarmonyAfter("me.loaforc.facilitymeltdown")]
    static void DisableTooltip(PlayerControllerB __instance)
    {
        if ((InsertApparatus.isInserted || InsertApparatus.doingInsertion) && flag)
        {
            flag = false;
            __instance.cursorIcon.enabled = false;
            __instance.cursorTip.text = "";
        }
    }
}