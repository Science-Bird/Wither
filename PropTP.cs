using GameNetcodeStuff;
using System.Collections;
using Unity.Netcode;
using UnityEngine;

namespace Wither;
public class PropTP : NetworkBehaviour
{	
	public GameObject[] scrapProps;

	public Vector3 telePosition;

	public GameObject[] scanNodes;

	private int scrapIndex = 0;

	private bool wasFallingLastFrame = false;

	private bool doingTP = false;

	public AudioSource clangPlayer;

	public float percentOfQuota = 20f;

	private Vector3 posDiff;

	private Vector3 posDiff1;

	private Vector3 posDiff2;

	private float fallTimeLastFrame;

	private float yLastFrame = 0f;

	private int scrapValue;

	private void Start()
	{
		posDiff = telePosition - scrapProps[0].transform.position;
		posDiff1 = scrapProps[1].transform.position - scrapProps[0].transform.position;
        posDiff2 = scrapProps[2].transform.position - scrapProps[0].transform.position;
    }

	private void Update()
	{
		if (doingTP)
		{
			if ((scrapProps[scrapIndex].GetComponent<GrabbableObject>().fallTime >= 1f || scrapProps[scrapIndex].GetComponent<GrabbableObject>().fallTime < fallTimeLastFrame || (scrapProps[scrapIndex].transform.position.y == yLastFrame && yLastFrame < 112)) && wasFallingLastFrame && base.IsServer)
			{
				CyclePropClientRpc();
				if (!doingTP || scrapIndex > 2)
				{
                    Wither.Logger.LogDebug("Prop TP finished.");
                    return;
				}
            }
			if (scrapProps[scrapIndex].GetComponent<GrabbableObject>().fallTime < 1f) {
				wasFallingLastFrame = true;
			}
			fallTimeLastFrame = scrapProps[scrapIndex].GetComponent<GrabbableObject>().fallTime;
			yLastFrame = scrapProps[scrapIndex].transform.position.y;
		}

		
	}

	public void TeleportProp()
	{
		doingTP = true;
		StartCoroutine(waitToEndOfFrameToFall());
	}

	private IEnumerator waitToEndOfFrameToFall()
	{
		yield return new WaitForEndOfFrame();
		PropsTeleport();
	}

	public void PropsTeleport()
	{
		int i = scrapIndex;
		int floorYRot = 0;
		if (i == 0)
		{
			scrapProps[i].transform.Rotate(0f,0f,227f,Space.Self);
			floorYRot = 227;
		}
		else if (i == 1)
		{
			scrapProps[i].transform.Rotate(0f,0f,45f,Space.Self);
			floorYRot = 45;
		}
		else
		{
			scrapProps[i].transform.Rotate(0f,0f,110f,Space.Self);
			floorYRot = 110;
		}

		scrapProps[i].transform.position = scrapProps[i].transform.position + posDiff;
		float scrapValueFloat = TimeOfDay.Instance.profitQuota * (percentOfQuota / 300);
		scrapValue = Mathf.RoundToInt(scrapValueFloat);
		if (scrapValue < 50)
		{
			scrapValue = 50;
		}
		else if (scrapValue > 250)
		{
			scrapValue = 250;
		}
		scrapProps[i].GetComponent<GrabbableObject>().scrapValue = scrapValue;
		scanNodes[i].GetComponentInChildren<ScanNodeProperties>().scrapValue = scrapValue;
		scanNodes[i].GetComponentInChildren<ScanNodeProperties>().subText = $"Value: {scrapValue}";
		//GetPhysicsRegionOfDroppedObject function
		Vector3 hitPoint;
		Transform transform = null;
		RaycastHit hitInfo;
		Ray ray = new Ray(scrapProps[i].transform.position, -Vector3.up);
		if (Physics.Raycast(ray, out hitInfo, 80f, 1342179585, QueryTriggerInteraction.Ignore))
		{
			Debug.DrawRay(scrapProps[i].transform.position, -Vector3.up * 80f, Color.blue, 2f);
			transform = hitInfo.collider.gameObject.transform;
		}
		
		if (transform != null)
		{
			hitPoint = hitInfo.point + Vector3.up * 0.04f + scrapProps[i].GetComponent<GrabbableObject>().itemProperties.verticalOffset * Vector3.up;

			//PlaceGrabbableObject function
			GrabbableObject placeObject = scrapProps[i].GetComponent<GrabbableObject>();
			placeObject.parentObject = null;
			placeObject.EnablePhysics(enable: true);
			placeObject.EnableItemMeshes(enable: true);
			placeObject.isHeld = false;
			placeObject.isPocketed = false;
			placeObject.heldByPlayerOnServer = false;
			placeObject.transform.localScale = placeObject.originalScale;
			placeObject.transform.position = placeObject.transform.position;
			placeObject.startFallingPosition = placeObject.transform.position;
			placeObject.targetFloorPosition = hitPoint;
			placeObject.floorYRot = floorYRot;
			placeObject.fallTime = 0f;
			DropObjectServerRpc(floorYRot, hitPoint, scrapProps[i].GetComponent<NetworkObject>(), i);
		}
		else
		{
            Wither.Logger.LogDebug($"Null transform at {i}");
		}
	}

