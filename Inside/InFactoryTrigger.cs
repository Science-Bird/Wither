using GameNetcodeStuff;
using Unity.Netcode;
using UnityEngine;
using System.Collections.Generic;

namespace Wither.Inside;
public class InFactoryTrigger : NetworkBehaviour
{
    public static List<int> playersInFalseInterior = new List<int>();
    public static bool isInFalseInterior = false;

    // this is a weird class, it's mainly a behaviour which sets players into the false interior upon entering a trigger (needed for false interior emergency exit, my custom teleport doors will do this themselves)
    // but, I also store some static information here about who's in the interior or not (each client needs to know which players are in the false interior for radar purposes)

    private void Awake()
    {
        isInFalseInterior = false;
        playersInFalseInterior.Clear();
    }

    private void OnTriggerEnter(Collider other)
    {
        PlayerControllerB enteringPlayer = other.gameObject.GetComponent<PlayerControllerB>();
        if (enteringPlayer == null)
        {
            return;
        }

        enteringPlayer.isInElevator = false;
        enteringPlayer.isInHangarShipRoom = false;
        for (int i = 0; i < enteringPlayer.ItemSlots.Length; i++)
        {
            if (enteringPlayer.ItemSlots[i] != null)
            {
                enteringPlayer.ItemSlots[i].isInFactory = true;
            }
        }
        if (GameNetworkManager.Instance.localPlayerController == enteringPlayer)// if player is self
        {
            isInFalseInterior = true;
        }
        else// if another player, add them to list
        {
            int playerIndex = (int)enteringPlayer.playerClientId;
            if (playerIndex >= 0 && !playersInFalseInterior.Contains(playerIndex))
            {
                //Wither.Logger.LogDebug($"ADDING TO LIST: {playerIndex}");
                playersInFalseInterior.Add(playerIndex);
            }
        }
        enteringPlayer.isInsideFactory = true;
    }

}
