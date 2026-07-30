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

            VrcProjectType projectType = ProjectContext.DetectInstalledSdk(out string detectionReason);
            EditorGUILayout.LabelField("Detected Project Type", projectType.ToString());
            EditorGUILayout.HelpBox(detectionReason, projectType == VrcProjectType.Unsupported
                ? MessageType.Warning
                : MessageType.Info);

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
    }
}
