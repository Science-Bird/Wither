using System.Collections;
using Unity.Netcode;
using UnityEngine;
using Wither.Inside;
using Wither.Mechanics;
using Wither.Patches;

namespace Wither.Events;
public class SpecialEventHandler : NetworkBehaviour
{
    public Transform noisePosition;

    public bool alarmActive = false;
    public int loopCounter = 16;

    public AudioSource hornPlayer;
    public AudioSource machinePlayer;
    public AudioSource buttonAudio;

    public AudioClip rumbleClip;
    public AudioClip dispenseClip;
    public AudioClip buttonSFX;

    [Space(10f)]

    public GameObject JLLSpawner;

    public Animator tentacleCutsceneAnimator;
    public Animator elevatorAnimator;
    public Animator buttonAnimator;
    public Animator hatchAnimator;

    [Space(10f)]

    public Animator spikesAnimator;
    public Animator steamFogAnimator;
    public ParticleSystem steamLeakParticles;
    public AudioSource steamSFX;
    public ParticleSystem waterDripParticles;
    public AudioSource waterSFX;

    public MeshRenderer thinVentMesh;
    public Material patchyGrateMat;

    [Space(10f)]

    public Animator[] breakerAnimators;
    public InteractTrigger[] breakerTriggers;
    public ScanNodeProperties[] breakerScanNodes;

    [Space(10f)]

    public Animator garageDoorAnimator;
    public AnimatedObjectTrigger garageDoorAnimTrigger;
    public AnimatedObjectTrigger garageLeverAnimTrigger;

    [Space(10f)]

    public AnimatedLightsManager lightsManager;
    public ItemDispenser dispenserScript;
    public LasherManager lasherManager;

    [Space(10f)]

    public GameObject topFloorObjects;
    public GameObject interiorScrapDrop;
    public GameObject interiorBreakerBox;
    public GameObject exteriorBreakerBox;

    private float loopTimer = 2f;

    private bool sendingRPC1 = false;
    private bool sendingRPC2 = false;

    public static SpecialEventHandler Instance { get; private set; }

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

    private void Update()
    {
        if (alarmActive)
        {
            LoopAudio();
        }
    }

    public void LoopAudio()
    {
        if (!hornPlayer.isPlaying)// when end of horn audio clip is reached
        {
            hornPlayer.Play();

            if (!GameNetworkManager.Instance.localPlayerController.isInsideFactory || InFactoryTrigger.isInFalseInterior)
            {
                StartCoroutine(AlarmPulse());// pulse lights with audio
                HUDManager.Instance.ShakeCamera(ScreenShakeType.Big);
                SoundManager.Instance.PlaySoundAroundLocalPlayer(rumbleClip, 0.65f);
            }

        }
        if (loopTimer <= 0f)// play noise for dogs
        {
            RoundManager.Instance.PlayAudibleNoise(noisePosition.position, 20f, 0.6f, 0, false);
            loopTimer = 2f;
        }
        else
        {
            loopTimer -= Time.deltaTime;
        }
    }

