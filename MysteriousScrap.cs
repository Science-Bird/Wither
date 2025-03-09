using GameNetcodeStuff;
using System.Collections;
using Unity.Netcode;
using UnityEngine;

namespace Wither;
public class MysteriousScrap : NetworkBehaviour
{
	private void OnEnable()
	{
		SetRarity();
    }

    public void SetRarity()
	{
		if (RoundManager.Instance.currentLevel != null)
		{
			int index = 0;
            for (int i = 0; i < RoundManager.Instance.currentLevel.spawnableScrap.Count; i++)
            {
				if (RoundManager.Instance.currentLevel.spawnableScrap[i].spawnableItem.itemName == "Ricardorb")
				{
					index = i;
					break;
				}
            }
            Wither.Logger.LogDebug($"Setting spawning to {Wither.MysteriousScrap.Value} for strange scrap (rarity {Wither.MysteriousScrapRarity.Value}, index {index}).");
            if (Wither.MysteriousScrap.Value)
			{
				RoundManager.Instance.currentLevel.spawnableScrap[index].rarity = Wither.MysteriousScrapRarity.Value;
			}
			else {
				RoundManager.Instance.currentLevel.spawnableScrap[index].rarity = 0;
			}
		}
		else
		{
            Wither.Logger.LogDebug("No selectable level found, skipping scrap config check.");
		}
	}
}