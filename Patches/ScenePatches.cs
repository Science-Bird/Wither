using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;
using Wither.Events;
using Wither.Inside;
using Wither.Scripts;

namespace Wither.Patches;

[HarmonyPatch]
public class ScenePatches
{
    public static bool onWither = false;
    private static bool done = false;
    private static bool waitingForLoad = false;

    // various things for loading and initialization

    [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.SceneManager_OnLoadComplete1))]
    [HarmonyPostfix]
    static void OnLoad(StartOfRound __instance, string sceneName)
    {
        InFactoryTrigger.isInFalseInterior = false;
        if (InFactoryTrigger.playersInFalseInterior != null)
        {
            InFactoryTrigger.playersInFalseInterior.Clear();
        }
        if (sceneName == "WitherScene")
        {
            onWither = true;
            if (Wither.mrovWeatherPresent && Scripts.MrovBlackoutCheck.CheckCurrentWeather() == "Blackout" && AnimatedLightsManager.Instance != null)
            {
                // enter post-alarm shut down mode during blackout
                AnimatedLightsManager.Instance.PowerOff(true);
                AnimatedLightsManager.Instance.EndAlarmSequence();
            }
        }
    }

    [HarmonyPatch(typeof(GiantKiwiAI), nameof(GiantKiwiAI.SpawnBirdNest))]
    [HarmonyPrefix]
    static bool OnKiwiSpawn(GiantKiwiAI __instance)// override kiwi spawns so they only spawn in certain parts of the map (mainly not in the desert or at tight chokepoints)
    {
        if (!onWither) { return true; }
        // this whole method is just copied from vanilla, but with different array sorting to exclude certain points

        GameObject[] array = (from x in GameObject.FindGameObjectsWithTag("OutsideAINode")
                              orderby NestDistanceCheck(x.transform.position, StartOfRound.Instance.shipLandingPosition.transform.position) descending
                              select x).ToArray();

        Vector3 position = array[0].transform.position;
        System.Random random = new System.Random(StartOfRound.Instance.randomMapSeed + 288);
        int num = random.Next(0, array.Length / 3);
        bool flag = false;
        Vector3 vector = Vector3.zero;
        for (int i = 0; i < array.Length; i++)
        {
            position = array[num].transform.position;
            position = RoundManager.Instance.GetRandomNavMeshPositionInBoxPredictable(position, 15f, default(NavMeshHit), random, RoundManager.Instance.GetLayermaskForEnemySizeLimit(__instance.enemyType));
            position = RoundManager.Instance.PositionWithDenialPointsChecked(position, array, __instance.enemyType, __instance.enemyType.nestDistanceFromShip);
            flag = __instance.CheckPathFromNodeToShip(array[num].transform);
            if (flag)
            {
                vector = RoundManager.Instance.PositionEdgeCheck(position, __instance.enemyType.nestSpawnPrefabWidth);
            }
            if (vector == Vector3.zero || !flag)
            {
                num++;
                if (num > array.Length - 1)
                {
                    position = RoundManager.Instance.GetRandomNavMeshPositionInBoxPredictable(array[0].transform.position, 15f, default(NavMeshHit), random, RoundManager.Instance.GetLayermaskForEnemySizeLimit(__instance.enemyType));
                    break;
                }
                continue;
            }
            position = vector;
            break;
        }
        GameObject gameObject = UnityEngine.Object.Instantiate(__instance.birdNestPrefab, position, Quaternion.Euler(Vector3.zero), RoundManager.Instance.mapPropsContainer.transform);
        gameObject.transform.Rotate(Vector3.up, random.Next(-180, 180), Space.World);
        if (!gameObject.gameObject.GetComponentInChildren<NetworkObject>())
        {
            Wither.Logger.LogError("Error: No NetworkObject found in enemy nest spawn prefab that was just spawned on the host: '" + gameObject.name + "'");
        }
        else
        {
            gameObject.gameObject.GetComponentInChildren<NetworkObject>().Spawn(destroyWithScene: true);
        }
        __instance.birdNestAmbience = gameObject.GetComponent<AudioSource>();
        __instance.birdNest = gameObject;
        __instance.agent.enabled = false;
        __instance.transform.position = __instance.birdNest.transform.position;
        __instance.transform.rotation = __instance.birdNest.transform.rotation;
        __instance.agent.enabled = true;
        return false;
    }

    private static float NestDistanceCheck(Vector3 position, Vector3 shipPosition)
    {
        if (position.z > 50f || (position.y > 40f && position.x > -155f) || (position.y < 40f && position.x < -75f))// exclude nodes around main structure, in desert, or next to entrances
        {
            return 0f;
        }
        return Vector3.Distance(position, shipPosition);
    }

    [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.Update))]
    [HarmonyPostfix]
    [HarmonyAfter("voxx.LethalElementsPlugin")]
    static void OnLatestLoad(StartOfRound __instance)// compatibility workaround for LethalElements snow weather conditions
    {
        if (onWither && waitingForLoad && !__instance.shipDoorsAnimator.GetBool("Closed"))// after ship doors open
        {
            waitingForLoad = false;
            // these are collision meshes only given renderers so LethalElements will use them for snow, we disable these renderers after LE has had time to copy them
            GameObject grass = GameObject.Find("Terrain/TerrainMain/Terrain/GrassTerrain");
            GameObject rock = GameObject.Find("Terrain/TerrainMain/Terrain/RockTerrain");
            GameObject sand = GameObject.Find("Terrain/TerrainMain/Terrain/SandTerrain");
            grass.GetComponent<MeshRenderer>().enabled = false;
            rock.GetComponent<MeshRenderer>().enabled = false;
            sand.GetComponent<MeshRenderer>().enabled = false;
        }
    }

    [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.SwitchMapMonitorPurpose))]
    [HarmonyPostfix]
    static void InitialSetup(StartOfRound __instance, bool displayInfo)// for stuff that needs to happen a little later after load
    {
        if (onWither && !__instance.inShipPhase && !displayInfo && !done)
        {
            done = true;
            waitingForLoad = true;

            // garage door is disabled a short time after initial load since loading with it enabled messes up nav mesh for some reason
            GameObject industrialR = GameObject.Find("CatwalksMesa/IndustrialEntranceR/GarageDoor");
            if (industrialR != null)
            {
                Transform garageDoor = industrialR.transform.Find("Door");
                if (garageDoor != null)
                {
                    garageDoor.gameObject.SetActive(true);
                }
            }

            // the dying apparatus on the map doesn't actually spawn in the docked state, so we find any dying apparatus which isn't in the ship and set it to docked
            GrabbableObject[] appList = Object.FindObjectsOfType<GrabbableObject>().Where(x => x is LungProp && x.itemProperties != null && x.itemProperties.itemName == "Dying apparatus" && !x.isInShipRoom && !x.scrapPersistedThroughRounds).ToArray();
            if (appList.Length > 0)
            {
                LungProp lungScript = (LungProp)appList.First();
                lungScript.isLungPowered = true;
                lungScript.isLungDocked = true;
                AudioSource lungAudio = lungScript.gameObject.GetComponentInChildren<AudioSource>();
                lungAudio.loop = true;
                lungAudio.Play();
            }

            // lashers need to load enabled so their network behaviours spawn properly
            if (LasherManager.Instance != null)
            {
                LasherManager.Instance.InitialDeactivate();
            }

            if (SpecialEventHandler.Instance != null && __instance.IsServer)// only host config will affect garage door state and vanilla plus mode
            {
                if (Wither.GarageStartOpen.Value)
                {
                    SpecialEventHandler.Instance.garageLeverAnimTrigger.TriggerAnimation(GameNetworkManager.Instance.localPlayerController);
                    SpecialEventHandler.Instance.garageDoorAnimTrigger.TriggerAnimation(GameNetworkManager.Instance.localPlayerController);
                }

                if (Wither.VanillaPlusMode.Value)
                {
                    if (DataSync.Instance != null)
                    {
                        DisableEventObjects();
                        DataSync.Instance.SetVanillaPlusModeClientRpc();
                    }
                }
            }
            
        }
    }

    [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.OnShipLandedMiscEvents))]
    [HarmonyPostfix]
    static void OnLand(StartOfRound __instance)
    {
        if (onWither)// disable and re-enable lights with "On Enable" shadows to fix an issue where they would fail to render shadows or seemingly cache shadows at the wrong time
        {
            List<GameObject> staticShadowObjects = new List<GameObject>();
            staticShadowObjects.Add(GameObject.Find("Spotlight1/Spotlight_big_v4_Holder/Spotlight_big_v4_Spotlight/OutsideSpotlight1"));
            staticShadowObjects.Add(GameObject.Find("Spotlight2/Spotlight_big_v4_Holder/Spotlight_big_v4_Spotlight/OutsideSpotlight1"));
            staticShadowObjects.Add(GameObject.Find("Lights/WallLight2/Light"));
            staticShadowObjects.Add(GameObject.Find("Lights/WallLight4/Light"));
            staticShadowObjects.Add(GameObject.Find("Lights/PitBroadIllum"));
            staticShadowObjects.Add(GameObject.Find("ExtraFanPitLights/BroadPitIllum"));
            staticShadowObjects.Add(GameObject.Find("PitLightE/Point Light"));
            staticShadowObjects.Add(GameObject.Find("PitLightS/Point Light"));
            staticShadowObjects.Add(GameObject.Find("PitLightW/Point Light"));
            staticShadowObjects.Add(GameObject.Find("Bar Lights/Neon Bars/Point Light (3)"));
            staticShadowObjects.Add(GameObject.Find("Bar Lights/Neon Bars/Point Light (4)"));
            staticShadowObjects.Add(GameObject.Find("Bar Lights/Neon Bars (1)/Point Light (3)"));
            staticShadowObjects.Add(GameObject.Find("Bar Lights/Neon Bars (2)/Point Light (3)"));
            staticShadowObjects.Add(GameObject.Find("Bar Lights/Neon Bars (2)/Point Light (4)"));
            staticShadowObjects.Add(GameObject.Find("Bar Lights/Neon Bars (3)/Point Light (3)"));
            staticShadowObjects.Add(GameObject.Find("Bar Lights/Neon Bars (3)/Point Light (4)"));
            staticShadowObjects.Add(GameObject.Find("2short (3)/HangingLight (40)/Light (1)"));
            staticShadowObjects.Add(GameObject.Find("IndustrialEntranceL/HangingLight/Light"));
            foreach (GameObject obj in staticShadowObjects)
            {
                if (obj != null)
                {
                    AnimatedLightsManager.RestartLight(obj);
                }
            }
        }
    }

    [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.ShipLeave))]
    [HarmonyPostfix]
    static void OnLeave(StartOfRound __instance)
    {
        onWither = false;
        done = false;
    }

    public static void DisableEventObjects()
    {
        SpecialEventHandler.Instance.topFloorObjects.SetActive(false);
        SpecialEventHandler.Instance.interiorScrapDrop.SetActive(false);
        SpecialEventHandler.Instance.interiorBreakerBox.SetActive(false);
        SpecialEventHandler.Instance.exteriorBreakerBox.SetActive(false);
    }
}