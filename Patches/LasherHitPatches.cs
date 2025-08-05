using UnityEngine;
using HarmonyLib;
using Wither.Mechanics;
using GameNetcodeStuff;
using System.Collections.Generic;
using System.Linq;

namespace Wither.Patches;

[HarmonyPatch]
public class LasherHitPatches
{
    [HarmonyPatch(typeof(SprayPaintItem), nameof(SprayPaintItem.TrySprayingWeedKillerBottle))]
    [HarmonyPrefix]
    static bool SprayLasherPatch(SprayPaintItem __instance)// custom raycast for using weed killer on lashers
    {
        if (!ScenePatches.onWither) { return true; }

        PlayerControllerB player = GameNetworkManager.Instance.localPlayerController;

        RaycastHit[] objectsSprayed;
        List<RaycastHit> objectsSprayedList = new List<RaycastHit>();
        Vector3 vector = player.gameplayCamera.transform.position - player.gameplayCamera.transform.forward * 0.7f;
        objectsSprayed = Physics.SphereCastAll(vector, 0.8f, player.gameplayCamera.transform.forward, 4.5f, 524288, QueryTriggerInteraction.Collide);
        objectsSprayedList = objectsSprayed.OrderBy((RaycastHit x) => x.distance).ToList();
        for (int i = 0; i < objectsSprayedList.Count; i++)
        {
            if (objectsSprayedList[i].collider.gameObject.GetComponent<LasherCollision>())
            {
                //Wither.Logger.LogDebug($"LASHER SPRAY HIT! {objectsSprayedList[i].collider.gameObject.GetComponent<LasherCollision>().lasherScript}");
                objectsSprayedList[i].collider.gameObject.GetComponent<LasherCollision>().lasherScript.KillLasherLocal();
                return false;
            }
        }
        return true;
    }

    [HarmonyPatch(typeof(ShotgunItem), nameof(ShotgunItem.ShootGun))]
    [HarmonyPostfix]
    static void ShootLasherPatch(ShotgunItem __instance, Vector3 shotgunPosition, Vector3 shotgunForward)// custom raycast for using shotgun on lashers
    {
        if (!ScenePatches.onWither || __instance.heldByEnemy != null) { return; }

        RaycastHit[] enemyColliders = new RaycastHit[10];
        Ray ray = new Ray(shotgunPosition - shotgunForward * 10f, shotgunForward);
        int num = Physics.SphereCastNonAlloc(ray, 5f, enemyColliders, 15f, 524288, QueryTriggerInteraction.Collide);
        for (int i = 0; i < num; i++)
        {
            if (!enemyColliders[i].transform.GetComponent<LasherCollision>())
            {
                continue;
            }
            IHittable component;
            if (enemyColliders[i].distance == 0f)
            {
                Wither.Logger.LogDebug("Spherecast started inside lasher collider.");
            }
            else if (!Physics.Linecast(shotgunPosition, enemyColliders[i].point, out var hitInfo, StartOfRound.Instance.collidersAndRoomMaskAndDefault, QueryTriggerInteraction.Ignore) && enemyColliders[i].transform.TryGetComponent<IHittable>(out component))
            {
                if (component.Hit(1, shotgunForward, __instance.playerHeldBy, playHitSFX: true))
                {
                    //Wither.Logger.LogDebug("SHOTGUN HIT LASHER");
                }
            }
        }
    }


}