    [ServerRpc(RequireOwnership = false)]
    private void DropObjectServerRpc(int floorYRot, Vector3 hitPoint, NetworkObjectReference grabbedObject, int index)
{		{
               DropObjectClientRpc(floorYRot, hitPoint, grabbedObject, index);
		}
}
    [ClientRpc]
    private void DropObjectClientRpc(int floorYRot, Vector3 hitPoint, NetworkObjectReference grabbedObject, int index)
{		if (grabbedObject.TryGet(out var networkObject))
		{
            GrabbableObject component = networkObject.GetComponent<GrabbableObject>();
            //PlaceGrabbableObject function
            component.parentObject = null;
            component.scrapValue = scrapValue;
            networkObject.GetComponentInChildren<ScanNodeProperties>().scrapValue = scrapValue;
            networkObject.GetComponentInChildren<ScanNodeProperties>().subText = $"Value: {scrapValue}";
            component.EnablePhysics(enable: true);
            component.EnableItemMeshes(enable: true);
            component.isHeld = false;
            component.isPocketed = false;
            component.heldByPlayerOnServer = false;
            component.transform.localScale = component.originalScale;
			if (index == 0)
			{
                component.transform.position = telePosition;
                component.startFallingPosition = telePosition;
            }
			else if (index == 1)
			{
                component.transform.position = telePosition + posDiff1;
                component.startFallingPosition = telePosition + posDiff1;
            }
			else if (index == 2)
			{
                component.transform.position = telePosition + posDiff2;
                component.startFallingPosition = telePosition + posDiff2;
            }
            component.startFallingPosition = component.transform.position;
            component.targetFloorPosition = hitPoint;
            component.floorYRot = floorYRot;
            component.fallTime = 0f;

            if (!component.itemProperties.syncDiscardFunction)
            {
                component.playerHeldBy = null;
            }
        }
        else
        {
            Wither.Logger.LogDebug("The server did not have a reference to the held object (when attempting to PLACE object on client.)");
        }
    }

    [ClientRpc]
    public void CyclePropClientRpc()
{	{
        clangPlayer.PlayOneShot(scrapProps[scrapIndex].GetComponent<GrabbableObject>().itemProperties.dropSFX);
        WalkieTalkie.TransmitOneShotAudio(clangPlayer, scrapProps[scrapIndex].GetComponent<GrabbableObject>().itemProperties.dropSFX);
        wasFallingLastFrame = false;
        if (scrapIndex < 2)
        {
            scrapIndex += 1;
            TeleportProp();
        }
        else
        {
            doingTP = false;
        }
    }
}

}

