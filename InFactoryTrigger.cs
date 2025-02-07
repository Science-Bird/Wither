using GameNetcodeStuff;
using Unity.Netcode;
using UnityEngine;

namespace Wither;
public class InFactoryTrigger : NetworkBehaviour
{

    public StartOfRound playersManager;

    private void Awake()
    {
        playersManager = Object.FindObjectOfType<StartOfRound>();
    }

    private void OnTriggerEnter(Collider other)
	{
        GameNetworkManager.Instance.localPlayerController.isInElevator = false;
        GameNetworkManager.Instance.localPlayerController.isInHangarShipRoom = false;
        SetPlayerServerRpc((int)GameNetworkManager.Instance.localPlayerController.playerClientId);
        GameNetworkManager.Instance.localPlayerController.isInsideFactory = true;
    }

    [ServerRpc(RequireOwnership = false)]
    public void SetPlayerServerRpc(int playerObj)
    {
        SetPlayerClientRpc(playerObj);
    }

    [ClientRpc]
    public void SetPlayerClientRpc(int playerObj)
    {
        if (playersManager.allPlayerScripts[playerObj] == GameNetworkManager.Instance.localPlayerController)
        {
            return;
        }
        playersManager.allPlayerScripts[playerObj].isInElevator = false;
        playersManager.allPlayerScripts[playerObj].isInHangarShipRoom = false;
        playersManager.allPlayerScripts[playerObj].isInsideFactory = true;
    }
}
