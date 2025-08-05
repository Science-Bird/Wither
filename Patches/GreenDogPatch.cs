using HarmonyLib;
using UnityEngine;

namespace Wither.Patches;

[HarmonyPatch]
public class GreenDogPatch
{
    public static Material greenMat;
    public static bool checking = false;
    public static Vector3 spawnLocation = new Vector3(-120.4989f, 65.56802f, -44.3673f);

    public static void LoadAssets()
    {
        greenMat = (Material)Wither.ExtraAssets.LoadAsset("MouthDogTexGreen");
    }

    [HarmonyPatch(typeof(EnemyAI), nameof(EnemyAI.Start))]
    [HarmonyPostfix]
    static void OnDogSpawn(EnemyAI __instance)// when a dog spawns within a certain range and checking is active, make it green
    {
        if (checking && __instance is MouthDogAI dog && Vector3.Distance(dog.transform.position, spawnLocation) < 15f)
        {
            foreach (SkinnedMeshRenderer renderer in dog.skinnedMeshRenderers)
            {
                renderer.material = greenMat;
            }
        }
    }
}