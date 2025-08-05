using HarmonyLib;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using Wither.Inside;

namespace Wither.Patches
{
    [HarmonyPatch]
    public class BetterDustClouds
    {
        private static bool initialSet = true;
        private static bool enableBuffer = true;
        private static bool transitionBuffer = false;
        private static bool transitionOverride = true;

        // mostly copied from my other mod ScienceBird Tweaks

        [HarmonyPatch(typeof(TimeOfDay), nameof(TimeOfDay.Update))]
        [HarmonyPostfix]
        static void ReEnable(TimeOfDay __instance)
        {
            if (!ScenePatches.onWither) { return; }

            if (__instance.currentLevelWeather == LevelWeatherType.DustClouds)
            {
                GameObject dustClouds = __instance.effects[0].effectObject;
                if (dustClouds != null && (!dustClouds.activeInHierarchy || !__instance.effects[0].effectEnabled) && GameNetworkManager.Instance.localPlayerController != null && !GameNetworkManager.Instance.localPlayerController.isInsideFactory)
                {
                    dustClouds.SetActive(true);
                    __instance.effects[0].effectEnabled = true;
                }
            }
        }

        [HarmonyPatch(typeof(TimeOfDay), nameof(TimeOfDay.Start))]
        [HarmonyPostfix]
        static void SetInitial(TimeOfDay __instance)
        {
            initialSet = true;
        }

        [HarmonyPatch(typeof(TimeOfDay), nameof(TimeOfDay.SetWeatherEffects))]
        [HarmonyPrefix]
        static void CheckBefore(TimeOfDay __instance)
        {
            if (!ScenePatches.onWither) { return; }

            GameObject dustClouds = TimeOfDay.Instance.effects[0].effectObject;
            if (__instance.currentLevelWeather == LevelWeatherType.DustClouds && dustClouds != null)
            {
                enableBuffer = __instance.effects[0].effectEnabled;
                transitionBuffer = __instance.effects[0].transitioning;
                __instance.effects[0].effectEnabled = false;
                __instance.effects[0].transitioning = transitionOverride;
            }
        }

        [HarmonyPatch(typeof(TimeOfDay), nameof(TimeOfDay.SetWeatherEffects))]
        [HarmonyPostfix]
        static void ChangeEffectObject(TimeOfDay __instance)
        {
            GameObject dustClouds = TimeOfDay.Instance.effects[0].effectObject;
            if (!ScenePatches.onWither)
            {
                if (initialSet && dustClouds != null && dustClouds.activeInHierarchy)// reset dust clouds to vanilla state if on any other moon
                {
                    AudioSource cloudsAudio = dustClouds.GetComponentInChildren<AudioSource>();
                    if (cloudsAudio != null)
                    {
                        cloudsAudio.Stop();
                    }
                    LocalVolumetricFog clouds = dustClouds.GetComponent<LocalVolumetricFog>();
                    if (clouds != null)
                    {
                        clouds.parameters.meanFreePath = 17f;
                    }
                    __instance.effects[0].lerpPosition = true;
                    initialSet = false;
                }
                return;
            }

            if (__instance.currentLevelWeather == LevelWeatherType.DustClouds && dustClouds != null)
            {
                __instance.effects[0].effectEnabled = enableBuffer;
                __instance.effects[0].transitioning = transitionBuffer;
                transitionOverride = true;
                if (initialSet)// one-time setup of cloud thickness and audio
                {
                    enableBuffer = true;
                    __instance.effects[0].lerpPosition = false;
                    GameObject cloudsAmbience = (GameObject)Wither.ExtraAssets.LoadAsset("WitherDustCloudsAmbience");
                    if (cloudsAmbience != null)
                    {
                        LocalVolumetricFog clouds = dustClouds.GetComponent<LocalVolumetricFog>();
                        if (clouds != null)
                        {
                            clouds.parameters.meanFreePath = 8f;
                        }
                        if (!dustClouds.GetComponentInChildren<AudioSource>())
                        {
                            GameObject audioObj = Object.Instantiate(cloudsAmbience, Vector3.zero, Quaternion.identity);
                            audioObj.transform.SetParent(dustClouds.transform, worldPositionStays: false);
                        }
                        AudioSource cloudsAudio = dustClouds.GetComponentInChildren<AudioSource>();
                        if (cloudsAudio != null)
                        {
                            cloudsAudio.Play();
                        }
                        else
                        {
                            Wither.Logger.LogError("Null dust clouds audio!");
                        }
                    }
                    initialSet = false;
                }
                if (__instance.effects[0].effectEnabled && GameNetworkManager.Instance.localPlayerController != null && !GameNetworkManager.Instance.localPlayerController.isInsideFactory)// essentially a replacement of existing SetWeatherEffects logic for Dust Clouds
                {
                    __instance.effects[0].transitioning = false;
                    if (__instance.effects[0].effectObject != null)
                    {
                        Vector3 vector = ((!GameNetworkManager.Instance.localPlayerController.isPlayerDead) ? StartOfRound.Instance.localPlayerController.transform.position : StartOfRound.Instance.spectateCamera.transform.position);
                        vector += Vector3.up * 4f;
                        dustClouds.transform.position = vector;
                    }
                }
                else if (!__instance.effects[0].transitioning)
                {
                    transitionOverride = false;
                }
            }
        }
    }

    [HarmonyPatch]
    public class DustSpaceClouds
    {
        [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.SetMapScreenInfoToCurrentLevel))]
        [HarmonyPostfix]
        [HarmonyPriority(Priority.Last)]
        static void AddSpaceToMapScreen(StartOfRound __instance)
        {
            string levelText = __instance.screenLevelDescription.text;
            if (!levelText.Contains("115 Wither")) { return;}

            if (levelText.Contains("DustClouds"))
            {
                levelText = levelText.Replace("DustClouds", "Dust Clouds");
                __instance.screenLevelDescription.text = levelText;
            }
        }

        [HarmonyPatch(typeof(Terminal), nameof(Terminal.LoadNewNode))]
        [HarmonyPostfix]
        [HarmonyPriority(Priority.Last)]
        [HarmonyAfter("mrov.terminalformatter")]
        static void AddSpaceToTerminal(Terminal __instance)
        {
            if (__instance.currentText.Contains("DustClouds"))
            {
                __instance.currentText = __instance.currentText.Replace("DustClouds", "Dust Clouds");
                __instance.screenText.text = __instance.currentText;
            }
        }
    }
}
