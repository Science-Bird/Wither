using System.Collections;
using Unity.Netcode;
using UnityEngine;
using Wither.Mechanics;
using Wither.Patches;

namespace Wither.Events;
public class LasherManager : NetworkBehaviour
{
    public WitheredLasher[] lashers;
    public bool sendingRPC;
    public GameObject tentacleContainer;
    private bool deactivated = false;

    public static LasherManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            UnityEngine.Object.Destroy(Instance.gameObject);
            Instance = this;
        }
    }

    public void InitialDeactivate()
    {
        if (!deactivated)
        {
            deactivated = true;
            tentacleContainer.SetActive(false);
        }
    }

    public void SpawnLashers()
    {
        deactivated = false;
        StartCoroutine(LasherSpawningCycle());
    }

    private IEnumerator LasherSpawningCycle()
    {
        foreach (WitheredLasher lasher in lashers)
        {
            yield return new WaitForSeconds(0.4f);
            lasher.SpawnLasher();
        }
    }

    public void KillLashers()
    {
        foreach (WitheredLasher lasher in lashers)
        {
            lasher.KillLasher();
        }
    }

    public void LasherEnemyScanLocal()
    {
        sendingRPC = true;
        LasherEnemyScan();
        LasherEnemyScanServerRpc();
    }

    [ServerRpc(RequireOwnership = false)]
    public void LasherEnemyScanServerRpc()
    {
        LasherEnemyScanClientRpc();
    }

    [ClientRpc]
    public void LasherEnemyScanClientRpc()
    {
        if (sendingRPC)
        {
            sendingRPC = false;
        }
        else
        {
            LasherEnemyScan();
        }
    }

    public void LasherEnemyScan()// simulate new creature scanned
    {
        TerminalEntryPatches.unlocked = true;
        HUDManager.Instance.DisplayGlobalNotification("New creature data sent to terminal!");
    }
}