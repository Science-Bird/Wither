using HarmonyLib;
using UnityEngine;

namespace Wither.Patches;

[HarmonyPatch]
public class FootstepsPatch
{
    public static bool swappedSurface = false;
    public static AudioClip[] sandClips;
    public static AudioClip sandHitClip;
    public static AudioClip[] gravelClips;
    public static AudioClip gravelHitClip;

    public static void LoadAssets()
    {
        AudioClip clip1 = (AudioClip)Wither.ExtraAssets.LoadAsset("sand1");
        AudioClip clip2 = (AudioClip)Wither.ExtraAssets.LoadAsset("sand2");
        AudioClip clip3 = (AudioClip)Wither.ExtraAssets.LoadAsset("sand3");
        AudioClip clip4 = (AudioClip)Wither.ExtraAssets.LoadAsset("sand4");
        sandHitClip = (AudioClip)Wither.ExtraAssets.LoadAsset("HitSand");
        sandClips = [clip1, clip2, clip3, clip4];
    }

    [HarmonyPatch(typeof(RoundManager), nameof(RoundManager.SyncScrapValuesClientRpc))]
    [HarmonyPostfix]
    static void OnSync(RoundManager __instance)// replace gravel surface sounds with custom sand sounds
    {
        if (ScenePatches.onWither && !swappedSurface)
        {
            for (int i = 0; i < StartOfRound.Instance.footstepSurfaces.Length; i++)
            {
                if (StartOfRound.Instance.footstepSurfaces[i].surfaceTag == "Gravel")
                {
                    gravelClips = StartOfRound.Instance.footstepSurfaces[i].clips;
                    gravelHitClip = StartOfRound.Instance.footstepSurfaces[i].hitSurfaceSFX;
                    StartOfRound.Instance.footstepSurfaces[i].clips = sandClips;
                    StartOfRound.Instance.footstepSurfaces[i].hitSurfaceSFX = sandHitClip;
                    swappedSurface = true;
                }
            }
        }
    }

    [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.ShipLeave))]
    [HarmonyPrefix]
    static void OnLeave(StartOfRound __instance)// reset gravel sounds back to normal
    {
        if (swappedSurface)
        {
            for (int i = 0; i < StartOfRound.Instance.footstepSurfaces.Length; i++)
            {
                if (StartOfRound.Instance.footstepSurfaces[i].surfaceTag == "Gravel")
                {
                    StartOfRound.Instance.footstepSurfaces[i].clips = gravelClips;
                    StartOfRound.Instance.footstepSurfaces[i].hitSurfaceSFX = gravelHitClip;
                    swappedSurface = false;
                }
            }
        }
    }
}