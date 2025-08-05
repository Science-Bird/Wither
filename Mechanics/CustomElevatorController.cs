using GameNetcodeStuff;
using Unity.Netcode;
using UnityEngine;

namespace Wither.Mechanics;
public class CustomElevatorController : NetworkBehaviour
{
    public Animator elevatorAnimator;

    public Transform elevatorPoint;

    public float elevatorFinishTimer;
    public float callCooldown;

    public bool elevatorFinishedMoving;
    public bool elevatorIsAtBottom;
    public bool elevatorCalled;
    public bool elevatorMovingDown;
    private bool movingDownLastFrame = true;
    public bool calledDown;

    public AudioSource elevatorAudio;
    public AudioSource elevatorAudioTop;
    public AudioSource elevatorJingleMusic;

    public AudioClip elevatorStartUpSFX;
    public AudioClip elevatorStartDownSFX;
    public AudioClip elevatorTravelSFX;
    public AudioClip elevatorFinishUpSFX;
    public AudioClip elevatorFinishDownSFX;
    public AudioClip elevatorCrashSFX;
    public AudioClip elevatorSmashSFX;

    public AudioClip[] elevatorJingleClips;
    public AudioClip[] elevatorJingleClipsLong;

    public Transform elevatorTopPoint;
    public Transform elevatorBottomPoint;
    public Transform elevatorInsidePoint;

    public Collider crashDamageTrigger;

    public Vector3 previousElevatorPosition;

    public bool elevatorDoorOpen;
    private bool playMusic;
    private bool startedMusic;

    private float stopPlayingMusicTimer;

    private int selectedTrack = 0;

    public static bool permanentlyDisabled = false;

    // clone of mineshaft elevator which uses Halloween clip system, fixes some minor bugs, and adds some special events for the elevator crash sequence

    public void Start()
    {
        permanentlyDisabled = false;
    }

    [ServerRpc]
    public void SetElevatorMusicServerRpc(bool setOn, int track)
    {
        {
            SetElevatorMusicClientRpc(setOn, track);
        }
    }
    [ClientRpc]
    public void SetElevatorMusicClientRpc(bool setOn, int track)
    {
        if (!IsServer)
        {
            selectedTrack = track;
            playMusic = setOn;
        }
    }

    public void LateUpdate()
    {
        previousElevatorPosition = elevatorInsidePoint.position;
    }

