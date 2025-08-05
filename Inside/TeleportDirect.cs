using Unity.Netcode;
using UnityEngine;

namespace Wither.Inside;
public class TeleportDirect : NetworkBehaviour
{
    public Transform destPoint;

    public static StartOfRound playersManager;

    public int audioReverbPreset = -1;

    public AudioSource entrancePointAudio;

    public AudioSource destPointAudio;

    public AudioClip[] doorAudios;

    private InteractTrigger triggerScript;

    public bool isEntrance = false;

    private void Awake()
    {
        playersManager = FindObjectOfType<StartOfRound>();
        triggerScript = gameObject.GetComponent<InteractTrigger>();
    }

    public void TeleportPlayer()// mostly copy-paste of vanilla EntranceTeleport code (same for other methods)
    {
        Transform thisPlayerBody = GameNetworkManager.Instance.localPlayerController.thisPlayerBody;
        GameNetworkManager.Instance.localPlayerController.TeleportPlayer(destPoint.position);
        GameNetworkManager.Instance.localPlayerController.isInElevator = false;
        GameNetworkManager.Instance.localPlayerController.isInHangarShipRoom = false;
        thisPlayerBody.eulerAngles = new Vector3(destPoint.eulerAngles.x, destPoint.eulerAngles.y, destPoint.eulerAngles.z);
        SetAudioPreset((int)GameNetworkManager.Instance.localPlayerController.playerClientId);
        for (int i = 0; i < GameNetworkManager.Instance.localPlayerController.ItemSlots.Length; i++)
        {
            if (GameNetworkManager.Instance.localPlayerController.ItemSlots[i] != null)
            {
                GameNetworkManager.Instance.localPlayerController.ItemSlots[i].isInFactory = isEntrance;
            }
        }
        InFactoryTrigger.isInFalseInterior = isEntrance;
        TeleportDirectPlayerServerRpc((int)GameNetworkManager.Instance.localPlayerController.playerClientId);

        GameNetworkManager.Instance.localPlayerController.isInsideFactory = isEntrance;

        if (Wither.mrovWeatherPresent)// special mrov method which is supposed to run on teleports
        {
            SetWeatherMrov.EnableWeather();
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void TeleportDirectPlayerServerRpc(int playerObj)
    {
        TeleportDirectPlayerClientRpc(playerObj);
    }

    [ClientRpc]
    public void TeleportDirectPlayerClientRpc(int playerObj)
    {
        if (playersManager.allPlayerScripts[playerObj] == GameNetworkManager.Instance.localPlayerController)
        {
            return;
        }
        playersManager.allPlayerScripts[playerObj].TeleportPlayer(destPoint.position, withRotation: true, destPoint.eulerAngles.y);
        playersManager.allPlayerScripts[playerObj].isInElevator = false;
        playersManager.allPlayerScripts[playerObj].isInHangarShipRoom = false;
        PlayAudioAtTeleportPositions();
        playersManager.allPlayerScripts[playerObj].isInsideFactory = isEntrance;
        if (isEntrance && !InFactoryTrigger.playersInFalseInterior.Contains(playerObj))
        {
            //Wither.Logger.LogDebug($"ADDING TO LIST: {playerObj}");
            InFactoryTrigger.playersInFalseInterior.Add(playerObj);
        }
        for (int i = 0; i < playersManager.allPlayerScripts[playerObj].ItemSlots.Length; i++)
        {
            if (playersManager.allPlayerScripts[playerObj].ItemSlots[i] != null)
            {
                playersManager.allPlayerScripts[playerObj].ItemSlots[i].isInFactory = isEntrance;
            }
        }
        if (GameNetworkManager.Instance.localPlayerController.isPlayerDead && playersManager.allPlayerScripts[playerObj] == GameNetworkManager.Instance.localPlayerController.spectatedPlayerScript)
        {
            SetAudioPreset(playerObj);
        }
    }
    private void SetAudioPreset(int playerObj)
    {
        if (audioReverbPreset != -1)
        {
            FindObjectOfType<AudioReverbPresets>().audioPresets[audioReverbPreset].ChangeAudioReverbForPlayer(StartOfRound.Instance.allPlayerScripts[playerObj]);
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
            entrancePointAudio.PlayOneShot(doorAudios[UnityEngine.Random.Range(0, doorAudios.Length)]);
            destPointAudio.PlayOneShot(doorAudios[UnityEngine.Random.Range(0, doorAudios.Length)]);
        }
    }
}
