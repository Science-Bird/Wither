using HarmonyLib;
using Wither.Events;

namespace Wither.Patches;

[HarmonyPatch]
public class InteriorPowerPatches
{
    [HarmonyPatch(typeof(RoundManager), nameof(RoundManager.TurnOnAllLights))]
    [HarmonyPostfix]
    static void OnPowerChange(RoundManager __instance, bool on)// breaker is synced with interior lights
    {
        if (!ScenePatches.onWither) { return; }

        if (on)
        {
            AnimatedLightsManager.Instance.PowerOn();
        }
        else
        {
            AnimatedLightsManager.Instance.PowerOff();
        }
    }
}