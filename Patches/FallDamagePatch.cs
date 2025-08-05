using GameNetcodeStuff;
using HarmonyLib;
using Wither.Mechanics;

namespace Wither.Patches;

[HarmonyPatch]
public class FallDamagePatch
{
    [HarmonyPatch(typeof(PlayerControllerB), nameof(PlayerControllerB.DamagePlayer))]
    [HarmonyPrefix]
    static void OnPlayerDamaged(PlayerControllerB __instance, ref int damageNumber, CauseOfDeath causeOfDeath, bool fallDamage, out bool __state)
    {
        __state = false;
        if (!ScenePatches.onWither) { return; }

        if (causeOfDeath == CauseOfDeath.Gravity && FallDamageTrigger.fallImmune)// use bool set by trigger to override damage
        {
            damageNumber = 0;
            __state = true;
        }
    }

    [HarmonyPatch(typeof(PlayerControllerB), nameof(PlayerControllerB.DamagePlayer))]
    [HarmonyPostfix]
    static void AfterPlayerDamaged(PlayerControllerB __instance, ref int damageNumber, CauseOfDeath causeOfDeath, bool fallDamage, bool __state)
    {
        if (!ScenePatches.onWither) { return; }

        if (__state)// if damage was overriden, don't play damage animation
        {
            __instance.playerBodyAnimator.ResetTrigger("Damage");
        }
    }
}