    public void Update()
    {
        if (!playMusic)
        {
            if (stopPlayingMusicTimer <= 0f)
            {
                if (elevatorJingleMusic.isPlaying)
                {
                    if (elevatorJingleMusic.pitch < 0.5f)
                    {
                        elevatorJingleMusic.volume -= Time.deltaTime * 3f;
                        if (elevatorJingleMusic.volume <= 0.01f)
                        {
                            elevatorJingleMusic.Stop();
                        }
                    }
                    else
                    {
                        elevatorJingleMusic.pitch -= Time.deltaTime;
                        elevatorJingleMusic.volume = Mathf.Max(elevatorJingleMusic.volume - Time.deltaTime * 2f, 0.4f);
                    }
                }
            }
            else
            {
                stopPlayingMusicTimer -= Time.deltaTime;
            }
        }
        else
        {
            stopPlayingMusicTimer = 1.5f;
            if (!elevatorJingleMusic.isPlaying)
            {
                //Wither.Logger.LogDebug($"Elevator playing track {selectedTrack}");
                if (elevatorMovingDown)
                {
                    elevatorJingleMusic.clip = elevatorJingleClips[selectedTrack];
                    elevatorJingleMusic.Play();
                    elevatorJingleMusic.volume = 1f;
                }
                else
                {
                    elevatorJingleMusic.clip = elevatorJingleClipsLong[selectedTrack];
                    elevatorJingleMusic.Play();
                    elevatorJingleMusic.volume = 1f;
                }
            }
            elevatorJingleMusic.pitch = Mathf.Clamp(elevatorJingleMusic.pitch += Time.deltaTime * 2f, 0.3f, 1f);
        }
        elevatorAnimator.SetBool("ElevatorGoingUp", !elevatorMovingDown);
        if (elevatorMovingDown != movingDownLastFrame)
        {
            movingDownLastFrame = elevatorMovingDown;
            if (elevatorMovingDown)
            {
                elevatorAudio.PlayOneShot(elevatorStartDownSFX);
            }
            else
            {
                elevatorAudio.PlayOneShot(elevatorStartUpSFX);
            }
            if (IsServer)
            {
                SetElevatorMovingServerRpc(elevatorMovingDown);
            }
        }
        if (!IsServer)
        {
            return;
        }
        if (elevatorFinishedMoving)
        {
            if (IsServer && startedMusic)
            {
                playMusic = false;
                startedMusic = false;
                SetElevatorMusicServerRpc(setOn: false, selectedTrack);
            }
        }
        else if (IsServer && !startedMusic)
        {
            startedMusic = true;
            playMusic = true;
            selectedTrack = Random.Range(0, elevatorJingleClips.Length);
            SetElevatorMusicServerRpc(setOn: true, selectedTrack);
        }
        if (elevatorFinishedMoving)
        {
            if (elevatorCalled)
            {
                if (callCooldown <= 0f)
                {
                    SwitchElevatorDirection();
                    SetElevatorCalledClientRpc(setCalled: false, elevatorMovingDown);
                }
                else
                {
                    callCooldown -= Time.deltaTime;
                }
            }
        }
        else if (elevatorFinishTimer <= 0f)
        {
            elevatorFinishedMoving = true;
            //Wither.Logger.LogDebug("Elevator finished moving!");
            PlayFinishAudio(!elevatorMovingDown);
            ElevatorFinishServerRpc(!elevatorMovingDown);
        }
        else
        {
            elevatorFinishTimer -= Time.deltaTime;
        }
    }

    private void SwitchElevatorDirection()
    {
        if (permanentlyDisabled) { return; }
        elevatorMovingDown = !elevatorMovingDown;
        elevatorFinishedMoving = false;
        elevatorFinishTimer = 14f;
        elevatorCalled = false;
        SetElevatorFinishedMovingClientRpc(finished: false);
    }

    [ClientRpc]
    public void SetElevatorFinishedMovingClientRpc(bool finished)
    {
        if (!IsServer)
        {
            elevatorFinishedMoving = finished;
        }
    }

    public void AnimationEvent_ElevatorFinishTop()
    {
        if (!elevatorMovingDown && !elevatorFinishedMoving)
        {
            elevatorFinishedMoving = true;
            if (IsServer)
            {
                PlayFinishAudio(!elevatorMovingDown);
                ElevatorFinishServerRpc(!elevatorMovingDown);
            }
        }
    }

    public void AnimationEvent_ElevatorStartFromBottom()
    {
        ShakePlayerCamera(shakeHard: false);
    }

    public void AnimationEvent_ElevatorHitBottom()
    {
        ShakePlayerCamera(shakeHard: true);
    }

    public void AnimationEvent_ElevatorTravel()
    {
        elevatorAudio.PlayOneShot(elevatorTravelSFX);
    }

    public void AnimationEvent_ElevatorFinishBottom()
    {
        if (elevatorMovingDown && !elevatorFinishedMoving)
        {
            elevatorFinishedMoving = true;
            if (IsServer)
            {
                //Wither.Logger.LogDebug("Elevator finished moving B!");
                PlayFinishAudio(!elevatorMovingDown);
                ElevatorFinishServerRpc(!elevatorMovingDown);
            }
        }
    }

    private void ShakePlayerCamera(bool shakeHard)
    {
        if (Vector3.Distance(StartOfRound.Instance.audioListener.transform.position, elevatorPoint.position) < 4f)
        {
            if (shakeHard)
            {
                HUDManager.Instance.ShakeCamera(ScreenShakeType.Big);
            }
            else
            {
                HUDManager.Instance.ShakeCamera(ScreenShakeType.Long);
            }
        }
    }

