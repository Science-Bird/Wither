using HarmonyLib;
using LethalLevelLoader;
using System.Collections.Generic;
using BepInEx.Configuration;
using Wither.Scripts;

namespace Wither.Patches;

[HarmonyPatch]
public class ModdedApparatusConfig
{
    public static Dictionary<Item,(string,string)> apparatusDict = new Dictionary<Item, (string modname, string scrapname)>();
    public static Dictionary<Item,(ConfigEntry<string>,ConfigEntry<string>)> configDict = new Dictionary<Item, (ConfigEntry<string> rotationoffset, ConfigEntry<string> positionoffset)>();

    [HarmonyPrefix]
    [HarmonyAfter("imabatby.lethallevelloader")]
    [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.Start))]
    static void OnStart(StartOfRound __instance)
    {
        if (Wither.evaisaPresent)// items from LethalLib
        {
            LethalLibApparatusCheck.AddApparatuses();
        }
        foreach (var extendedItem in PatchedContent.ExtendedItems)// items from LLL
        {
            if (extendedItem.ContentType == ContentType.Vanilla)
            {
                continue;
            }
            if (Mechanics.InsertApparatus.IsApparatus(extendedItem.Item.itemName))
            {
                apparatusDict.TryAdd(extendedItem.Item, (FilterSpecialCharacters(extendedItem.ModName), FilterSpecialCharacters(extendedItem.Item.itemName)));
            }
        }
        ConfigLoader();
    }

    static void ConfigLoader()
    {
        foreach (var apparatus in apparatusDict)
        {
            string defaultRotValue = "0,0,0";
            string defaultPosValue = "0,0,0";
            Wither.Logger.LogDebug($"Found apparatus: {apparatus.Value.Item2}");
            //default values
            if (apparatus.Value.Item2.ToLower().Contains("egyptapparatus"))
            {
                defaultRotValue = "-90,0,0";
                defaultPosValue = "0,-0.1,0";
            }
            if (apparatus.Value.Item2.ToLower().Contains("egyptlapparatus"))
            {
                continue;
            }
            if (apparatus.Value.Item2.ToLower().Contains("dull apparatus") || apparatus.Value.Item2.ToLower().Contains("mech apparatus"))
            {
                defaultPosValue = "0.07,0,0";
            }
            configDict.TryAdd(apparatus.Key, (Wither.Instance.Config.Bind("Rotation Offsets", $"{apparatus.Value.Item2} - {apparatus.Value.Item1}", defaultRotValue, "If this apparatus is appearing incorrectly in the socket, adjust its rotation here (should be a comma-separated string of x,y,z rotation angles)."), Wither.Instance.Config.Bind("Position Offsets", $"{apparatus.Value.Item2} - {apparatus.Value.Item1}", defaultPosValue, "If this apparatus is appearing incorrectly in the socket, adjust its position here (should be a comma-separated string of x,y,z displacement).")));
        }
    }

    public static string FilterSpecialCharacters(string input)
    {
        return input.Replace("\n", "").Replace("\t", "").Replace("\\", "").Replace("\"", "").Replace("'", "").Replace("[", "").Replace("]", "");
    }
}