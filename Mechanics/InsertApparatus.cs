using GameNetcodeStuff;
using System.Collections;
using System.Numerics;
using Unity.Netcode;
using UnityEngine;
using Wither.Patches;
using System;
using System.Reflection;

namespace Wither.Mechanics;
public class InsertApparatus : NetworkBehaviour
{
    public StartOfRound playersManager;

    public AnimatedObjectTrigger objectsEnableTrigger;

    public InteractTrigger insertTrigger;

    public AnimatedObjectTrigger animatedDoorTrigger;

    public static bool isInserted;

    public NetworkObject apparatusContainer;

    public static bool doingInsertion = false;

    private bool initialSet = true;

    private NetworkObject networkedApp;

    public static GrabbableObject? insertedApparatus;

    public static bool loafPresent = false;

    private void Update()
    {
        if (initialSet)
        {
            insertedApparatus = null;
            ApparatusRotationPatch.posSet = true;
            ApparatusRotationPatch.initialSet = true;
            playersManager = FindObjectOfType<StartOfRound>();
            doingInsertion = false;
            isInserted = false;
            playersManager = FindObjectOfType<StartOfRound>();
            loafPresent = false;
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (assembly.GetName().Name == "FacilityMeltdown")
                {
                    Wither.Logger.LogDebug("Found loaf!");
                    loafPresent = true;
                    break;
                }
            }
            initialSet = false;
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
        else if (insertedApparatus == null)
        {
            GameObject parent = GameObject.Find("WitherLungPosition");
            if (parent != null)
            {
                GrabbableObject child = parent.GetComponentInChildren<GrabbableObject>();
                if (child != null)
                {
                    insertedApparatus = child;
                }
            }
        }
    }

    public void InsertItem()
    {
        PlayerControllerB playerInserting = GameNetworkManager.Instance.localPlayerController;
        if (playerInserting.currentlyHeldObjectServer != null && ((playerInserting.currentlyHeldObjectServer.itemProperties.itemName.Contains("Apparatus") || playerInserting.currentlyHeldObjectServer.itemProperties.itemName.Contains("apparatus")) && !playerInserting.currentlyHeldObjectServer.itemProperties.itemName.Contains("concept") && !Wither.DyingApparatusOnly.Value || playerInserting.currentlyHeldObjectServer.itemProperties.itemName == "Dying apparatus") && !playerInserting.isGrabbingObjectAnimation)
        {
            if (playerInserting.currentlyHeldObjectServer.radarIcon != null)
            {
                Destroy(playerInserting.currentlyHeldObjectServer.radarIcon.gameObject);
            }
            if (playerInserting.currentlyHeldObjectServer.gameObject.GetComponentInChildren<ScanNodeProperties>() != null)
            {
                Destroy(playerInserting.currentlyHeldObjectServer.gameObject.GetComponentInChildren<ScanNodeProperties>().gameObject);
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
            Destroy(playersManager.allPlayerScripts[playerObj].currentlyHeldObjectServer.radarIcon.gameObject);
        }
        if (playersManager.allPlayerScripts[playerObj].currentlyHeldObjectServer.gameObject.GetComponentInChildren<ScanNodeProperties>().gameObject != null)
        {
            Destroy(playersManager.allPlayerScripts[playerObj].currentlyHeldObjectServer.gameObject.GetComponentInChildren<ScanNodeProperties>().gameObject);
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
                isInserted = true;
                networkedApp.gameObject.GetComponentInChildren<GrabbableObject>().EnablePhysics(enable: false);
                networkedApp.gameObject.GetComponentInChildren<GrabbableObject>().grabbable = false;
                Wither.Logger.LogDebug($"Setting apparatus value to {networkedApp.gameObject.GetComponentInChildren<GrabbableObject>().scrapValue}");
                PropTP.apparatusValue = networkedApp.gameObject.GetComponentInChildren<GrabbableObject>().scrapValue;
            }
            else
            {
                Wither.Logger.LogError("ClientRpc: Could not find networked apparatus.");
            }
        }
    }
}
