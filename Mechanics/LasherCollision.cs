using System.Collections;
using GameNetcodeStuff;
using UnityEngine;

namespace Wither.Mechanics;
public class LasherCollision : MonoBehaviour, IHittable
{
    public WitheredLasher lasherScript;
    public int tentacleIndex = -1;// used for group animations where multiple lashers are controlled by one script, this determines which lasher the collider belongs to (for corpse grabbing)

    bool IHittable.Hit(int force, Vector3 hitDirection, PlayerControllerB playerWhoHit, bool playHitSFX, int hitID)
    {
        if (lasherScript.doingCollisions)
        {
            lasherScript.KillLasherLocal();
            return true;
        }
        return false;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (lasherScript.doingCollisions && other.CompareTag("Player") && (bool)other.gameObject.GetComponent<PlayerControllerB>())
        {
            PlayerControllerB player = other.gameObject.GetComponent<PlayerControllerB>();
            if (player.IsOwner && !player.isPlayerDead && !lasherScript.ignoreLocalPlayer)
            {
                //Wither.Logger.LogDebug("PLAYER COLLIDED WITH LASHER");
                if (player.health > 30)
                {
                    lasherScript.tentacleSFX.PlayOneShot(lasherScript.hitPlayerClip);
                    float dist = Vector3.Distance(player.transform.position, base.transform.position);
                    Vector3 direction = Vector3.Normalize(player.transform.position + Vector3.up * dist - base.transform.position);
                    // push player away from lasher, adding an upward component (inversely proportional to how upwards the original direction would be, so upward component is relatively constant)
                    Vector3 force = direction * 15f + (1f - Vector3.Dot(direction, Vector3.up)) * Vector3.up * 30f;

                    player.DamagePlayer(30, causeOfDeath: CauseOfDeath.Strangulation);
                    player.externalForceAutoFade += force;
                    StartCoroutine(InvulnerabilityFrames());
                }
                else// this will ignore critical injury check
                {
                    //Wither.Logger.LogDebug("KILLING PLAYER");
                    player.KillPlayer(Vector3.zero, causeOfDeath: CauseOfDeath.Strangulation);
                    if (lasherScript.grabber)
                    {
                        lasherScript.GrabPlayerCorpseLocal((int)GameNetworkManager.Instance.localPlayerController.playerClientId, tentacleIndex);
                    }
                }
            }
        }
    }

    private IEnumerator InvulnerabilityFrames()// so the same lasher can't hit a player twice in quick succession
    {
        lasherScript.ignoreLocalPlayer = true;
        yield return new WaitForSeconds(0.7f);
        lasherScript.ignoreLocalPlayer = false;
    }
}
