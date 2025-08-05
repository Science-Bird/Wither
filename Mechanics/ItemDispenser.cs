using GameNetcodeStuff;
using System.Collections.Generic;
using System.Collections;
using Unity.Netcode;
using UnityEngine;
using System.Linq;
using LethalLevelLoader;
using DunGen;

namespace Wither.Mechanics;
public class ItemDispenser : NetworkBehaviour
{
    public Transform[] targetPositions;

    public Animator[] lightAnimators;

    private float[] randomWeights = [0f, 0f, 0f, 0f];

    public AudioSource tentaclePreAudio;
    public AudioClip tentacleWarning;

    private static int minVal;
    private static int maxVal;
    public static int apparatusValue;

    public void Start()
    {
        if (base.IsServer)// host generates random value multipliers on spawn
        {
            for (int i = 0; i < randomWeights.Length; i++)
            {
                randomWeights[i] = Random.Range(0.5f, 2.5f);
            }
            if (Wither.MinValue.Value > Wither.MaxValue.Value)
            {
                minVal = 50;
                maxVal = 300;
            }
            else
            {
                minVal = Wither.MinValue.Value;
                maxVal = Wither.MaxValue.Value;
            }
        }
    }

    public void SpawnItemsServer()
    {
        // grab all custom wither items except Ricardorb and dying apparatus, and put withered clock last in the list
        List<ExtendedItem> witheredEItems = PatchedContent.ExtendedItems.FindAll(x => x.ModName == "Wither" && x.Item.itemName != "Dying apparatus" && x.Item.itemName != "Ricardorb").OrderBy(x => x.Item.itemName == "Withered clock").ToList();
        List<GameObject> witheredPrefabs = new List<GameObject>();
        witheredEItems.ForEach(x => witheredPrefabs.Add(x.Item.spawnPrefab));

        // logs simulating the whole calculation process for debugging purposes
        //Wither.Logger.LogDebug($"-------------");
        //Wither.Logger.LogDebug($"Apparatus value calculation breakdown:");
        //Wither.Logger.LogDebug($"Initial base values: {(TimeOfDay.Instance.profitQuota * Wither.QuotaFraction.Value) / 3}, {(TimeOfDay.Instance.profitQuota * Wither.QuotaFraction.Value) / 3}, {(TimeOfDay.Instance.profitQuota * Wither.QuotaFraction.Value) / 3}, {(TimeOfDay.Instance.profitQuota * Wither.QuotaFraction.Value) / 3}");
        //Wither.Logger.LogDebug($"({Wither.QuotaFraction.Value * 100}% of quota {TimeOfDay.Instance.profitQuota} is {TimeOfDay.Instance.profitQuota * Wither.QuotaFraction.Value})");
        //float tempFloat = ((TimeOfDay.Instance.profitQuota * Wither.QuotaFraction.Value) / 3 > minVal) ? (TimeOfDay.Instance.profitQuota * Wither.QuotaFraction.Value / 3) : minVal;
        //Wither.Logger.LogDebug($"Restrict to minimum value ({minVal}): {tempFloat}, {tempFloat}, {tempFloat}");
        //float tempFloat1 = tempFloat * randomWeights[0];
        //float tempFloat2 = tempFloat * randomWeights[1];
        //float tempFloat3 = tempFloat * randomWeights[2];
        //float tempFloat4 = tempFloat * randomWeights[3];
        //Wither.Logger.LogDebug($"Apply random weights ({randomWeights[0]}, {randomWeights[1]}, {randomWeights[2]}, {randomWeights[3]}): {tempFloat1}, {tempFloat2}, {tempFloat3}, {tempFloat4}");
        //if (apparatusValue > 0)
        //{
        //    Wither.Logger.LogDebug($"Inserted apparatus value: {apparatusValue}, base apparatus scaling value: {Wither.ScalingBase.Value}");
        //    tempFloat1 = tempFloat1 * ((float)apparatusValue / (float)Wither.ScalingBase.Value);
        //    tempFloat2 = tempFloat2 * ((float)apparatusValue / (float)Wither.ScalingBase.Value);
        //    tempFloat3 = tempFloat3 * ((float)apparatusValue / (float)Wither.ScalingBase.Value);
        //    tempFloat4 = tempFloat4 * ((float)apparatusValue / (float)Wither.ScalingBase.Value);
        //    Wither.Logger.LogDebug($"Applying apparatus scaling ({((float)apparatusValue / (float)Wither.ScalingBase.Value)}x): {tempFloat1}, {tempFloat2}, {tempFloat3}, {tempFloat4}");
        //}
        //else
        //{
        //    Wither.Logger.LogDebug("Didn't find apparatus value! Not applying scaling factor.");
        //}
        //int tempInt1 = Mathf.RoundToInt(tempFloat1);
        //int tempInt2 = Mathf.RoundToInt(tempFloat2);
        //int tempInt3 = Mathf.RoundToInt(tempFloat3);
        //int tempInt4 = Mathf.RoundToInt(tempFloat4);
        //Wither.Logger.LogDebug($"Rounding to int: {tempInt1}, {tempInt2}, {tempInt3}, {tempInt4}");
        //tempInt1 = tempInt1 > minVal ? tempInt1 : minVal;
        //tempInt2 = tempInt2 > minVal ? tempInt2 : minVal;
        //tempInt3 = tempInt3 > minVal ? tempInt3 : minVal;
        //tempInt4 = tempInt4 > minVal ? tempInt4 : minVal;
        //Wither.Logger.LogDebug($"Restrict to minimum value ({minVal}): {tempInt1}, {tempInt2}, {tempInt3}, {tempInt4}");
        //tempInt1 = tempInt1 < maxVal ? tempInt1 : maxVal;
        //tempInt2 = tempInt2 < maxVal ? tempInt2 : maxVal;
        //tempInt3 = tempInt3 < maxVal ? tempInt3 : maxVal;
        //tempInt4 = tempInt4 < maxVal ? tempInt4 : maxVal;
        //Wither.Logger.LogDebug($"Restrict to maximum value ({maxVal}): {tempInt1}, {tempInt2}, {tempInt3}, {tempInt4}");
        //tempInt4 = Mathf.RoundToInt((float)tempInt4 * Wither.ExtraMultiplier.Value);
        //Wither.Logger.LogDebug($"Applying 4th item bonus: ({Wither.ExtraMultiplier.Value}x): {tempInt1}, {tempInt2}, {tempInt3}, {tempInt4}");
        //Wither.Logger.LogDebug($"");
        //Wither.Logger.LogDebug($"FINAL VALUES:");
        //Wither.Logger.LogDebug($"Item1: {tempInt1}, Item2: {tempInt2}, Item3: {tempInt3}, Item4: {tempInt4}");
        //Wither.Logger.LogDebug($"-------------");

        StartCoroutine(SpawningRoutine(witheredPrefabs));
    }

