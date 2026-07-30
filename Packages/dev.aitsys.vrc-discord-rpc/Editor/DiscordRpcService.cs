using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using VRC.Core;

namespace AITSYS.VRCUnity.DiscordRPC
{
    [InitializeOnLoad]
    internal static class DiscordRpcService
    {
        private const double ContextPollInterval = 5d;
        private const double MetadataRetryInterval = 60d;

        private static readonly DiscordRpcNative.RichPresence Presence = new DiscordRpcNative.RichPresence();
        private static readonly string UnityVersion = Application.unityVersion;

        private static RpcState state = RpcState.EditMode;
        private static long timestamp = UnixTimestamp();
        private static string activeBlueprintId;
        private static string activeDescriptorName;
        private static string metadataImageUrl;
        private static string metadataDisplayName;
        private static string lastWarningKey;
        private static double nextPoll;
        private static double nextMetadataRetry;
        private static bool initialized;
        private static bool metadataRequestInFlight;
        private static bool refreshQueued;

        internal static string Status { get; private set; } = "Waiting for a supported VRChat context.";

        static DiscordRpcService()
        {
            EditorApplication.update += Tick;
            EditorApplication.hierarchyChanged += QueueRefresh;
            EditorSceneManager.sceneOpened += OnSceneOpened;
            EditorSceneManager.activeSceneChangedInEditMode += OnActiveSceneChanged;
            AssemblyReloadEvents.beforeAssemblyReload += Shutdown;
            EditorApplication.quitting += Shutdown;
            EditorApplication.delayCall += QueueRefresh;
        }

        internal static void SetState(RpcState newState, bool resetTime)
        {
            state = newState;
            if (resetTime)
                timestamp = UnixTimestamp();

            RefreshNow(false);
        }

        internal static void RefreshNow(bool fetchMetadata = true)
        {
            VRCUnityDiscordRPCSettings settings = VRCUnityDiscordRPCSettings.instance;
            if (!settings.enabled)
            {
                Status = "Disabled in Project Settings.";
                Shutdown();
                return;
            }

            if (!ProjectContext.TryFind(out ProjectContext context))
            {
                Status = context.Reason;
                Shutdown();
                return;
            }

            if (!EnsureInitialized())
                return;

            string blueprintId = context.Pipeline.blueprintId ?? string.Empty;
            if (blueprintId != activeBlueprintId)
            {
                activeBlueprintId = blueprintId;
                ResetMetadata();
                fetchMetadata = true;
            }

            activeDescriptorName = context.Descriptor.name;
            ApplyPresence(context);
            Status = "Connected for " + context.ProjectType + " project '" + context.Descriptor.name + "'.";

            if (fetchMetadata && !string.IsNullOrEmpty(blueprintId) && !metadataRequestInFlight)
                FetchMetadata(context.ProjectType, blueprintId);
        }

        internal static void ClearPresence()
        {
            Shutdown();
            Status = "Presence cleared.";
        }

        private static void Tick()
        {
            double now = EditorApplication.timeSinceStartup;
            if (now < nextPoll)
                return;

            nextPoll = now + ContextPollInterval;
            if (!VRCUnityDiscordRPCSettings.instance.enabled)
            {
                Shutdown();
                return;
            }

            if (!ProjectContext.TryFind(out ProjectContext context))
            {
                Status = context.Reason;
                Shutdown();
                return;
            }

            string blueprintId = context.Pipeline.blueprintId ?? string.Empty;
            bool changed = blueprintId != activeBlueprintId || context.Descriptor.name != activeDescriptorName;
            if (changed || (!HasMetadata() && !metadataRequestInFlight &&
                            !string.IsNullOrEmpty(blueprintId) && now >= nextMetadataRetry))
            {
                RefreshNow(true);
            }
        }

        private static bool EnsureInitialized()
        {
            if (initialized)
                return true;

            try
            {
                DiscordRpcNative.Initialize(
                    VRCUnityDiscordRPCSettings.ApplicationId,
                    default(DiscordRpcNative.EventHandlers));
                initialized = true;
                lastWarningKey = null;
                return true;
            }
            catch (Exception exception)
            {
                Status = "Discord RPC could not initialize: " + exception.Message;
                WarnOnce("initialize", Status);
                return false;
            }
        }