    [ServerRpc]
    public void ElevatorFinishServerRpc(bool atTop)
    {
        {
            ElevatorFinishClientRpc(atTop);
        }
    }
    [ClientRpc]
    public void ElevatorFinishClientRpc(bool atTop)
    {
        if (!IsServer)
        {
            PlayFinishAudio(atTop);
            elevatorFinishedMoving = true;
        }
    }
    private void PlayFinishAudio(bool atTop)
    {
        if (atTop)
        {
            elevatorAudio.PlayOneShot(elevatorFinishUpSFX);
        }
        else
        {
            elevatorAudio.PlayOneShot(elevatorFinishDownSFX);
        }
    }

    public void PlayCrashClip()
    {
        HUDManager.Instance.ShakeCamera(ScreenShakeType.VeryStrong);
        elevatorAudio.PlayOneShot(elevatorCrashSFX);
        permanentlyDisabled = true;
    }

    public void PlaySmashClip()
    {
        HUDManager.Instance.ShakeCamera(ScreenShakeType.VeryStrong);
        elevatorAudioTop.PlayOneShot(elevatorSmashSFX);
        permanentlyDisabled = true;
    }

    public void AnimationEvent_DoCrashDamage()// damage any players in the elevator (occurs during crash)
    {
        if (crashDamageTrigger.CompareTag("Player") && (bool)crashDamageTrigger.gameObject.GetComponent<PlayerControllerB>() && crashDamageTrigger.gameObject.GetComponent<PlayerControllerB>().IsOwner && !crashDamageTrigger.gameObject.GetComponent<PlayerControllerB>().isPlayerDead)
        {
            crashDamageTrigger.gameObject.GetComponent<PlayerControllerB>().DamagePlayer(30, causeOfDeath: CauseOfDeath.Inertia);
        }
        else if (crashDamageTrigger.bounds.Contains(GameNetworkManager.Instance.localPlayerController.transform.position))
        {
            GameNetworkManager.Instance.localPlayerController.DamagePlayer(30, causeOfDeath: CauseOfDeath.Inertia);
        }
    }

    [ServerRpc]
    public void SetElevatorMovingServerRpc(bool movingDown)
    {
        {
            SetElevatorMovingClientRpc(movingDown);
        }
    }
    [ClientRpc]
    public void SetElevatorMovingClientRpc(bool movingDown)
    {
        if (!IsServer)
        {
            elevatorMovingDown = movingDown;
        }
    }
    [ServerRpc(RequireOwnership = false)]
    public void CallElevatorServerRpc(bool callDown)
    {
        CallElevatorOnServer(callDown);
    }

    public void CallElevatorOnServer(bool callDown)
    {
        if (elevatorMovingDown != callDown)
        {
            elevatorCalled = true;
            callCooldown = 4f;
            SetElevatorCalledClientRpc(elevatorCalled, elevatorMovingDown);
        }
    }

    public void SetElevatorDoorOpen()
    {
        elevatorDoorOpen = true;
    }

    public void SetElevatorDoorClosed()
    {
        elevatorDoorOpen = false;
    }

    [ClientRpc]
    public void SetElevatorCalledClientRpc(bool setCalled, bool elevatorDown)
    {
        if (!IsServer)
        {
            elevatorCalled = setCalled;
            elevatorMovingDown = elevatorDown;
        }
    }
    public void CallElevator(bool callDown)
    {
        if (permanentlyDisabled) { return; }
        //Wither.Logger.LogDebug($"Call elevator 0; call down: {callDown}; elevator moving down: {elevatorMovingDown}");
        CallElevatorServerRpc(callDown);
    }

    [ServerRpc(RequireOwnership = false)]
    public void PressElevatorButtonServerRpc()
    {
        PressElevatorButtonOnServer();
    }

    public void PressElevatorButtonOnServer(bool requireFinishedMoving = false)
    {
        if ((elevatorFinishedMoving || (elevatorFinishTimer < 0.16f && !requireFinishedMoving)) && !elevatorJingleMusic.isPlaying)// added isPlaying check to fix vanilla issue with elevator music
        {
            SwitchElevatorDirection();
        }
    }

    public void PressElevatorButton()
    {
        if (permanentlyDisabled) { return; }
        PressElevatorButtonServerRpc();
    }
}
