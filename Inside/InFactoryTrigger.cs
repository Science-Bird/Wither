using GameNetcodeStuff;
using Unity.Netcode;
using UnityEngine;

namespace Wither.Inside;
public class InFactoryTrigger : NetworkBehaviour
{
    public static bool isInFalseInterior = false;

    public StartOfRound playersManager;

    private void Awake()
    {
        isInFalseInterior = false;
        playersManager = FindObjectOfType<StartOfRound>();
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
        if (GameNetworkManager.Instance.localPlayerController == enteringPlayer)
        {
            isInFalseInterior = true;
        }
        enteringPlayer.isInsideFactory = true;
    }

}