        private static void ApplyPresence(ProjectContext context)
        {
            Presence.details = "In Project: " + context.Descriptor.name;
            Presence.state = "Currently " + state.DisplayName();
            Presence.startTimestamp = timestamp;
            Presence.smallImageKey = "unity-white";
            Presence.smallImageText = "Unity " + UnityVersion;
            Presence.largeImageKey = metadataImageUrl;
            Presence.largeImageText = string.IsNullOrEmpty(metadataDisplayName)
                ? context.Descriptor.name
                : metadataDisplayName + " (" + context.Pipeline.blueprintId + ")";

            try
            {
                DiscordRpcNative.UpdatePresence(Presence);
            }
            catch (Exception exception)
            {
                Status = "Discord RPC update failed: " + exception.Message;
                WarnOnce("presence", Status);
            }
        }

        private static void FetchMetadata(VrcProjectType projectType, string blueprintId)
        {
            metadataRequestInFlight = true;
            nextMetadataRetry = EditorApplication.timeSinceStartup + MetadataRetryInterval;

            if (projectType == VrcProjectType.World)
            {
                var requested = new ApiWorld { id = blueprintId };
                requested.Fetch(
                    container => CompleteWorldFetch(blueprintId, requested, container),
                    container => FailMetadataFetch(blueprintId, container),
                    null,
                    true);
            }
            else
            {
                var requested = new ApiAvatar { id = blueprintId };
                requested.Fetch(
                    container => CompleteAvatarFetch(blueprintId, requested, container),
                    container => FailMetadataFetch(blueprintId, container),
                    null,
                    true);
            }
        }

        private static void CompleteWorldFetch(string blueprintId, ApiWorld requested, ApiContainer container)
        {
            metadataRequestInFlight = false;
            if (!IsCurrentBlueprint(blueprintId))
                return;

            ApiWorld world = container != null && container.Model is ApiWorld fetched ? fetched : requested;
            metadataImageUrl = world.imageUrl;
            metadataDisplayName = world.name;
            lastWarningKey = null;
            RefreshNow(false);
        }

        private static void CompleteAvatarFetch(string blueprintId, ApiAvatar requested, ApiContainer container)
        {
            metadataRequestInFlight = false;
            if (!IsCurrentBlueprint(blueprintId))
                return;

            ApiAvatar avatar = container != null && container.Model is ApiAvatar fetched ? fetched : requested;
            metadataImageUrl = avatar.imageUrl;
            metadataDisplayName = avatar.name;
            lastWarningKey = null;
            RefreshNow(false);
        }

        private static void FailMetadataFetch(string blueprintId, ApiContainer container)
        {
            metadataRequestInFlight = false;
            if (!IsCurrentBlueprint(blueprintId))
                return;

            string error = container == null || string.IsNullOrEmpty(container.Error)
                ? "unknown API error"
                : container.Error;
            Status = "Using local project details; VRChat metadata fetch failed and will retry later.";
            WarnOnce("metadata:" + blueprintId, Status + " " + error);
            RefreshNow(false);
        }

        private static bool IsCurrentBlueprint(string blueprintId)
        {
            return initialized &&
                   blueprintId == activeBlueprintId &&
                   ProjectContext.TryFind(out ProjectContext context) &&
                   context.Pipeline.blueprintId == blueprintId;
        }

        private static bool HasMetadata()
        {
            return !string.IsNullOrEmpty(metadataImageUrl) || !string.IsNullOrEmpty(metadataDisplayName);
        }

        private static void ResetMetadata()
        {
            metadataImageUrl = null;
            metadataDisplayName = null;
            metadataRequestInFlight = false;
            nextMetadataRetry = 0d;
        }

        private static void Shutdown()
        {
            if (!initialized)
                return;

            try
            {
                DiscordRpcNative.ClearPresence();
                DiscordRpcNative.Shutdown();
            }
            catch
            {
                // Native plugins can unload before Unity's managed domain callbacks.
            }
            finally
            {
                initialized = false;
                activeBlueprintId = null;
                activeDescriptorName = null;
                ResetMetadata();
            }
        }

        private static void QueueRefresh()
        {
            if (refreshQueued)
                return;

            refreshQueued = true;
            EditorApplication.delayCall += RefreshAfterEditorSettles;
        }

        private static void RefreshAfterEditorSettles()
        {
            refreshQueued = false;
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                QueueRefresh();
                return;
            }

            RefreshNow();
        }

        private static void OnSceneOpened(Scene scene, OpenSceneMode mode)
        {
            QueueRefresh();
        }

        private static void OnActiveSceneChanged(Scene previousScene, Scene nextScene)
        {
            QueueRefresh();
        }

        private static void WarnOnce(string key, string message)
        {
            if (lastWarningKey == key)
                return;

            lastWarningKey = key;
            Debug.LogWarning("[VRC Unity Discord RPC] " + message);
        }

        private static long UnixTimestamp()
        {
            return (long)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalSeconds;
        }
    }
}
