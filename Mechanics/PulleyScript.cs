using System.Collections;
using System.Collections.Generic;
using GameNetcodeStuff;
using Unity.Netcode;
using UnityEngine;

namespace Wither.Mechanics;
public class PulleyScript : NetworkBehaviour
{
    public Animator pulleyAnimator;
    public AudioClip wheelSound;
    public AudioSource wheelAudio;
    private bool sendingRPC = false;

    // most of the pulley logic is in the animator itself, so this just makes sure the state of the pulley is set on all clients when the wheel is interacted with

    public void TurnWheel()
    {
        SetAnimBool(true);
        sendingRPC = true;
        SetAnimBoolServerRpc(true);
    }

    public void StartPulling()
    {
        SetAnimBool(true);
    }

    public void StopPulling()
    {
        SetAnimBool(false);
    }

    public void OffGround()
    {
        pulleyAnimator.SetBool("grounded", false);
    }

    public void OnGround()
    {
        pulleyAnimator.SetBool("grounded", true);
    }

    [ServerRpc(RequireOwnership = false)]
    private void SetAnimBoolServerRpc(bool set = false)
    {
        SetAnimBoolClientRpc(set);
    }

    [ClientRpc]
    private void SetAnimBoolClientRpc(bool set)
    {
        if (sendingRPC)
        {
            sendingRPC = false;
        }
        else
        {
            SetAnimBool(set);
        }
    }

    private void SetAnimBool(bool set = false)
    {
        pulleyAnimator.SetBool("pulling", set);
        if (set)
        {
            wheelAudio.PlayOneShot(wheelSound);
            WalkieTalkie.TransmitOneShotAudio(wheelAudio, wheelSound);
        }
    }
}
