using UnityEditor;

namespace AITSYS.VRCUnity.DiscordRPC
{
    [FilePath("ProjectSettings/VRCUnityDiscordRPCSettings.asset", FilePathAttribute.Location.ProjectFolder)]
    internal sealed class VRCUnityDiscordRPCSettings : ScriptableSingleton<VRCUnityDiscordRPCSettings>
    {
        internal const string ApplicationId = "1032024833493569546";

        public bool enabled = true;
        public bool showSceneStats = true;
        public int statCycleSeconds = 15;
        public RpcStatFlags statFlags = RpcStatFlags.All;
        public long lastBuildSizeBytes;
        public bool legacySettingsMigrated;

        internal void SetEnabled(bool value)
        {
            if (enabled == value)
                return;

            enabled = value;
            Save(true);
            DiscordRpcService.RefreshNow();
        }

        internal void MarkLegacyMigrated()
        {
            legacySettingsMigrated = true;
            Save(true);
        }

        internal void SetLastBuildSize(long bytes)
        {
            lastBuildSizeBytes = bytes;
            Save(true);
        }

        internal void SaveSettings()
        {
            Save(true);
        }
    }
}
