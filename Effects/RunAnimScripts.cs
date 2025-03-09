using GameNetcodeStuff;
using System.Collections;
using Unity.Netcode;
using UnityEngine;
using Wither.Mechanics;

namespace Wither.Effects;
public class RunAnimScripts : NetworkBehaviour
{
    public GameObject scriptEvent;

    public AnimatedObjectTrigger animatedObjectTrigger;

    private void OnEnable()
    {
        animatedObjectTrigger.TriggerAnimation(GameNetworkManager.Instance.localPlayerController);
        scriptEvent.GetComponent<PropTP>().TeleportProp();
        HUDManager.Instance.ShakeCamera(ScreenShakeType.VeryStrong);
    }
}