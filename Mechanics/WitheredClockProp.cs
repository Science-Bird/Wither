using Unity.Netcode;
using UnityEngine;

namespace Wither.Mechanics;

public class WitheredClockProp : GrabbableObject
{
    public Transform hourHand;

    public Transform minuteHand;

    public Transform secondHand;

    private float timeOfLastSecond;

    private float timeOfLastInterval;

    private float intervalLength = 10f;

    private int secondsPassed;

    private int minutesPassed;

    public AudioSource tickAudio;

    public AudioSource gearAudio;

    public AudioClip tickSFX;

    public AudioClip tockSFX;

    public float targetTimeMultiplier = 1f;

    private float timeMultiplier = 1f;

    private float trueMultiplier = 1f;

    private bool tickOrTock;

    private int timesPlayedInOneSpot;

    private Vector3 lastPosition;

    private bool fastMode = false;

    // highly modified version of the vanilla clock item

    public override void Update()
    {
        base.Update();

        if (!isHeld && !isPocketed) { return; }

        if (base.IsOwner && Time.realtimeSinceStartup - timeOfLastInterval > intervalLength)// check when interval is up
        {
            // random checks run only on item holder, and are then sent to other clients
            if (Random.Range(0,2) == 0)
            {
                targetTimeMultiplier = Random.Range(1f,41f);// 1x to 41x speed
            }
            else
            {
                targetTimeMultiplier = Random.Range(-1f, -3f);// 1x to 1/3x speed
            }
            intervalLength = Random.Range(5,11);// 5-11 seconds between speed changes
            fastMode = false;// we set fast mode to false every interval so we know when a new fast cycle has been called and to play the audible noise for dogs each time
            timeOfLastSecond = Time.realtimeSinceStartup;
            timeOfLastInterval = Time.realtimeSinceStartup;
            //Wither.Logger.LogDebug($"Changing speed to {targetTimeMultiplier}x for {intervalLength}s");
            ChangeMultServerRpc(targetTimeMultiplier, intervalLength);
        }

        // instead of snapping to new multiplier, speed slowly shifts towards it
        if (timeMultiplier > targetTimeMultiplier)
        {
            timeMultiplier -= 0.05f;
        }
        else
        {
            timeMultiplier += 0.05f;
        }
        if (Mathf.Abs(timeMultiplier - targetTimeMultiplier) > 0.1f)// wait until close to the new target speed before starting to count down the interval to the next speed change (so the speed doesn't change before the target can be reached)
        {
            timeOfLastInterval = Time.realtimeSinceStartup;
            //Wither.Logger.LogDebug($"MULT: {timeMultiplier}");
        }
        trueMultiplier = timeMultiplier > 0 ? 1 / timeMultiplier : -timeMultiplier;// since we're dealing with time directly, 40x speed means dividing time delta by 40 (positives), and 1/3x speed means multiplying it by 3
        if (timeMultiplier > 21f || (fastMode && !base.IsOwner))// enter special fast mode when speed > 21x (only owner needs to check this)
        {
            // this is the same logic as in regular case, but we never check the speed so this just runs every frame (maximum possible speed)
            secondHand.Rotate(-6f, 0f, 0f, Space.Self);
            secondsPassed++;
            if (minutesPassed >= 60)
            {
                minutesPassed = 0;
            }
            if (secondsPassed >= 60)
            {
                secondsPassed = 0;
                minutesPassed++;
                minuteHand.Rotate(-6f, 0f, 0f, Space.Self);
                if (minutesPassed % 6 == 0)
                {
                    hourHand.Rotate(-3f, 0f, 0f, Space.Self);
                }
            }

            if (!fastMode && base.IsOwner)// if it's in fast mode, send that information to clients
            {
                fastMode = true;
                ToggleFastServerRpc(true);
            }
        }
        else if (Time.realtimeSinceStartup - timeOfLastSecond > trueMultiplier)// frequency this runs depends on the multiplier
        {
            if (base.IsOwner && gearAudio.isPlaying)// stop fast mode if it was on
            {
                ToggleFastServerRpc(false);
            }
            secondHand.Rotate(-6f, 0f, 0f, Space.Self);// rotate second hand 1/60 around clock
            secondsPassed++;
            if (minutesPassed >= 60)// reset minutes on the hour
            {
                minutesPassed = 0;
            }
            if (secondsPassed >= 60)// when a minute has passed
            {
                secondsPassed = 0;
                minutesPassed++;
                minuteHand.Rotate(-6f, 0f, 0f, Space.Self);// rotate minute hand 1/60 around clock
                if (minutesPassed % 6 == 0)
                {
                    hourHand.Rotate(-3f, 0f, 0f, Space.Self);// every 6 minutes, rotate hour hand 1/120 around the clock (10 times in an hour = 1/12 rotation around clock)
                }
            }
            timeOfLastSecond = Time.realtimeSinceStartup;
            tickOrTock = !tickOrTock;

            if (tickOrTock)
            {
                tickAudio.PlayOneShot(tickSFX);
            }
            else
            {
                tickAudio.PlayOneShot(tockSFX);
            }
        }
        else
        {
            gearAudio.Stop();
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void ChangeMultServerRpc(float mult, float length)
    {
        ChangeMultClientRpc(mult, length);
    }

    [ClientRpc]
    public void ChangeMultClientRpc(float mult, float length)
    {
        if (!base.IsOwner)// technically non-owners don't use the interval length, but we set it so if they pick up the item it'll know where the last person left off in the cycle
        {
            targetTimeMultiplier = mult;
            intervalLength = length;
            //Wither.Logger.LogDebug($"Changing speed to {targetTimeMultiplier}x");
            timeOfLastSecond = Time.realtimeSinceStartup;
            timeOfLastInterval = Time.realtimeSinceStartup;
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void ToggleFastServerRpc(bool on)
    {
        ToggleFastClientRpc(on);
    }

    [ClientRpc]
    public void ToggleFastClientRpc(bool on)// it's important we sync this behaviour specifically because it's what alerts dogs
    {
        if (on)
        {
            //Wither.Logger.LogDebug("FAST START!");
            if (!gearAudio.isPlaying)
            {
                gearAudio.Play();
            }
            fastMode = true;
            if (Vector3.Distance(lastPosition, base.transform.position) < 4f)
            {
                timesPlayedInOneSpot++;
            }
            else
            {
                timesPlayedInOneSpot = 0;
            }
            RoundManager.Instance.PlayAudibleNoise(base.transform.position, 17f, 0.55f, timesPlayedInOneSpot, isInElevator && StartOfRound.Instance.hangarDoorsClosed);
            lastPosition = base.transform.position;
        }
        else
        {
            gearAudio.Stop();
            fastMode = false;
        }
    }

    public override void DiscardItem()// stop fast mode if item is dropped (this method is synced)
    {
        base.DiscardItem();
        gearAudio.Stop();
        fastMode = false;
    }
}
