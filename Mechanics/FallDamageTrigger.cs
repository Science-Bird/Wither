using GameNetcodeStuff;
using UnityEngine;

namespace Wither.Mechanics;
public class FallDamageTrigger : MonoBehaviour
{
    public static bool fallImmune = false;
    private bool inTrigger = false;
    private int frameCounter = 0;

    private void Update()// a bunch of checks to make sure fall immunity is definitely cleared when the player leaves the trigger
    {
        if (fallImmune)
        {
            if (!inTrigger)// often there is an occasional frame where a player is not considered in the trigger even when they are, so we wait for 5 frames in a row before disabling the immunity
            {
                frameCounter++;
                if (frameCounter > 5)
                {
                    fallImmune = false;
                    frameCounter = 0;
                }
            }
        }
        if (inTrigger)// continually set inTrigger false so trigger stay has to keep updating it
        {
            inTrigger = false;
        }
    }

    private void OnTriggerStay(Collider other)// make player immune to fall damage when in trigger
    {
        if (other.CompareTag("Player") && (bool)other.gameObject.GetComponent<PlayerControllerB>() && other.gameObject.GetComponent<PlayerControllerB>().IsOwner && !other.gameObject.GetComponent<PlayerControllerB>().isPlayerDead)
        {
            frameCounter = 0;
            inTrigger = true;
            fallImmune = true;
        }
    }

    private void OnTriggerExit(Collider other)// stuff like teleporting won't count as exiting the trigger
    {
        if (other.CompareTag("Player") && (bool)other.gameObject.GetComponent<PlayerControllerB>() && other.gameObject.GetComponent<PlayerControllerB>().IsOwner)
        {
            inTrigger = false;
            fallImmune = false;
            frameCounter = 0;
        }
    }
}
