using System.Collections;
using System.Collections.Generic;
using GameNetcodeStuff;
using Unity.Netcode;
using UnityEngine;

namespace Wither.Mechanics;
public class WitheredLasher : NetworkBehaviour
{
    public Animator mainAnimator;
    public Animator[] groupAnimators;

    public bool groupAnimation = false;// multiple lashers/objects all governed by one controller (only used for the grate lashers in the fan room)
    public bool linkedAnimation = false;// one lasher and some objects governed by one controller (used for lashers which interact with objects like vents or signs)

    public bool grabber = true;// whether a lasher grabs player corpses

    public AudioSource tentacleSFX;
    public AudioSource slitherSFX;

    public AudioClip[] damagedClips;
    public AudioClip hitGroundClip;
    public AudioClip spawnRoarClip;
    public AudioClip hitPlayerClip;

    private bool sendingRPC1 = false;
    private bool sendingRPC2 = false;

    public Transform tentacleGrabPoint;
    public Transform[] tentacleGrabPointArray;
    private DeadBodyInfo currentlyHeldBody;

    public float speedOverride = -1f;// override the speed of lasher spawn/death animation

    public bool doingCollisions = false;// main bool for handling collisions, generally controlled by animator
    private bool stopCollisions = false;// override bool to stop collisions internally (since we can't easily override doingCollisions if it's animator controlled)

    public bool ignoreLocalPlayer = false;// used for invulnerability

    public void SpawnLasher()
    {
        if (!gameObject.GetComponent<NetworkObject>().IsSpawned && base.IsServer)
        {
            GetComponent<NetworkObject>().Spawn();
        }
        slitherSFX.Play();
        if (groupAnimation || linkedAnimation)
        {
            if (groupAnimation)// if there are multiple lashers, we need to enable/disable collisions manually (since this script object won't be in any of the individual animators)
            {
                doingCollisions = true;
            }
            foreach (Animator animator in groupAnimators)// set all animators in the group
            {
                //Wither.Logger.LogDebug($"LINKED ANIMATOR: {animator.name} {Time.realtimeSinceStartup}");
                animator.SetTrigger("start");
                if (speedOverride > 0f)
                {
                    animator.speed = speedOverride;
                }
            }
        }
        else
        {
            mainAnimator.SetTrigger("start");
            if (speedOverride > 0f)
            {
                mainAnimator.speed = speedOverride;
            }
        }
    }

    public void KillLasher()
    {
        if (!doingCollisions || stopCollisions) { return; }

        doingCollisions = false;
        stopCollisions = true;// set override so even if KillLasher gets called again, we won't run this anymore
        int random = Random.Range(0, damagedClips.Length);
        tentacleSFX.PlayOneShot(damagedClips[random]);
        WalkieTalkie.TransmitOneShotAudio(tentacleSFX, damagedClips[random]);
        slitherSFX.Stop();
        if (groupAnimation || linkedAnimation)
        {
            if (groupAnimation)
            {
                doingCollisions = false;
            }
            foreach (Animator animator in groupAnimators)
            {
                animator.SetTrigger("kill");
                if (speedOverride > 0f)
                {
                    animator.speed = 1f;
                }
            }
        }
        else
        {
            mainAnimator.SetTrigger("kill");
            if (speedOverride > 0f)
            {
                mainAnimator.speed = 1f;
            }
        }
        ScanNodeProperties[] componentsInChildren = base.gameObject.GetComponentsInChildren<ScanNodeProperties>();
        foreach (ScanNodeProperties component in componentsInChildren)
        {
            component.gameObject.SetActive(false);
        }
    }

    public void KillLasherLocal()
    {
        KillLasher();
        sendingRPC1 = true;
        KillLasherServerRpc();
    }

    [ServerRpc(RequireOwnership = false)]
    public void KillLasherServerRpc()
    {
        KillLasherClientRpc();
    }

    [ClientRpc]
    public void KillLasherClientRpc()
    {
        if (sendingRPC1)
        {
            sendingRPC1 = false;
        }
        else
        {
            KillLasher();
        }
    }

    public void DeathFlopAudio()
    {
        tentacleSFX.PlayOneShot(hitGroundClip);
        WalkieTalkie.TransmitOneShotAudio(tentacleSFX, hitGroundClip);
    }

    public void SpawnAudio()
    {
        tentacleSFX.PlayOneShot(spawnRoarClip);
        WalkieTalkie.TransmitOneShotAudio(tentacleSFX, spawnRoarClip);
    }

    public void GrabPlayerCorpseLocal(int playerID, int index)
    {
        sendingRPC2 = true;
        StartCoroutine(GrabPlayerCorpse(playerID, index));
        GrabPlayerCorpseServerRpc(playerID, index);
    }

    [ServerRpc(RequireOwnership = false)]
    public void GrabPlayerCorpseServerRpc(int playerID, int index)
    {
        GrabPlayerCorpseClientRpc(playerID, index);
    }

    [ClientRpc]
    public void GrabPlayerCorpseClientRpc(int playerID, int index)
    {
        if (sendingRPC2)
        {
            sendingRPC2 = false;
        }
        else
        {
            StartCoroutine(GrabPlayerCorpse(playerID, index));
        }
    }

    private IEnumerator GrabPlayerCorpse(int playerID, int index)// partially taken from company monster
    {
        //Wither.Logger.LogDebug($"PLAYER GRABBING {index}");
        PlayerControllerB playerDying = StartOfRound.Instance.allPlayerScripts[playerID];
        float startTime = Time.timeSinceLevelLoad;
        yield return new WaitUntil(() => playerDying.deadBody != null || Time.timeSinceLevelLoad - startTime > 4f);
        if (playerDying.deadBody != null)
        {
            if (groupAnimation && index != -1)// if group animation, determine which lasher killed the player
            {
                playerDying.deadBody.attachedTo = tentacleGrabPointArray[index];
            }
            else
            {
                playerDying.deadBody.attachedTo = tentacleGrabPoint;
            }
            playerDying.deadBody.attachedLimb = playerDying.deadBody.bodyParts[6];
            playerDying.deadBody.matchPositionExactly = true;
            currentlyHeldBody = playerDying.deadBody;
            if (groupAnimation)// if player is killed by lashers under the grate, the lashers retreat with the body
            {
                KillLasher();
            }
        }
        else
        {
            Wither.Logger.LogInfo("Player body was not spawned in time for animation.");
        }
    }

    public void AnimationEvent_ReleaseCorpse()// let go of any held body at certain point in lasher death animation
    {
        if (currentlyHeldBody != null)
        {
            currentlyHeldBody.attachedTo = null;
            currentlyHeldBody.attachedLimb = null;
            currentlyHeldBody.matchPositionExactly = false;
        }
    }
}
