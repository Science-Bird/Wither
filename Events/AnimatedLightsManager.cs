using System.Collections;
using Unity.Netcode;
using UnityEngine;

namespace Wither.Events;
public class AnimatedLightsManager : NetworkBehaviour
{
    public Animator[] powerShutdownLinked;

    public Animator[] alarmLinked;

    public AudioSource interiorPower;
    public AudioClip powerOnClip;
    public AudioClip powerOffClip;

    public bool permanentlyOff = false;

    public static AnimatedLightsManager Instance { get; private set; }

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

    public void BeginAlarmSequence()
    {
        foreach (Animator animator in alarmLinked)
        {
            animator.ResetTrigger("on");
            animator.ResetTrigger("off");
            animator.SetTrigger("alarm");
        }
    }

    public void PulseAlarmOn()
    {
        foreach (Animator animator in alarmLinked)
        {
            animator.SetBool("pulsing",true);
        }
    }

    public void PulseAlarmOff()
    {
        foreach (Animator animator in alarmLinked)
        {
            animator.SetBool("pulsing",false);
        }
    }

    public void EndAlarmSequence()
    {
        foreach (Animator animator in alarmLinked)
        {
            animator.ResetTrigger("on");
            animator.ResetTrigger("alarm");
            animator.SetTrigger("off");
        }
        permanentlyOff = true;
    }

    public void PowerOn(bool ignoreCheck = false)// turn on false interior lights
    {
        if (ignoreCheck || (!permanentlyOff && StartOfRound.Instance.shipHasLanded))
        {
            interiorPower.PlayOneShot(powerOnClip);
            foreach (Animator animator in powerShutdownLinked)
            {
                animator.ResetTrigger("off");
                animator.SetTrigger("on");
            }
        }
    }

    public void PowerOff(bool ignoreCheck = false)// turn off false interior lights
    {
        if (ignoreCheck || StartOfRound.Instance.shipHasLanded)
        {
            if (!permanentlyOff)
            {
                interiorPower.PlayOneShot(powerOffClip);
            }
            foreach (Animator animator in powerShutdownLinked)
            {
                animator.ResetTrigger("on");
                animator.SetTrigger("off");
            }
        }
    }

    public static void RestartLight(GameObject light)
    {
        if (Instance != null)
        {
            Instance.StartCoroutine(LightSwitcher(light));
        }
    }

    private static IEnumerator LightSwitcher(GameObject light)
    {
        light.SetActive(false);
        yield return new WaitForSeconds(1f);
        light.SetActive(true);
    }

}