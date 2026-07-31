using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace AITSYS.VRCUnity.DiscordRPC
{
    [InitializeOnLoad]
    internal static class DiscordRpcRuntimeHooks
    {
        static DiscordRpcRuntimeHooks()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredEditMode)
                DiscordRpcService.SetState(RpcState.EditMode, true);
            else if (state == PlayModeStateChange.EnteredPlayMode)
                DiscordRpcService.SetState(RpcState.PlayMode, true);
        }
    }

    internal sealed class DiscordRpcBuildSizeHook : IPostprocessBuildWithReport
    {
        public int callbackOrder => 0;

        public void OnPostprocessBuild(BuildReport report)
        {
            if (report == null)
                return;

            SceneStatisticsCache.RecordBuildSize((long)report.summary.totalSize);
        }
    }
}
