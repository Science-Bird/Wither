using UnityEngine;
using HarmonyLib;
using System;
using GameNetcodeStuff;
using System.Globalization;
using static UnityEngine.Rendering.DebugUI;

namespace Wither.Patches;

[HarmonyPatch]
public class ApparatusRotationPatch
{
    public static bool initialSet = true;

    public static Vector3 rotLastFrame = Vector3.zero;

    public static Vector3 targetRotation;

    public static Vector3 targetPosition;

    public static Vector3 positionOffset;

    public static bool posSet = true;

    private static bool tempInitial = true;


    [HarmonyPatch(typeof(GrabbableObject), nameof(GrabbableObject.LateUpdate))]
    [HarmonyPostfix]
    [HarmonyAfter("me.loaforc.facilitymeltdown")]
    static void UpdateRotation(GrabbableObject __instance)
    {
        if (__instance == Mechanics.InsertApparatus.insertedApparatus && (Mechanics.InsertApparatus.doingInsertion || Mechanics.InsertApparatus.isInserted))
        {
            __instance.grabbable = false;
            if (initialSet)
            {
                if (ModdedApparatusConfig.configDict.TryGetValue(__instance.itemProperties, out var value))
                {
                    string[] rotation = value.Item1.Value.Split(",");
                    string[] position = value.Item2.Value.Split(",");
                    Vector3 parsedRotation = Vector3.zero;
                    if (rotation.Length == 3)
                    {
                        if (float.TryParse(rotation[0], NumberStyles.Float, CultureInfo.InvariantCulture, out var x))
                        {
                            if (float.TryParse(rotation[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var y))
                            {
                                if (float.TryParse(rotation[2], NumberStyles.Float, CultureInfo.InvariantCulture, out var z))
                                {
                                    parsedRotation = new Vector3(x, y, z);
                                }
                            }
                        }
                    }
                    Vector3 parsedPosition = Vector3.zero;
                    if (position.Length == 3)
                    {
                        if (float.TryParse(position[0], NumberStyles.Float, CultureInfo.InvariantCulture, out var x))
                        {
                            if (float.TryParse(position[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var y))
                            {
                                if (float.TryParse(position[2], NumberStyles.Float, CultureInfo.InvariantCulture, out var z))
                                {
                                    parsedPosition = new Vector3(x, y, z);
                                }
                            }
                        }
                    }
                    targetRotation = parsedRotation + new Vector3(0f, 193f, 0f);
                    Wither.Logger.LogDebug($"Setting target to: {targetRotation}");
                    positionOffset = parsedPosition;
                    Wither.Logger.LogDebug($"Setting offset to: {positionOffset}");
                }
                else
                {
                    Wither.Logger.LogDebug("Couldn't find apparatus in dictionary!");
                    targetRotation = new Vector3(0f, 193f, 0f);
                    positionOffset = Vector3.zero;
                }
                initialSet = false;
            }

            if (positionOffset == Vector3.zero)
            {
                targetPosition = __instance.gameObject.transform.position;
            }
            //Wither.Logger.LogDebug($"{__instance.gameObject.transform.eulerAngles}, {rotLastFrame}");
            if ((Mathf.Abs(Quaternion.Dot(__instance.gameObject.transform.rotation, Quaternion.Euler(targetRotation))) < 0.99f || Mathf.Abs((__instance.gameObject.transform.position - targetPosition).magnitude) > 0.005f) && __instance.gameObject.transform.eulerAngles == rotLastFrame)
            {
                if (posSet && positionOffset != Vector3.zero)
                {
                    posSet = false;
                    targetPosition = __instance.transform.position + positionOffset;
                    Wither.Logger.LogDebug($"Setting target position! {targetPosition}");
                }
                Wither.Logger.LogDebug($"Fixing apparatus rotation! {__instance.gameObject.transform.eulerAngles}");
                __instance.transform.eulerAngles = targetRotation;
                Wither.Logger.LogDebug($"Fixed apparatus rotation! {__instance.gameObject.transform.eulerAngles}");
                if (!posSet)
                {
                    Wither.Logger.LogDebug($"Fixing apparatus position! {__instance.gameObject.transform.position}");
                    __instance.transform.position = targetPosition;
                    Wither.Logger.LogDebug($"Fixed apparatus position! {__instance.gameObject.transform.position}");
                    __instance.targetFloorPosition = __instance.transform.localPosition;
                }
            }
            rotLastFrame = __instance.gameObject.transform.eulerAngles;
        }
    }
}