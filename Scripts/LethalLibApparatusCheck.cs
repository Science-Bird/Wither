using LethalLib.Modules;
using Wither.Patches;

namespace Wither.Scripts;
public class LethalLibApparatusCheck
{
    public static void AddApparatuses()
    {
        foreach (var scrapItem in Items.scrapItems)
        {
            if (Mechanics.InsertApparatus.IsApparatus(scrapItem.item.itemName))
            {
                ModdedApparatusConfig.apparatusDict.TryAdd(scrapItem.item, (ModdedApparatusConfig.FilterSpecialCharacters(scrapItem.modName), ModdedApparatusConfig.FilterSpecialCharacters(scrapItem.item.itemName)));
            }
        }
    }
}