    [ClientRpc]
    public void InitializeEventClientRpc()
    {
        if (sendingRPC1)
        {
            sendingRPC1 = false;
        }
        else
        {
            InitializeEvent();
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void InitializeEventServerRpc()
    {
        InitializeEventClientRpc();
    }

    public void InitializeEventLocal()
    {
        InitializeEvent();
        sendingRPC1 = true;
        InitializeEventServerRpc();
    }

    public void InitializeEvent()
    {
        StartCoroutine(EventStartRoutine());
    }

    public IEnumerator EventStartRoutine()// main event routine
    {
        // button pressed
        buttonAnimator.SetTrigger("press");
        buttonAudio.PlayOneShot(buttonSFX);
        WalkieTalkie.TransmitOneShotAudio(buttonAudio, buttonSFX);
        CustomElevatorController.permanentlyDisabled = true;// disable elevator in prep for animation
        yield return new WaitForSeconds(0.1f);
        hatchAnimator.SetTrigger("openSmallHatch");
        machinePlayer.PlayOneShot(dispenseClip);
        WalkieTalkie.TransmitOneShotAudio(machinePlayer, dispenseClip);
        yield return new WaitForSeconds(3f);
        // spawn items
        if (base.IsServer)
        {
            dispenserScript.SpawnItemsServer();
        }
        yield return new WaitForSeconds(7f);
        // lasher "cutscene" starts
        lasherManager.tentacleContainer.SetActive(true);
        tentacleCutsceneAnimator.SetTrigger("start");
        elevatorAnimator.SetTrigger("crash");
        yield return new WaitForSeconds(2f);
        // lasher "cutscene" ends, alarm starts and other lashers spawn
        lasherManager.SpawnLashers();
        lightsManager.PulseAlarmOn();
        lightsManager.BeginAlarmSequence();
        // stop interior pipe water drip and replace it with steam (and add fog)
        waterDripParticles.Stop();
        waterSFX.Stop();
        ParticleSystem.MainModule main = steamLeakParticles.main;
        main.loop = true;
        steamLeakParticles.Play();
        steamSFX.Play();
        steamFogAnimator.SetTrigger("enableFog");
        // set patchy material for vents lashers are poking through
        thinVentMesh.material = patchyGrateMat;
        foreach (Animator animator in breakerAnimators)
        {
            animator.SetBool("on", true);
        }
        foreach (ScanNodeProperties node in breakerScanNodes)
        {
            node.subText = "Armed";
        }
        foreach (InteractTrigger interact in breakerTriggers)
        {
            interact.interactable = true;
        }
        if (garageDoorAnimator.GetBool("open") && base.IsServer)// close garage door if open
        {
            garageLeverAnimTrigger.TriggerAnimation(GameNetworkManager.Instance.localPlayerController);
            garageDoorAnimTrigger.TriggerAnimation(GameNetworkManager.Instance.localPlayerController);
        }
        // spawn green dog
        GreenDogPatch.checking = true;
        JLLSpawner.SetActive(true);
        alarmActive = true;
        yield return new WaitForSeconds(1.5f);
        // spikes emerge from ground
        spikesAnimator.SetTrigger("emerge");
        GreenDogPatch.checking = false;
    }

    [ClientRpc]
    public void AlarmShutdownClientRpc()
    {
        if (sendingRPC2)
        {
            sendingRPC2 = false;
        }
        else
        {
            AlarmShutdown();
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void AlarmShutdownServerRpc()
    {
        AlarmShutdownClientRpc();
    }

    public void AlarmShutdownLocal()
    {
        AlarmShutdown();
        sendingRPC2 = true;
        AlarmShutdownServerRpc();
    }

    public void AlarmShutdown()
    {
        lightsManager.EndAlarmSequence();
        alarmActive = false;
        steamLeakParticles.Stop();
        steamSFX.Stop();
        lasherManager.KillLashers();
        StartCoroutine(DisableBreakers());
    }

    public IEnumerator DisableBreakers()
    {
        foreach (InteractTrigger interact in breakerTriggers)
        {
            interact.interactable = false;
            interact.disabledHoverTip = "";
        }
        foreach (Animator animator in breakerAnimators)
        {
            animator.SetBool("on", false);
        }
        foreach (ScanNodeProperties node in breakerScanNodes)
        {
            node.subText = "Inoperable";
        }

        // the only reason this is an IEnemerator is so we can close all the breaker doors after a delay
        yield return new WaitForSeconds(1.7f);
        foreach (InteractTrigger interact in breakerTriggers)
        {
            if (base.IsServer && (bool)interact.GetComponent<AnimatedObjectTrigger>())
            {
                AnimatedObjectTrigger trigger = interact.GetComponent<AnimatedObjectTrigger>();
                if (trigger.boolValue)
                {
                    trigger.TriggerAnimation(GameNetworkManager.Instance.localPlayerController);
                }
            }
        }
    }

    private IEnumerator AlarmPulse()
    {
        lightsManager.PulseAlarmOn();
        yield return new WaitForSeconds(2.5f);
        lightsManager.PulseAlarmOff();
    }
}