using GameNetcodeStuff;
using System.Collections;
using Unity.Netcode;
using UnityEngine;

namespace Wither;
public class InsertApparatus : NetworkBehaviour
{
	public StartOfRound playersManager;

	public AnimatedObjectTrigger objectsEnableTrigger;

	public InteractTrigger insertTrigger;

	public AnimatedObjectTrigger animatedDoorTrigger;

	public bool isInserted;

	private void Update()
	{
		if (!isInserted)
		{
			if (GameNetworkManager.Instance.localPlayerController.currentlyHeldObjectServer != null && (GameNetworkManager.Instance.localPlayerController.currentlyHeldObjectServer.itemProperties.itemName.Contains("Apparatus") || GameNetworkManager.Instance.localPlayerController.currentlyHeldObjectServer.itemProperties.itemName.Contains("apparatus")) && !GameNetworkManager.Instance.localPlayerController.currentlyHeldObjectServer.itemProperties.itemName.Contains("concept"))
			{
				if (StartOfRound.Instance.localPlayerUsingController)
				{
					insertTrigger.hoverTip = "Insert apparatus: [D-pad up]";
				}
				else
				{
					insertTrigger.hoverTip = "Insert apparatus: [ E ]";
				}
			}
			else
			{
				insertTrigger.hoverTip = "Nothing to insert";
			}
		}
	}

	public void InsertItem()
	{
		PlayerControllerB playerInserting = GameNetworkManager.Instance.localPlayerController;
		if (playerInserting.currentlyHeldObjectServer != null && (playerInserting.currentlyHeldObjectServer.itemProperties.itemName.Contains("Apparatus") || playerInserting.currentlyHeldObjectServer.itemProperties.itemName.Contains("apparatus")) && !playerInserting.currentlyHeldObjectServer.itemProperties.itemName.Contains("concept") && !playerInserting.isGrabbingObjectAnimation)
		{
			UnityEngine.Object.Destroy(playerInserting.currentlyHeldObjectServer.radarIcon.gameObject);
			playerInserting.DestroyItemInSlotAndSync(playerInserting.currentItemSlot);
			objectsEnableTrigger.TriggerAnimation(GameNetworkManager.Instance.localPlayerController);
			animatedDoorTrigger.TriggerAnimation(GameNetworkManager.Instance.localPlayerController);
		}
	}
}
