using GameNetcodeStuff;
using System.Collections;
using Unity.Netcode;
using UnityEngine;
using System.Linq;

namespace Wither.Mechanics;
public class PropTP : NetworkBehaviour
{
    public GameObject[] scrapProps;

    public Vector3 telePosition;

    public ScanNodeProperties[] scanNodes;

    private int scrapIndex = -1;

    public static bool doingTP = false;

    public AudioSource clangPlayer;

    private Vector3 posDiff;

    private Vector3 posDiff1;

    private Vector3 posDiff2;

    private int scrapValue = 0;

    private bool foundPrefabs = false;

    private int failCount = 0;

    private string[] scrapNames = ["WitheredRobotToy(Clone)", "WitheredOldPhone(Clone)", "WitheredDentures(Clone)"];

    private float[] randomWeights = [0f, 0f, 0f];

    private bool initialSet = true;

    public static bool propReady = false;

    private int propCycleFailsafe = 0;

    private int minVal;

    private int maxVal;

    public static int apparatusValue;

    private void Update()
    {
        if (initialSet)
        {
            apparatusValue = 0;
            if (Wither.MinValue.Value > Wither.MaxValue.Value)
            {
                minVal = 50;
                maxVal = 300;
            }
            else
            {
                minVal = Wither.MinValue.Value;
                maxVal = Wither.MaxValue.Value;
            }
            if (Patches.SeedWeightRandom.randomWeightsTemp[0] > 0f)
            {
                propReady = false;
                randomWeights = Patches.SeedWeightRandom.randomWeightsTemp;
                initialSet = false;
                Patches.SeedWeightRandom.randomWeightsTemp = [0f, 0f, 0f];
            }
        }

        if (!foundPrefabs)
        {
            for (int i = 0; i < 3; i++)
            {
                if (GameObject.Find(scrapNames[i]))
                {
                    GameObject[] objectList = Resources.FindObjectsOfTypeAll<GameObject>().Where(obj => obj.name == scrapNames[i]).ToArray();
                    if (objectList.Length > 1)
                    {
                        foreach (GameObject obj in objectList)
                        {
                            if (!obj.GetComponent<GrabbableObject>().isInShipRoom && !obj.GetComponent<GrabbableObject>().scrapPersistedThroughRounds)
                            {
                                scrapProps[i] = obj;
                                break;
                            }
                        }
                    }
                    else
                    {
                        scrapProps[i] = GameObject.Find(scrapNames[i]);
                    }
                    if (i == 0)
                    {
                        Wither.Logger.LogDebug("Found prefabs!");
                        foundPrefabs = true;
                    }
                }
                else
                {
                    foundPrefabs = false;
                }
            }
            GameObject apparatus = GameObject.Find("DyingLungApparatus(Clone)");
            GameObject[] appList = Resources.FindObjectsOfTypeAll<GameObject>().Where(obj => obj.name == "DyingLungApparatus(Clone)").ToArray();
            if (appList.Length > 1)
            {
                foreach (GameObject obj in appList)
                {
                    if (!obj.GetComponent<GrabbableObject>().isInShipRoom && !obj.GetComponent<GrabbableObject>().scrapPersistedThroughRounds)
                    {
                        Wither.Logger.LogDebug("Found apparatus!");
                        apparatus = obj;
                        break;
                    }
                }
            }
            LungProp lungScript = apparatus.GetComponentInChildren<LungProp>();
            lungScript.isLungPowered = true;
            lungScript.isLungDocked = true;
            AudioSource lungAudio = apparatus.GetComponentInChildren<AudioSource>();
            lungAudio.loop = true;
            lungAudio.Play();

            if (foundPrefabs)
            {
                for (int i = 0; i < 3; i++)
                {
                    scanNodes[i] = scrapProps[i].GetComponentInChildren<ScanNodeProperties>();
                }
                telePosition += new Vector3(0.5f, 0f, 0.5f);
                posDiff = telePosition - scrapProps[0].transform.position;
                posDiff1 = scrapProps[1].transform.position - scrapProps[0].transform.position;
                posDiff2 = scrapProps[2].transform.position - scrapProps[0].transform.position;
            }
            else
            {
                failCount += 1;
            }
            if (failCount >= 100)
            {
                foundPrefabs = true;
            }
        }
        if (doingTP && scrapIndex >= 0)
        {
            if (IsServer)
            {
                propCycleFailsafe += 1;
                if (propCycleFailsafe >= 150)
                {
                    Wither.Logger.LogWarning($"Never recieved fall! Falling back to failsafe...");
                    propReady = true;
                }
            }
            GrabbableObject currentProp = scrapProps[scrapIndex].GetComponent<GrabbableObject>();

            if (propReady && IsServer)
            {
                propCycleFailsafe = 0;
                propReady = false;
                Wither.Logger.LogDebug("Cycling prop...");
                CyclePropClientRpc();
                if (!doingTP || scrapIndex > 2)
                {
                    Wither.Logger.LogInfo("Prop TP finished.");
                    doingTP = false;
                    return;
                }
            }
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
        scrapIndex += 1;
        if (scrapIndex == 0)
        {
            Wither.Logger.LogDebug($"-------------");
            Wither.Logger.LogDebug($"Apparatus value calculation breakdown:");
            Wither.Logger.LogDebug($"Initial base values: {(TimeOfDay.Instance.profitQuota * Wither.QuotaFraction.Value) / 3}, {(TimeOfDay.Instance.profitQuota * Wither.QuotaFraction.Value) / 3}, {(TimeOfDay.Instance.profitQuota * Wither.QuotaFraction.Value) / 3}");
            Wither.Logger.LogDebug($"({Wither.QuotaFraction.Value * 100}% of quota {TimeOfDay.Instance.profitQuota} is {TimeOfDay.Instance.profitQuota * Wither.QuotaFraction.Value})");
            float tempFloat = ((TimeOfDay.Instance.profitQuota * Wither.QuotaFraction.Value) / 3 > minVal) ? (TimeOfDay.Instance.profitQuota * Wither.QuotaFraction.Value / 3) : minVal;
            Wither.Logger.LogDebug($"Restrict to minimum value ({minVal}): {tempFloat}, {tempFloat}, {tempFloat}");
            float tempFloat1 = tempFloat * randomWeights[0];
            float tempFloat2 = tempFloat * randomWeights[1];
            float tempFloat3 = tempFloat * randomWeights[2];
            Wither.Logger.LogDebug($"Apply random weights ({randomWeights[0]}, {randomWeights[1]}, {randomWeights[2]}): {tempFloat1}, {tempFloat2}, {tempFloat3}");
            if (apparatusValue > 0)
            {
                Wither.Logger.LogDebug($"Inserted apparatus value: {apparatusValue}, base apparatus scaling value: {Wither.ScalingBase.Value}");
                tempFloat1 = tempFloat1 * ((float)apparatusValue / (float)Wither.ScalingBase.Value);
                tempFloat2 = tempFloat2 * ((float)apparatusValue / (float)Wither.ScalingBase.Value);
                tempFloat3 = tempFloat3 * ((float)apparatusValue / (float)Wither.ScalingBase.Value);
                Wither.Logger.LogDebug($"Applying apparatus scaling ({((float)apparatusValue / (float)Wither.ScalingBase.Value)}x): {tempFloat1}, {tempFloat2}, {tempFloat3}");
            }
            else
            {
                Wither.Logger.LogDebug("Didn't find apparatus value! Not applying scaling factor.");
            }
            int tempInt1 = Mathf.RoundToInt(tempFloat1);
            int tempInt2 = Mathf.RoundToInt(tempFloat2);
            int tempInt3 = Mathf.RoundToInt(tempFloat3);
            Wither.Logger.LogDebug($"Rounding to int: {tempInt1}, {tempInt2}, {tempInt3}");
            tempInt1 = tempInt1 > minVal ? tempInt1 : minVal;
            tempInt2 = tempInt2 > minVal ? tempInt2 : minVal;
            tempInt3 = tempInt3 > minVal ? tempInt3 : minVal;
            Wither.Logger.LogDebug($"Restrict to minimum value ({minVal}): {tempInt1}, {tempInt2}, {tempInt3}");
            tempInt1 = tempInt1 < maxVal ? tempInt1 : maxVal;
            tempInt2 = tempInt2 < maxVal ? tempInt2 : maxVal;
            tempInt3 = tempInt3 < maxVal ? tempInt3 : maxVal;
            Wither.Logger.LogDebug($"Restrict to maximum value ({maxVal}): {tempInt1}, {tempInt2}, {tempInt3}");
            Wither.Logger.LogDebug($"");
            Wither.Logger.LogDebug($"FINAL VALUES:");
            Wither.Logger.LogDebug($"Robot: {tempInt1}, Phone: {tempInt2}, Teeth: {tempInt3}");
            Wither.Logger.LogDebug($"-------------");
        }
        int i = scrapIndex;
        int floorYRot = 0;

        float scrapValueFloat = (TimeOfDay.Instance.profitQuota * Wither.QuotaFraction.Value) / 3;
        if (scrapValueFloat < minVal)
        {
            scrapValueFloat = minVal;
        }
        float randomWeight = randomWeights[i];
        scrapValueFloat = scrapValueFloat * randomWeight;
        if (apparatusValue > 0)
        {
            scrapValueFloat = scrapValueFloat * ((float)apparatusValue / (float)Wither.ScalingBase.Value);
        }
        scrapValue = Mathf.RoundToInt(scrapValueFloat);
        if (scrapValue < minVal)
        {
            scrapValue = minVal;
        }
        else if (scrapValue > maxVal)
        {
            scrapValue = maxVal;
        }
        if (i == 0)
        {
            scrapProps[i].transform.Rotate(0f, 0f, 227f, Space.Self);
            floorYRot = 227;
        }
        else if (i == 1)
        {
            scrapProps[i].transform.Rotate(0f, 0f, 45f, Space.Self);
            floorYRot = 45;
        }
        else
        {
            scrapProps[i].transform.Rotate(0f, 0f, 110f, Space.Self);
            floorYRot = 110;
        }
        scrapProps[i].GetComponent<GrabbableObject>().grabbable = false;
        scrapProps[i].transform.position = scrapProps[i].transform.position + posDiff;
        scrapProps[i].GetComponent<GrabbableObject>().scrapValue = scrapValue;
        scanNodes[i].scrapValue = scrapValue;
        scanNodes[i].subText = $"Value: {scrapValue}";
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
    {
        {
            DropObjectClientRpc(floorYRot, hitPoint, grabbedObject, index);
        }
    }
    [ClientRpc]
    private void DropObjectClientRpc(int floorYRot, Vector3 hitPoint, NetworkObjectReference grabbedObject, int index)
    {
        if (grabbedObject.TryGet(out var networkObject))
        {
            GrabbableObject component = networkObject.GetComponent<GrabbableObject>();
            //PlaceGrabbableObject function
            component.parentObject = null;
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
    {
        {
            if (scrapIndex > 2 || scrapIndex < 0)
            {
                Wither.Logger.LogError($"Invalid scrap index {scrapIndex}!");
                if (scrapIndex < 0)
                {
                    TeleportProp();
                }
                else if (scrapIndex > 2)
                {
                    Wither.Logger.LogDebug("Making props grabbable... (EDGE CASE)");
                    for (int i = 0; i < 3; i++)
                    {
                        AnimatedItem scrapPropAnim = scrapProps[i].GetComponentInChildren<AnimatedItem>();
                        if (scrapPropAnim.itemRandomChance == null)
                        {
                            Wither.Logger.LogInfo("Fixing System.Random!");
                            scrapPropAnim.itemRandomChance = new System.Random(StartOfRound.Instance.randomMapSeed + StartOfRound.Instance.currentLevelID + scrapPropAnim.itemProperties.itemId);
                        }
                        scrapProps[i].GetComponent<GrabbableObject>().grabbable = true;
                    }
                    doingTP = false;
                }
                return;
            }
            clangPlayer.PlayOneShot(scrapProps[scrapIndex].GetComponent<GrabbableObject>().itemProperties.dropSFX);
            WalkieTalkie.TransmitOneShotAudio(clangPlayer, scrapProps[scrapIndex].GetComponent<GrabbableObject>().itemProperties.dropSFX);
            if (scrapIndex < 2)
            {
                TeleportProp();
            }
            else
            {
                Wither.Logger.LogDebug("Making props grabbable...");
                for (int i = 0; i < 3; i++)
                {
                    AnimatedItem scrapPropAnim = scrapProps[i].GetComponentInChildren<AnimatedItem>();
                    if (scrapPropAnim.itemRandomChance == null)
                    {
                        Wither.Logger.LogInfo("Fixing System.Random!");
                        scrapPropAnim.itemRandomChance = new System.Random(StartOfRound.Instance.randomMapSeed + StartOfRound.Instance.currentLevelID + scrapPropAnim.itemProperties.itemId);
                    }
                    scrapProps[i].GetComponent<GrabbableObject>().grabbable = true;
                }
                doingTP = false;
            }
        }
    }

}

