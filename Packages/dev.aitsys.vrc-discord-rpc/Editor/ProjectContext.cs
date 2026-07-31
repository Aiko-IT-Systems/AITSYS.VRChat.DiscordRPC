using System;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AITSYS.VRCUnity.DiscordRPC
{
    internal enum RpcProjectType
    {
        Unity,
        World,
        Avatar
    }

    internal sealed class ProjectMetadata
    {
        internal string DisplayName;
        internal string ImageUrl;
    }

    internal sealed class ProjectContext
    {
        internal delegate bool MetadataFetcher(
            ProjectContext context,
            Action<ProjectMetadata> onSuccess,
            Action<string> onError);

        private static Func<ProjectContext> contextProvider;
        private static MetadataFetcher metadataFetcher;

        internal RpcProjectType ProjectType;
        internal string DisplayName;
        internal string BlueprintId;
        internal string Reason;
        internal string Identity;

        internal bool SupportsMetadata =>
            (ProjectType == RpcProjectType.World || ProjectType == RpcProjectType.Avatar) &&
            !string.IsNullOrEmpty(BlueprintId) &&
            metadataFetcher != null;

        internal static ProjectContext Resolve()
        {
            ProjectContext context = contextProvider?.Invoke();
            return context ?? CreateUnity("Standard Unity project detected.");
        }

        internal static ProjectContext CreateUnity(string reason)
        {
            Scene activeScene = SceneManager.GetActiveScene();
            string projectName = Path.GetFileName(Path.GetDirectoryName(Application.dataPath));
            if (string.IsNullOrWhiteSpace(projectName))
                projectName = string.IsNullOrWhiteSpace(Application.productName) ? "Unity Project" : Application.productName;

            string sceneIdentity = activeScene.IsValid() ? activeScene.path : string.Empty;
            return new ProjectContext
            {
                ProjectType = RpcProjectType.Unity,
                DisplayName = projectName,
                BlueprintId = string.Empty,
                Reason = reason,
                Identity = "unity:" + projectName + ":" + sceneIdentity
            };
        }

        internal static void RegisterProvider(Func<ProjectContext> provider, MetadataFetcher fetcher)
        {
            contextProvider = provider;
            metadataFetcher = fetcher;
            DiscordRpcService.QueueIntegrationRefresh();
        }

        internal static bool TryFetchMetadata(
            ProjectContext context,
            Action<ProjectMetadata> onSuccess,
            Action<string> onError)
        {
            return context != null && context.SupportsMetadata && metadataFetcher(context, onSuccess, onError);
        }
    }
}
