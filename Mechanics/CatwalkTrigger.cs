using GameNetcodeStuff;
using Unity.Netcode;
using UnityEngine;

namespace Wither.Mechanics;
public class CatwalkTrigger : NetworkBehaviour
{
    private int timesTriggered = 0;

    private int triggerThreshold;

    public AnimatedObjectTrigger animatedObjectTriggerShake;

    public AnimatedObjectTrigger animatedObjectTriggerFall;

    private bool initialSet = true;

    private bool bridgeFell;

    private void Update()
    {
        if (initialSet)
        {
            if (IsServer)
            {
                triggerThreshold = Random.Range(2, Wither.MaxCatwalkTriggers.Value + 1);
                Wither.Logger.LogDebug($"Catwalk triggers: {triggerThreshold}");
                SetThresholdClientRpc(triggerThreshold);
            }
            initialSet = false;
        }
    }

    [ClientRpc]
    public void SetThresholdClientRpc(int threshold)
    {
        if (!IsServer)
        {
            triggerThreshold = threshold;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!bridgeFell)
        {
            PlayerControllerB component = other.gameObject.GetComponent<PlayerControllerB>();
            if (component != null && GameNetworkManager.Instance.localPlayerController == component)
            {
                AddToBridgeInstabilityServerRpc();
            }
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void AddToBridgeInstabilityServerRpc()
    {
        {
            timesTriggered++;
            if (timesTriggered < triggerThreshold)
            {
                animatedObjectTriggerShake.TriggerAnimation(GameNetworkManager.Instance.localPlayerController);
            }
            if (timesTriggered >= triggerThreshold)
            {
                bridgeFell = true;
                animatedObjectTriggerFall.TriggerAnimation(GameNetworkManager.Instance.localPlayerController);
            }
        }
    }
}