    public IEnumerator SpawningRoutine(List<GameObject> witheredPrefabs)// main server-side routine for spawning items (similar to vanilla gift box)
    {
        for (int i = 0; i < witheredPrefabs.Count; i++)
        {
            // same as simulated above
            float scrapValueFloat = (TimeOfDay.Instance.profitQuota * Wither.QuotaFraction.Value) / 3;
            if (scrapValueFloat < minVal)
            {
                scrapValueFloat = minVal;
            }
            float randomWeight = randomWeights[i];
            scrapValueFloat = scrapValueFloat * randomWeight;
            if (apparatusValue > 0)
            {
                scrapValueFloat = scrapValueFloat * ((float)apparatusValue / (float)Wither.ScalingBase.Value);
            }

            int scrapValue = Mathf.RoundToInt(scrapValueFloat);
            if (scrapValue < minVal)
            {
                scrapValue = minVal;
            }
            else if (scrapValue > maxVal)
            {
                scrapValue = maxVal;
            }
            if (i == 3)
            {
                scrapValue = Mathf.RoundToInt((float)scrapValue * Wither.ExtraMultiplier.Value);
            }

            GameObject dispensedItem = UnityEngine.Object.Instantiate(witheredPrefabs[i], targetPositions[i].position, Quaternion.identity, RoundManager.Instance.spawnedScrapContainer);
            GrabbableObject grabbable = dispensedItem.GetComponent<GrabbableObject>();
            grabbable.startFallingPosition = targetPositions[i].position;
            StartCoroutine(SetObjectToHitGroundSFX(grabbable));
            grabbable.targetFloorPosition = grabbable.GetItemFloorPosition(grabbable.transform.position);
            grabbable.SetScrapValue(scrapValue);
            int randomRot = Random.RandomRangeInt(0, 360);
            grabbable.floorYRot = randomRot;
            grabbable.NetworkObject.Spawn();
            StartSpawningItemClientRpc(dispensedItem.GetComponent<NetworkObject>(), scrapValue, targetPositions[i].position, randomRot, i);
            yield return new WaitForSeconds(1f);
        }
    }

    private IEnumerator SetObjectToHitGroundSFX(GrabbableObject gObject)
    {
        yield return new WaitForEndOfFrame();
        gObject.reachedFloorTarget = false;
        gObject.hasHitGround = false;
        gObject.fallTime = 0f;
    }

    [ClientRpc]
    public void StartSpawningItemClientRpc(NetworkObjectReference netObjectRef, int scrapValue, Vector3 startFallingPos, int randomYRot, int index)
    {
        {
            lightAnimators[index].SetTrigger("switchOn");// turn on a light animator for each item spawned
            if (index == randomWeights.Length - 1)
            {
                // after the last item is spawned, play growling audio and shake screen
                HUDManager.Instance.ShakeCamera(ScreenShakeType.Big);
                tentaclePreAudio.PlayOneShot(tentacleWarning);
                WalkieTalkie.TransmitOneShotAudio(tentaclePreAudio, tentacleWarning);
            }
            if (!base.IsServer)
            {
                StartCoroutine(WaitForItemToSpawn(netObjectRef, scrapValue, startFallingPos, randomYRot, index));
            }
        }
    }

    private IEnumerator WaitForItemToSpawn(NetworkObjectReference netObjectRef, int scrapValue, Vector3 startFallingPos, int randomYRot, int index)
    {
        NetworkObject netObject = null;
        float startTime = Time.realtimeSinceStartup;
        while (Time.realtimeSinceStartup - startTime < 8f && !netObjectRef.TryGet(out netObject))
        {
            yield return new WaitForSeconds(0.03f);
        }
        if (netObject == null)
        {
            Wither.Logger.LogError("No network object found for spawned scrap!");
            yield break;
        }
        yield return new WaitForEndOfFrame();
        GrabbableObject grabbable = netObject.GetComponent<GrabbableObject>();
        RoundManager.Instance.totalScrapValueInLevel += grabbable.scrapValue;
        grabbable.SetScrapValue(scrapValue);
        grabbable.floorYRot = randomYRot;
        grabbable.startFallingPosition = startFallingPos;
        grabbable.fallTime = 0f;
        grabbable.hasHitGround = false;
        grabbable.reachedFloorTarget = false;
    }
}

