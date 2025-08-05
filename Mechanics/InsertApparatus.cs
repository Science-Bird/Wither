using GameNetcodeStuff;
using Unity.Netcode;
using UnityEngine;
using Wither.Patches;
using Wither.Events;

namespace Wither.Mechanics;
public class InsertApparatus : NetworkBehaviour
{
    public StartOfRound playersManager;

    public InteractTrigger insertTrigger;

    public AnimatedObjectTrigger insertionAnimator;

    public static bool isInserted;
    public static bool doingInsertion = false;

    public NetworkObject apparatusContainer;
    private NetworkObject networkedApp;

    private bool initialSet = true;

    public static GrabbableObject? insertedApparatus;

    private void Update()
    {
        if (initialSet)
        {
            ApparatusRotationPatch.posSet = true;
            ApparatusRotationPatch.initialSet = true;
            playersManager = FindObjectOfType<StartOfRound>();
            insertedApparatus = null;
            doingInsertion = false;
            isInserted = false;
            initialSet = false;
        }
        if (!isInserted)// change apparatus slot hover tip depending on whether an apparatus is being held or not
        {
            if (GameNetworkManager.Instance.localPlayerController.currentlyHeldObjectServer != null && GameNetworkManager.Instance.localPlayerController.currentlyHeldObjectServer.itemProperties != null && IsApparatus(GameNetworkManager.Instance.localPlayerController.currentlyHeldObjectServer.itemProperties.itemName))
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
        else if (insertedApparatus == null)// fetch and store the apparatus grabbable object that's been inserted
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

    public static bool IsApparatus(string name, bool onlyDying = false)
    {
        return ((name.Contains("Apparatus") || name.Contains("apparatus")) && !name.Contains("concept") && !onlyDying) || name == "Dying apparatus";
    }

    public void InsertItem()// this only runs for the local player inserting it
    {
        PlayerControllerB playerInserting = GameNetworkManager.Instance.localPlayerController;
        if (playerInserting.currentlyHeldObjectServer != null && IsApparatus(playerInserting.currentlyHeldObjectServer.itemProperties.itemName, Wither.DyingApparatusOnly.Value) && !playerInserting.isGrabbingObjectAnimation)
        {
            insertionAnimator.TriggerAnimation(playerInserting);
            AnimatedLightsManager.Instance.PowerOn(true);// turn on interior lights if they're off in prep for alarm (they often will be if apparatus was taken on the same day)
            if (playerInserting.currentlyHeldObjectServer.radarIcon != null)
            {
                Destroy(playerInserting.currentlyHeldObjectServer.radarIcon.gameObject);// Destroy radar icon
            }
            if (playerInserting.currentlyHeldObjectServer.gameObject.GetComponentInChildren<ScanNodeProperties>() != null)
            {
                Destroy(playerInserting.currentlyHeldObjectServer.gameObject.GetComponentInChildren<ScanNodeProperties>().gameObject);// Destroy scan node
            }
            AudioSource[] audioPlayers = playerInserting.currentlyHeldObjectServer.GetComponentsInChildren<AudioSource>();
            if (audioPlayers != null)
            {
                foreach (AudioSource audioPlayer in audioPlayers)// if the apparatus has any inherent audio playing, stop it
                {
                    audioPlayer.Stop();
                }
            }

            // A bunch of messy stuff has to be done to properly discard an item on all clients
            DestroyChildObjectsServerRpc((int)playerInserting.playerClientId);
            ParentObjectToSlotServerRpc(playerInserting.currentlyHeldObjectServer.gameObject.GetComponent<NetworkObject>());
            playerInserting.DiscardHeldObject(placeObject: true, apparatusContainer, new UnityEngine.Vector3(0f, 0f, 0f), matchRotationOfParent: false);
            AudioSource newAudioPlayer = apparatusContainer.GetComponent<AudioSource>();
            if (newAudioPlayer != null)
            {
                newAudioPlayer.Play();// play apparatus hum while inserted
            }
            else
            {
                Wither.Logger.LogError("Null audio player!");
            }
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
            Destroy(playersManager.allPlayerScripts[playerObj].currentlyHeldObjectServer.radarIcon.gameObject);// Destroy radar icon
        }
        if (playersManager.allPlayerScripts[playerObj].currentlyHeldObjectServer.gameObject.GetComponentInChildren<ScanNodeProperties>().gameObject != null)
        {
            Destroy(playersManager.allPlayerScripts[playerObj].currentlyHeldObjectServer.gameObject.GetComponentInChildren<ScanNodeProperties>().gameObject);// Destroy scan node
        }
        AudioSource[] audioPlayers = playersManager.allPlayerScripts[playerObj].currentlyHeldObjectServer.GetComponentsInChildren<AudioSource>();
        if (audioPlayers != null)
        {
            foreach (AudioSource audioPlayer in audioPlayers)// if the apparatus has any inherent audio playing, stop it
            {
                audioPlayer.Stop();
            }
        }
        AudioSource newAudioPlayer = apparatusContainer.GetComponent<AudioSource>();
        if (newAudioPlayer != null)
        {
            newAudioPlayer.Play();// play apparatus hum while inserted
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
            Wither.Logger.LogDebug($"Setting apparatus value to {networkedApp.gameObject.GetComponentInChildren<GrabbableObject>().scrapValue}");
            ItemDispenser.apparatusValue = networkedApp.gameObject.GetComponentInChildren<GrabbableObject>().scrapValue;// tell item dispenser how much this apparatus is worth
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
            }
            else
            {
                Wither.Logger.LogError("ClientRpc: Could not find networked apparatus.");
            }
        }
    }
}
