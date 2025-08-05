using System;
using System.Linq;
using HarmonyLib;
using Wither.Events;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using LethalModDataLib.Attributes;
using LethalModDataLib.Enums;

namespace Wither.Patches;

[HarmonyPatch]
public class TerminalEntryPatches
{
    [ModData(SaveWhen.OnAutoSave, LoadWhen.OnLoad, SaveLocation.CurrentSave)]
    public static bool unlocked = false;

    [ModData(SaveWhen.OnAutoSave, LoadWhen.OnLoad, SaveLocation.CurrentSave)]
    public static bool unread = true;

    public static TerminalNode lasherFile;

    public static void LoadAssets()
    {
        lasherFile = (TerminalNode)Wither.ExtraAssets.LoadAsset("LasherFile");
    }

    [HarmonyPatch(typeof(Terminal), nameof(Terminal.OnSubmit))]
    [HarmonyPrefix]
    static bool TerminalSubmit(Terminal __instance)// detect commands which should bring up lasher bestiary entry
    {
        if (!__instance.terminalInUse || (__instance.currentNode != null && __instance.currentNode.acceptAnything))
        {
            return true;
        }
        else if (unlocked)
        {
            string s = __instance.screenText.text.Substring(__instance.screenText.text.Length - __instance.textAdded);
            s = __instance.RemovePunctuation(s);
            string sTrimmed = s.Trim();
            string[] spacing = s.Split(sTrimmed);
            s = s.ToLower();
            if (s == "lasher" || s == "lashers" || (s.Length >= 8 && "withered lashers".Contains(s)))
            {
                __instance.screenText.ActivateInputField();
                __instance.screenText.Select();
                __instance.LoadNewNode(lasherFile);
                unread = false;
                return false;
            }
        }
        return true;
    }

    [HarmonyPatch(typeof(Terminal), nameof(Terminal.LoadNewNode))]
    [HarmonyPrefix]
    [HarmonyBefore("mrov.terminalformatter")]
    static void TerminalTextCheck(Terminal __instance, TerminalNode node, out bool __state)// detect when bestiary page is loaded
    {
        if (unlocked && node.displayText.Contains("[currentScannedEnemiesList]"))
        {
            __state = true;
        }
        else
        {
            __state = false;
        }
        
    }

    [HarmonyPatch(typeof(Terminal), nameof(Terminal.LoadNewNode))]
    [HarmonyPostfix]
    [HarmonyPriority(Priority.Last)]
    [HarmonyAfter("mrov.terminalformatter")]
    static void TerminalLogsTextOverride(Terminal __instance, bool __state)// add lasher to bestiary list
    {
        if (__state && unlocked)
        {
            string unreadString = unread ? " (NEW)" : "";
            //Wither.Logger.LogDebug($"{__instance.currentText}");

            // regex explanation: middle group will match literally anything, the surrounding groups say that the string shouldn't be preceded by the start of string or any amount of white space/"\n" and shouldn't be followed by the end of string or any amount of white space/"\n"
            // in practice, this means the matched text will have trimmed all the white space and instances of "\n" from either side, plus the first and last characters of the inner text (because those characters are also preceded/followed by the above mentioned characters)
            Match match = Regex.Match(__instance.currentText, "(?<!^)(?<!^(?:\\s|[-|/]|\\\\n|\\\\)+)([\\s\\S]+)(?!(?:\\s|[-|/]|\\\\n|n)+$)(?!$)");// I'll have you know this is NOT ai-generated, and the shit claude came up with could never compete with my masterpiece
            if (match.Success)
            {
                //Wither.Logger.LogDebug($"{match.Value}");
                // the way I actually implemented the regex has some side effects: I don't actually check that there is exactly a "\n" sequence, because regex lookbehinds/lookaheads don't deal with multiple characters well
                // instead, for the pre-string check, I check if there is "\n" or just "\" (so it doesn't see a "\" and think it's done because "\" alone won't match with "\n"), for the same reason, the post-string check does "\n" or "n"
                // the consequence of this is that if the first character of a string is specifically "\" or the last is specifically "n", they'll get skipped (even though they shouldn't since they don't make up a "\n" string)
                // to address this edge case, we apply an additional shift depending on if the string starts or ends with these characters (and the characters aren't part of a full "\n" string)
                int buffer1 = 0;
                int buffer2 = 0;
                char char1 = '\\';
                char char2 = 'n';
                if (!__instance.currentText.StartsWith("\n") && __instance.currentText.First() == char1)
                {
                    buffer1 = -1;
                }
                if (!__instance.currentText.EndsWith("\n") && __instance.currentText.Last() == char2)
                {
                    buffer2 = 1;
                }
                //Wither.Logger.LogDebug($"{__instance.currentText.IndexOf(match.Value)}, {buffer1}, __instance.currentText.Length, {buffer2}");
                // the base -1 and +2 are the offset we need to apply to include the initially trimmed first and last characters
                string mainBody = __instance.currentText.Substring(__instance.currentText.IndexOf(match.Value) - 1 + buffer1, match.Value.Length + 2 - buffer1 + buffer2);
                // this whole process is almost definitely an overly-complex solution but I had fun doing it so whatever

                // bunch of stuff to put all the strings back together and clean things up (some dependent on whether terminal formatter is installed)
                string[] padding = __instance.currentText.Split(mainBody);
                string mrovSpace = Wither.mrovTerminalPresent ? " " : "";
                mainBody += "\n" + mrovSpace + "Withered lashers" + unreadString;
                if (mainBody.Contains("NO DATA COLLECTED") || mainBody.Contains("Scan creatures to unlock their data."))
                {
                    mainBody += "\n";
                }

                string finalText = padding[0] + mainBody + padding[1];
                //Wither.Logger.LogDebug($"Final 1: {finalText}");
                if (finalText.Contains("No data collected on wildlife. Scans are required.") || finalText.Contains("NO DATA COLLECTED") || finalText.Contains("Scan creatures to unlock their data."))
                {
                    List<string> lines = finalText.Split("\n").ToList();
                    lines.RemoveAll(x => x.Contains("No data collected on wildlife. Scans are required.") || x.Contains("NO DATA COLLECTED") || x.Contains("Scan creatures to unlock their data."));
                    finalText = string.Join("\n", lines);
                }
                //Wither.Logger.LogDebug($"Final 2: {finalText}");
                __instance.currentText = finalText;
                __instance.screenText.text = __instance.currentText;
            }
            else
            {
                Wither.Logger.LogError("Failed to trim terminal text!");
            }
        }
    }

    [HarmonyPatch(typeof(HUDManager), nameof(HUDManager.AssignNewNodes))]
    [HarmonyPostfix]
    static void DetectLasherScan(HUDManager __instance)
    {
        if (unlocked || !ScenePatches.onWither || Wither.testPresent) { return; }// this won't run/work properly with TestAccount's GoodItemScan

        for (int i = 0; i < __instance.scanElements.Length; i++)
        {
            if (__instance.scanNodes.Count > 0 && __instance.scanNodes.TryGetValue(__instance.scanElements[i], out var value) && value != null)
            {
                try
                {
                    if (__instance.NodeIsNotVisible(value, i))
                    {
                        continue;
                    }
                    if (!__instance.scanElements[i].gameObject.activeSelf)
                    {
                        if (value.headerText == "Withered lasher")// send scanned lasher info to other clients
                        {
                            LasherManager.Instance.LasherEnemyScanLocal();
                            break;
                        }
                    }
                }
                catch (Exception arg)
                {
                    Wither.Logger.LogError($"Error in scan node patch: {arg}");
                }
            }
        }
    }


}