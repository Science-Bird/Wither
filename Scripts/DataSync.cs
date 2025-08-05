using Unity.Netcode;
using Wither.Events;
using Wither.Patches;

namespace Wither.Scripts
{
    public class DataSync : NetworkBehaviour
    {
        public static DataSync Instance { get; private set; }

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

        [ClientRpc]
        public void UpdateTerminalPatchClientRpc(bool unlocked, bool unread)// update unlocked and unread booleans to those from the host's save file
        {
            if (!IsServer)
            {
                TerminalEntryPatches.unlocked = unlocked;
                TerminalEntryPatches.unread = unread;
            }
        }

        [ClientRpc]
        public void SetVanillaPlusModeClientRpc()// disable all map event objects at server's request
        {
            if (!IsServer)
            {
                ScenePatches.DisableEventObjects();
            }
        }
    }
}
