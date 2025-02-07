using System.Collections;
using Unity.Netcode;
using UnityEngine;

namespace Wither;
public class TeleportDirect : NetworkBehaviour
{
	public Transform destPoint;

	public StartOfRound playersManager;

	public int audioReverbPreset = -1;

	public AudioSource entrancePointAudio;

	public AudioSource destPointAudio;

	public AudioClip[] doorAudios;

	private InteractTrigger triggerScript;

	public float timeAtLastUse;

	public bool isEntrance = false;

	private void Awake()
	{
		playersManager = Object.FindObjectOfType<StartOfRound>();
		triggerScript = base.gameObject.GetComponent<InteractTrigger>();
	}

	public void TeleportPlayer()
	{
        Transform thisPlayerBody = GameNetworkManager.Instance.localPlayerController.thisPlayerBody;
		GameNetworkManager.Instance.localPlayerController.TeleportPlayer(destPoint.position);
        GameNetworkManager.Instance.localPlayerController.isInElevator = false;
        GameNetworkManager.Instance.localPlayerController.isInHangarShipRoom = false;
        thisPlayerBody.eulerAngles = new Vector3(destPoint.eulerAngles.x, destPoint.eulerAngles.y, destPoint.eulerAngles.z);
		SetAudioPreset((int)GameNetworkManager.Instance.localPlayerController.playerClientId);
		timeAtLastUse = Time.realtimeSinceStartup;
		TeleportDirectPlayerServerRpc((int)GameNetworkManager.Instance.localPlayerController.playerClientId);
        GameNetworkManager.Instance.localPlayerController.isInsideFactory = isEntrance;
    }

	[ServerRpc(RequireOwnership = false)]
	public void TeleportDirectPlayerServerRpc(int playerObj)
			{
				TeleportDirectPlayerClientRpc(playerObj);
			}

	[ClientRpc]
	public void TeleportDirectPlayerClientRpc(int playerObj)
{if(playersManager.allPlayerScripts[playerObj] == GameNetworkManager.Instance.localPlayerController)		{
			return;
		}
		playersManager.allPlayerScripts[playerObj].TeleportPlayer(destPoint.position, withRotation: true, destPoint.eulerAngles.y);
        playersManager.allPlayerScripts[playerObj].isInElevator = false;
        playersManager.allPlayerScripts[playerObj].isInHangarShipRoom = false;
        PlayAudioAtTeleportPositions();
		playersManager.allPlayerScripts[playerObj].isInsideFactory = isEntrance;

		if (GameNetworkManager.Instance.localPlayerController.isPlayerDead && playersManager.allPlayerScripts[playerObj] == GameNetworkManager.Instance.localPlayerController.spectatedPlayerScript)
		{
			SetAudioPreset(playerObj);
		}
		timeAtLastUse = Time.realtimeSinceStartup;
}
	private void SetAudioPreset(int playerObj)
	{
		if (audioReverbPreset != -1)
		{
			Object.FindObjectOfType<AudioReverbPresets>().audioPresets[audioReverbPreset].ChangeAudioReverbForPlayer(StartOfRound.Instance.allPlayerScripts[playerObj]);
			if (entrancePointAudio != null)
			{
				PlayAudioAtTeleportPositions();
			}
		}
	}

	public void PlayAudioAtTeleportPositions()
	{
		if (doorAudios.Length != 0)
		{
			entrancePointAudio.PlayOneShot(doorAudios[Random.Range(0, doorAudios.Length)]);
			destPointAudio.PlayOneShot(doorAudios[Random.Range(0, doorAudios.Length)]);
		}
	}
}
