using System;
using UnityEditor;
using VRC.SDKBase.Editor;

namespace AITSYS.VRCUnity.DiscordRPC
{
    [InitializeOnLoad]
    internal static class DiscordRpcRuntimeHooks
    {
        private static IVRCSdkBuilderApi registeredBuilder;

        static DiscordRpcRuntimeHooks()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            EditorApplication.delayCall += TryRegisterBuildHooks;
            VRCSdkControlPanel.OnSdkPanelEnable += OnSdkPanelEnabled;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredEditMode)
                DiscordRpcService.SetState(RpcState.EditMode, true);
            else if (state == PlayModeStateChange.EnteredPlayMode)
                DiscordRpcService.SetState(RpcState.PlayMode, true);
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
            VrcProjectType type = ProjectContext.DetectInstalledSdk(out _);
            DiscordRpcService.SetState(
                type == VrcProjectType.Avatar ? RpcState.BuildAvatar : RpcState.BuildWorld,
                true);
        }

        private static void OnUploadStarted(object sender, EventArgs args)
        {
            VrcProjectType type = ProjectContext.DetectInstalledSdk(out _);
            DiscordRpcService.SetState(
                type == VrcProjectType.Avatar ? RpcState.UploadAvatar : RpcState.UploadWorld,
                true);
        }

        private static void OnBuildFinished(object sender, string path)
        {
            DiscordRpcService.SetState(RpcState.EditMode, true);
        }

        private static void OnUploadFinished(object sender, string result)
        {
            DiscordRpcService.SetState(RpcState.EditMode, true);
        }
    }
}
