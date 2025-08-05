using GameNetcodeStuff;
using UnityEngine;

namespace Wither.Mechanics;
public class DamageAtPosition : MonoBehaviour
{
    public Transform damagePoint;
    public int damage = -1;
    public float range;
    public bool useAxis;
    public Transform axisMarker;

    public void DamagePlayerByPosition()// do damage either within range of some point, or within range of some axis (i.e. line). this should run for all players as an animation event
    {
        PlayerControllerB localPlayer = GameNetworkManager.Instance.localPlayerController;
        if (localPlayer != null && !localPlayer.isPlayerDead)
        {
            Vector3 position = damagePoint.position;
            Vector3 toPlayer = Vector3.zero;
            Vector3 toMarker = Vector3.zero;
            if (useAxis)
            {
                toPlayer = localPlayer.transform.position - position;// vector from pos1 to player
                toMarker = axisMarker.position - position;// vector from pos1 to pos2, forming the line where damage should be done

                // the normalized dot product of these will be closer to 1 when the vectors are similar, which is equivalent to the player being closer to the line
                //Wither.Logger.LogDebug($"DOT PRODUCT: {Mathf.Abs(Vector3.Dot(Vector3.Normalize(toMarker), Vector3.Normalize(toPlayer)))}");
            }
            //Wither.Logger.LogDebug($"DISTANCE: {Vector3.Distance(localPlayer.transform.position, position)}");
            // use either distance from point or similarity of vectors
            if (Vector3.Distance(localPlayer.transform.position, position) < range && ((useAxis && Mathf.Abs(Vector3.Dot(Vector3.Normalize(toMarker), Vector3.Normalize(toPlayer))) > 0.985) || !useAxis))
            {
                if (damage > 0)
                {
                    localPlayer.DamagePlayer(damage, causeOfDeath: CauseOfDeath.Crushing);
                }
                else
                {
                    localPlayer.KillPlayer(Vector3.zero, causeOfDeath: CauseOfDeath.Crushing);
                }
            }
        }
    }
}
