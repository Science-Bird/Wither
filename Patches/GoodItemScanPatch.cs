using HarmonyLib;
using GoodItemScan;
using Wither.Events;

namespace Wither.Patches;

public class GoodItemScanPatch
{
    public static void DoPatching()
    {
        Wither.Harmony?.Patch(AccessTools.Method(typeof(Scanner), nameof(Scanner.AssignNodeToUIElement)), prefix: new HarmonyMethod(typeof(ScanElementPatch).GetMethod("AssignNodePatch")));
        Wither.Harmony?.Patch(AccessTools.Method(typeof(StartOfRound), nameof(StartOfRound.Start)), postfix: new HarmonyMethod(typeof(ScanElementPatch).GetMethod("OnStart")));
    }
}

public class ScanElementPatch
{
    public static bool scanDone = false;

    public static void OnStart(StartOfRound __instance)
    {
        scanDone = false;
    }

    public static void AssignNodePatch(Scanner __instance, ScanNodeProperties node)// check for withered lasher in custom scan method
    {
        if (!TerminalEntryPatches.unlocked && ScenePatches.onWither)
        {
            if (node.headerText == "Withered lasher")
            {
                scanDone = true;
                LasherManager.Instance.LasherEnemyScanLocal();
            }
        }
    }
}