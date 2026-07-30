using UnityEngine;

namespace AITSYS
{
    // Compatibility component used only while migrating pre-package scenes.
    [AddComponentMenu("")]
    public sealed class DiscordRPCSettings : MonoBehaviour
    {
        public bool AITSYS_RPC = true;
    }
}
