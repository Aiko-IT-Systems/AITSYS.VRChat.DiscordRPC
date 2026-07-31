using System;
using UnityEditor;
using VRC.SDKBase.Editor;

namespace AITSYS.VRCUnity.DiscordRPC
{
    [InitializeOnLoad]
    internal static class VrcDiscordRpcRuntimeHooks
    {
        private static IVRCSdkBuilderApi registeredBuilder;

        static VrcDiscordRpcRuntimeHooks()
        {
            EditorApplication.delayCall += TryRegisterBuildHooks;
            VRCSdkControlPanel.OnSdkPanelEnable += OnSdkPanelEnabled;
        }

        private static void OnSdkPanelEnabled(object sender, EventArgs args)
        {
            TryRegisterBuildHooks();
        }

        private static void TryRegisterBuildHooks()
        {
            if (!VRCSdkControlPanel.TryGetBuilder(out IVRCSdkBuilderApi builder) ||
                ReferenceEquals(builder, registeredBuilder))
                return;

            RemoveBuildHooks();
            registeredBuilder = builder;
            registeredBuilder.OnSdkBuildStart += OnBuildStarted;
            registeredBuilder.OnSdkUploadStart += OnUploadStarted;
            registeredBuilder.OnSdkBuildFinish += OnBuildFinished;
            registeredBuilder.OnSdkUploadFinish += OnUploadFinished;
        }

        private static void RemoveBuildHooks()
        {
            if (registeredBuilder == null)
                return;

            registeredBuilder.OnSdkBuildStart -= OnBuildStarted;
            registeredBuilder.OnSdkUploadStart -= OnUploadStarted;
            registeredBuilder.OnSdkBuildFinish -= OnBuildFinished;
            registeredBuilder.OnSdkUploadFinish -= OnUploadFinished;
            registeredBuilder = null;
        }

        private static void OnBuildStarted(object sender, object target)
        {
            RpcProjectType type = ProjectContext.Resolve().ProjectType;
            DiscordRpcService.SetState(
                type == RpcProjectType.Avatar ? RpcState.BuildAvatar : RpcState.BuildWorld,
                true);
        }

        private static void OnUploadStarted(object sender, EventArgs args)
        {
            RpcProjectType type = ProjectContext.Resolve().ProjectType;
            DiscordRpcService.SetState(
                type == RpcProjectType.Avatar ? RpcState.UploadAvatar : RpcState.UploadWorld,
                true);
        }

        private static void OnBuildFinished(object sender, string path)
        {
            SceneStatisticsCache.RecordBuildArtifact(path);
            DiscordRpcService.SetState(RpcState.EditMode, true);
        }

        private static void OnUploadFinished(object sender, string result)
        {
            DiscordRpcService.SetState(RpcState.EditMode, true);
        }
    }
}
