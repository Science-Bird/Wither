using UnityEngine;
using HarmonyLib;
using System;
using LethalLib.Modules;
using LethalLevelLoader;
using System.Collections.Generic;
using BepInEx.Configuration;

namespace Wither.Patches;

[HarmonyPatch]
public class ModdedApparatusConfig
{
    public static Dictionary<Item,(string,string)> apparatusDict = new Dictionary<Item, (string modname, string scrapname)>();
    public static Dictionary<Item,(ConfigEntry<string>,ConfigEntry<string>)> configDict = new Dictionary<Item, (ConfigEntry<string> rotationoffset, ConfigEntry<string> positionoffset)>();

    [HarmonyPrefix]
    [HarmonyAfter("imabatby.lethallevelloader")]
    [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.Start))]
    static void AfterFall(StartOfRound __instance)
    {
        foreach (var scrapItem in Items.scrapItems)
        {
            if ((scrapItem.item.itemName.Contains("Apparatus") || scrapItem.item.itemName.Contains("apparatus")) && !scrapItem.item.itemName.Contains("concept"))
            {
                apparatusDict.TryAdd(scrapItem.item, (scrapItem.modName, scrapItem.item.itemName));
            }
        }
        foreach (var extendedItem in PatchedContent.ExtendedItems)
        {
            if (extendedItem.ContentType == ContentType.Vanilla)
            {
                continue;
            }
            if ((extendedItem.Item.itemName.Contains("Apparatus") || extendedItem.Item.itemName.Contains("apparatus")) && !extendedItem.Item.itemName.Contains("concept"))
            {
                apparatusDict.TryAdd(extendedItem.Item, (extendedItem.ModName, extendedItem.Item.itemName));
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
            if (apparatus.Value.Item2.ToLower().Contains("egyptapparatus"))
            {
                defaultRotValue = "-90,0,0";
                defaultPosValue = "0,-0.1,0";
            }
            if (apparatus.Value.Item2.ToLower().Contains("egyptlapparatus"))
            {
                continue;
            }
            configDict.TryAdd(apparatus.Key, (Wither.Instance.Config.Bind("Rotation Offsets", $"{apparatus.Value.Item2} III {apparatus.Value.Item1}", defaultRotValue, "If this apparatus is appearing incorrectly in the socket, adjust its rotation here (should be a comma-separated string of x,y,z rotation angles)."), Wither.Instance.Config.Bind("Position Offsets", $"{apparatus.Value.Item2} III {apparatus.Value.Item1}", defaultPosValue, "If this apparatus is appearing incorrectly in the socket, adjust its position here (should be a comma-separated string of x,y,z displacement).")));
        }
    }
}