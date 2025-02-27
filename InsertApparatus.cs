using GameNetcodeStuff;
using System.Collections;
using System.Numerics;
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

	public NetworkObject apparatusContainer;

	public static bool doingInsertion = false;

	private bool initialSet = true;

	private NetworkObject networkedApp;

    private void Update()
	{
		if (initialSet)
		{
			playersManager = Object.FindObjectOfType<StartOfRound>();
			initialSet = false;
			doingInsertion = false;
		}
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
            if (playerInserting.currentlyHeldObjectServer.radarIcon != null)
			{
                UnityEngine.Object.Destroy(playerInserting.currentlyHeldObjectServer.radarIcon.gameObject);
            }
			if (playerInserting.currentlyHeldObjectServer.gameObject.GetComponentInChildren<ScanNodeProperties>() != null)
            {
                UnityEngine.Object.Destroy(playerInserting.currentlyHeldObjectServer.gameObject.GetComponentInChildren<ScanNodeProperties>().gameObject);
            }
			AudioSource[] audioPlayers = playerInserting.currentlyHeldObjectServer.GetComponentsInChildren<AudioSource>();
			if (audioPlayers != null)
			{
				foreach (AudioSource audioPlayer in audioPlayers)
				{
					audioPlayer.Stop();
				}
			}
            DestroyChildObjectsServerRpc((int)playerInserting.playerClientId);

			ParentObjectToSlotServerRpc(playerInserting.currentlyHeldObjectServer.gameObject.GetComponent<NetworkObject>());
			playerInserting.DiscardHeldObject(placeObject: true, apparatusContainer, new UnityEngine.Vector3(0f, 0f, 0f), matchRotationOfParent: false);
			AudioSource newAudioPlayer = apparatusContainer.GetComponent<AudioSource>();
            if (newAudioPlayer != null)
            {
                newAudioPlayer.Play();
            }
            else
            {
                Wither.Logger.LogError("Null audio player!");
            }
            objectsEnableTrigger.TriggerAnimation(GameNetworkManager.Instance.localPlayerController);
			animatedDoorTrigger.TriggerAnimation(GameNetworkManager.Instance.localPlayerController);
		}
	}

    [ServerRpc(RequireOwnership = false)]
    public void DestroyChildObjectsServerRpc(int playerObj)
    {
        DestroyChildObjectsClientRpc(playerObj);
    }

	[ClientRpc]
	public void DestroyChildObjectsClientRpc(int playerObj)
	{
        doingInsertion = true;
        if (playersManager.allPlayerScripts[playerObj] == GameNetworkManager.Instance.localPlayerController)
		{
			return;
		}
        if (playersManager.allPlayerScripts[playerObj].currentlyHeldObjectServer.radarIcon != null)
        {
            UnityEngine.Object.Destroy(playersManager.allPlayerScripts[playerObj].currentlyHeldObjectServer.radarIcon.gameObject);
        }
        if (playersManager.allPlayerScripts[playerObj].currentlyHeldObjectServer.gameObject.GetComponentInChildren<ScanNodeProperties>().gameObject != null)
        {
            UnityEngine.Object.Destroy(playersManager.allPlayerScripts[playerObj].currentlyHeldObjectServer.gameObject.GetComponentInChildren<ScanNodeProperties>().gameObject);
        }
        AudioSource[] audioPlayers = playersManager.allPlayerScripts[playerObj].currentlyHeldObjectServer.GetComponentsInChildren<AudioSource>();
        if (audioPlayers != null)
        {
            foreach (AudioSource audioPlayer in audioPlayers)
            {
                audioPlayer.Stop();
            }
        }
        AudioSource newAudioPlayer = apparatusContainer.GetComponent<AudioSource>();
		if (newAudioPlayer != null)
		{
            newAudioPlayer.Play();
        }
		else
		{
			Wither.Logger.LogError("Null audio player!");
		}
    }

    [ServerRpc(RequireOwnership = false)]
    public void ParentObjectToSlotServerRpc(NetworkObjectReference grabbableObjectNetObject)
    {
        if (grabbableObjectNetObject.TryGet(out networkedApp))
        {
			ParentObjectToSlotClientRpc(grabbableObjectNetObject);
        }
        else
        {
            Wither.Logger.LogError("ServerRpc: Could not find networked apparatus.");
        }
    }

    [ClientRpc]
    public void ParentObjectToSlotClientRpc(NetworkObjectReference grabbableObjectNetObject)
    {
        {
            if (grabbableObjectNetObject.TryGet(out networkedApp))
            {
                networkedApp.gameObject.GetComponentInChildren<GrabbableObject>().EnablePhysics(enable: false);
				networkedApp.gameObject.GetComponentInChildren<GrabbableObject>().grabbable = false;
				//networkedApp.gameObject.GetComponentInChildren<GrabbableObject>().itemProperties.rotationOffset += new UnityEngine.Vector3(0f, 90f, 0f);
            }
            else
            {
                Wither.Logger.LogError("ClientRpc: Could not find networked apparatus.");
            }
        }
    }
}
