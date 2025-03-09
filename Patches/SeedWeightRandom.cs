using UnityEngine;
using HarmonyLib;
using System;
using GameNetcodeStuff;
using Unity.Netcode;

namespace Wither.Patches;

[HarmonyPatch]
public class SeedWeightRandom
{
    private static System.Random weightRandom;

    public static float[] randomWeightsTemp = [0f, 0f, 0f];

    [HarmonyPatch(typeof(RoundManager), nameof(RoundManager.FinishGeneratingNewLevelClientRpc))]
    [HarmonyPostfix]
    static void SetWeights(RoundManager __instance)
    {
        if (__instance.currentLevel.PlanetName != "115 Wither") { return; }

        weightRandom = new System.Random(StartOfRound.Instance.randomMapSeed);
        for (int i = 0; i < 3; i++)
        {
            double randomVar = weightRandom.NextDouble();
            float randomWeight = (float)(randomVar * 2f + 0.5f);
            randomWeightsTemp[i] = randomWeight;
        }
        Wither.Logger.LogInfo($"Random weights: {randomWeightsTemp[0]}, {randomWeightsTemp[1]}, {randomWeightsTemp[2]}");
    }
}