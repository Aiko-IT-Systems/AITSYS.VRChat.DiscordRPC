using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace AITSYS.VRCUnity.DiscordRPC
{
    [InitializeOnLoad]
    internal static class LegacySettingsMigration
    {
        private static bool queued;

        static LegacySettingsMigration()
        {
            EditorApplication.hierarchyChanged += Queue;
            EditorSceneManager.sceneOpened += (_, __) => Queue();
            EditorApplication.delayCall += Queue;
        }

        private static void Queue()
        {
            if (queued)
                return;

            queued = true;
            EditorApplication.delayCall += Migrate;
        }

        private static void Migrate()
        {
            queued = false;
            if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Queue();
                return;
            }

            AITSYS.DiscordRPCSettings[] legacySettings =
                Resources.FindObjectsOfTypeAll<AITSYS.DiscordRPCSettings>();
            if (legacySettings == null || legacySettings.Length == 0)
                return;

            VRCUnityDiscordRPCSettings settings = VRCUnityDiscordRPCSettings.instance;
            bool copiedValue = settings.legacySettingsMigrated;

            foreach (AITSYS.DiscordRPCSettings legacy in legacySettings)
            {
                if (legacy == null || !legacy.gameObject.scene.IsValid() || !legacy.gameObject.scene.isLoaded)
                    continue;

                if (!copiedValue)
                {
                    settings.enabled = legacy.AITSYS_RPC;
                    settings.MarkLegacyMigrated();
                    copiedValue = true;
                }

                GameObject owner = legacy.gameObject;
                var scene = owner.scene;
                UnityEngine.Object.DestroyImmediate(legacy, true);

                Component[] remaining = owner.GetComponents<Component>();
                if (remaining.Length == 1 && remaining[0] is Transform &&
                    string.Equals(owner.name, "Discord RPC Settings", StringComparison.OrdinalIgnoreCase))
                {
                    UnityEngine.Object.DestroyImmediate(owner, true);
                }

                EditorSceneManager.MarkSceneDirty(scene);
            }

            DiscordRpcService.RefreshNow();
        }
    }
}
