using GameNetcodeStuff;
using System.Collections;
using Unity.Netcode;
using UnityEngine;

namespace Wither;
public class LoopAudibleHorn : NetworkBehaviour
{	
	public Transform noisePosition;

	public AudioSource hornPlayer;

	public bool isPlaying = true;

	public int loopCounter = 16;

    public AudioClip rumbleClip;

	private float loopTimer = 2f;

	private void Update()
	{
		if (isPlaying)
		{
            LoopAudio();
		}
	}

	public void LoopAudio()
	{
        if (!hornPlayer.isPlaying)
        {
            hornPlayer.Play();
            loopCounter -= 1;
            if (!GameNetworkManager.Instance.localPlayerController.isInsideFactory)
            {
                HUDManager.Instance.ShakeCamera(ScreenShakeType.Big);
                SoundManager.Instance.PlaySoundAroundLocalPlayer(rumbleClip, 0.65f);
            }
        }
        if (loopTimer <= 0f)
        {
            RoundManager.Instance.PlayAudibleNoise(noisePosition.position, 20f, 0.6f, 0, false);
            loopTimer = 2f;
        }
        else
        {
            loopTimer -= Time.deltaTime;
        }
        if (loopCounter <= 0)
        {
            isPlaying = false;
        }
    }
}