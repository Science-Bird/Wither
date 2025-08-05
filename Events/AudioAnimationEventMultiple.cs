using UnityEngine;
using UnityEngine.Events;

namespace Wither.Events;
public class AudioAnimationEventMultiple : MonoBehaviour
{
    public AudioSource audioSource1;
    public AudioSource audioSource2;
    public AudioSource audioSource3;

    public AudioClip audioClip1;
    public AudioClip audioClip2;
    public AudioClip audioClip3;

    public float volume1 = 1f;
    public float volume2 = 1f;
    public float volume3 = 1f;

    public bool playAudibleNoise;

    public UnityEvent onAnimationEventCalled;

    // modified version of a vanilla class to make it more convenient for use in my animations

    public void PlayAudio1()
    {
        if (audioSource1 == null || audioClip1 == null) { return; }
        audioSource1.PlayOneShot(audioClip1,volume1);
        WalkieTalkie.TransmitOneShotAudio(audioSource1, audioClip1);
        if (playAudibleNoise)
        {
            RoundManager.Instance.PlayAudibleNoise(transform.position, 10f, 0.65f, 0, noiseIsInsideClosedShip: false, 546);
        }
    }

    public void PlayAudio2()
    {
        if (audioSource2 == null || audioClip2 == null) { return; }
        audioSource2.PlayOneShot(audioClip2,volume2);
        WalkieTalkie.TransmitOneShotAudio(audioSource2, audioClip2);
        if (playAudibleNoise)
        {
            RoundManager.Instance.PlayAudibleNoise(transform.position, 10f, 0.65f, 0, noiseIsInsideClosedShip: false, 546);
        }
    }

    public void PlayAudio3()
    {
        if (audioSource3 == null || audioClip3 == null) { return; }
        audioSource3.PlayOneShot(audioClip3,volume3);
        WalkieTalkie.TransmitOneShotAudio(audioSource3, audioClip3);
        if (playAudibleNoise)
        {
            RoundManager.Instance.PlayAudibleNoise(transform.position, 10f, 0.65f, 0, noiseIsInsideClosedShip: false, 546);
        }
    }

    public void OnAnimationEvent()
    {
        onAnimationEventCalled.Invoke();
    }
}