using UnityEditor;
using UnityEngine;

namespace AITSYS.VRCUnity.DiscordRPC
{
    internal static class DiscordRpcSettingsProvider
    {
        [SettingsProvider]
        private static SettingsProvider CreateProvider()
        {
            return new SettingsProvider("Project/AITSYS/VRC Unity", SettingsScope.Project)
            {
                label = "VRC Unity",
                guiHandler = DrawSettings,
                keywords = new[] { "AITSYS", "Discord", "RPC", "Rich Presence", "VRChat" }
            };
        }

        [MenuItem("AITSYS/VRC Unity/Discord RPC Settings")]
        private static void OpenSettings()
        {
            SettingsService.OpenProjectSettings("Project/AITSYS/VRC Unity");
        }

        private static void DrawSettings(string searchContext)
        {
            VRCUnityDiscordRPCSettings settings = VRCUnityDiscordRPCSettings.instance;

            EditorGUILayout.LabelField("VRC Unity Discord RPC", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            EditorGUI.BeginChangeCheck();
            bool enabled = EditorGUILayout.Toggle("Enable Discord RPC", settings.enabled);
            if (EditorGUI.EndChangeCheck())
                settings.SetEnabled(enabled);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Rotating Statistics", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Adds one cached scene statistic to the current Edit or Play mode and rotates it at a Discord-safe interval. Build and Upload states remain unchanged.",
                MessageType.Info);

            EditorGUI.BeginChangeCheck();
            settings.showSceneStats = EditorGUILayout.Toggle("Show Scene Statistics", settings.showSceneStats);
            using (new EditorGUI.DisabledScope(!settings.showSceneStats))
            {
                settings.statCycleSeconds = EditorGUILayout.IntSlider("Cycle Every (Seconds)", settings.statCycleSeconds, 5, 120);
                EditorGUILayout.LabelField("Included Statistics", EditorStyles.miniBoldLabel);
                settings.statFlags = DrawFlag("Objects", settings.statFlags, RpcStatFlags.Objects);
                settings.statFlags = DrawFlag("Meshes and Renderers", settings.statFlags, RpcStatFlags.MeshesAndRenderers);
                settings.statFlags = DrawFlag("Triangles and Materials", settings.statFlags, RpcStatFlags.TrianglesAndMaterials);
                settings.statFlags = DrawFlag("Lights", settings.statFlags, RpcStatFlags.Lights);
                settings.statFlags = DrawFlag("Last Build Size", settings.statFlags, RpcStatFlags.LastBuildSize);
            }

            if (EditorGUI.EndChangeCheck())
            {
                settings.SaveSettings();
                DiscordRpcService.StatisticsSettingsChanged();
            }

            ProjectContext context = ProjectContext.Resolve();
            EditorGUILayout.LabelField("Detected Project Type", context.ProjectType.ToString());
            EditorGUILayout.HelpBox(context.Reason, MessageType.Info);

            EditorGUILayout.LabelField("Status", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(DiscordRpcService.Status, MessageType.None);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Refresh Presence"))
                    DiscordRpcService.RefreshNow();

                if (GUILayout.Button("Clear Presence"))
                    DiscordRpcService.ClearPresence();
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Application ID", VRCUnityDiscordRPCSettings.ApplicationId);
            EditorGUILayout.LabelField("Platform", "Windows Editor x86_64");
        }

        private static RpcStatFlags DrawFlag(string label, RpcStatFlags flags, RpcStatFlags flag)
        {
            bool enabled = (flags & flag) != 0;
            bool next = EditorGUILayout.ToggleLeft(label, enabled);
            if (next == enabled)
                return flags;

            return next ? flags | flag : flags & ~flag;
        }
    }
}
