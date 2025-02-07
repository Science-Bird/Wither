using GameNetcodeStuff;
using System.Collections;
using Unity.Netcode;
using UnityEngine;

namespace Wither;
public class EnableScrapSpawn : NetworkBehaviour
{
	public int rarityPoint = 100;
	public void SetRarity(bool configVal)
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
            Wither.Logger.LogDebug($"Setting spawning to {configVal} for strange scrap (rarity {rarityPoint}, index {index}).");
            if (configVal)
			{
				RoundManager.Instance.currentLevel.spawnableScrap[index].rarity = rarityPoint;
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