using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

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
        private static string activeContextIdentity;
        private static string activeBlueprintId;
        private static string metadataImageUrl;
        private static string metadataDisplayName;
        private static string lastWarningKey;
        private static double nextPoll;
        private static double nextMetadataRetry;
        private static bool initialized;
        private static bool metadataRequestInFlight;
        private static bool refreshQueued;
        private static double nextStatRotation;
        private static string lastPresenceSignature;

        internal static string Status { get; private set; } = "Waiting for a Unity project context.";

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

            SceneStatisticsCache.Refresh(true);
            ScheduleNextStatRotation();
            RefreshNow(false);
        }

        internal static void StatisticsSettingsChanged()
        {
            SceneStatisticsCache.Refresh(true);
            ScheduleNextStatRotation();
            RefreshNow(false);
        }

        internal static void QueueIntegrationRefresh()
        {
            QueueRefresh();
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

            ProjectContext context = ProjectContext.Resolve();
            if (!EnsureInitialized())
                return;

            if (context.Identity != activeContextIdentity)
            {
                activeContextIdentity = context.Identity;
                activeBlueprintId = context.BlueprintId ?? string.Empty;
                ResetMetadata();
                fetchMetadata = true;
            }

            ApplyPresence(context);
            if (nextStatRotation <= EditorApplication.timeSinceStartup)
                ScheduleNextStatRotation();

            Status = "Connected for " + context.ProjectType + " project '" + context.DisplayName + "'.";
            if (fetchMetadata && context.SupportsMetadata && !metadataRequestInFlight)
                FetchMetadata(context);
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
            VRCUnityDiscordRPCSettings settings = VRCUnityDiscordRPCSettings.instance;
            if (!settings.enabled)
            {
                Shutdown();
                return;
            }

            ProjectContext context = ProjectContext.Resolve();
            bool changed = context.Identity != activeContextIdentity;
            bool shouldRetryMetadata = context.SupportsMetadata &&
                                       !HasMetadata() &&
                                       !metadataRequestInFlight &&
                                       now >= nextMetadataRetry;
            if (changed || shouldRetryMetadata)
            {
                RefreshNow(true);
                return;
            }

            if (settings.showSceneStats && state.SupportsStatistics() && now >= nextStatRotation)
            {
                SceneStatisticsCache.Advance();
                ScheduleNextStatRotation();
                ApplyPresence(context);
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
            VRCUnityDiscordRPCSettings settings = VRCUnityDiscordRPCSettings.instance;
            string stateText = "Currently " + state.DisplayName();
            if (settings.showSceneStats && state.SupportsStatistics())
            {
                string statistic = SceneStatisticsCache.CurrentLine(settings);
                if (!string.IsNullOrEmpty(statistic))
                    stateText += " | " + statistic;
            }

            Presence.details = DiscordText.ClampActivityText("In Project: " + context.DisplayName);
            Presence.state = DiscordText.ClampActivityText(stateText);
            Presence.startTimestamp = timestamp;
            Presence.smallImageKey = "unity-white";
            Presence.smallImageText = DiscordText.ClampActivityText("Unity " + UnityVersion);
            Presence.largeImageKey = metadataImageUrl;

            string largeText = string.IsNullOrEmpty(metadataDisplayName)
                ? context.DisplayName
                : metadataDisplayName;
            if (!string.IsNullOrEmpty(context.BlueprintId) && !string.IsNullOrEmpty(metadataDisplayName))
                largeText += " (" + context.BlueprintId + ")";
            Presence.largeImageText = DiscordText.ClampActivityText(largeText);

            string signature = Presence.details + "\n" + Presence.state + "\n" + Presence.startTimestamp + "\n" +
                               Presence.largeImageKey + "\n" + Presence.largeImageText;
            if (signature == lastPresenceSignature)
                return;

            try
            {
                DiscordRpcNative.UpdatePresence(Presence);
                lastPresenceSignature = signature;
            }
            catch (Exception exception)
            {
                Status = "Discord RPC update failed: " + exception.Message;
                WarnOnce("presence", Status);
            }
        }

        private static void FetchMetadata(ProjectContext context)
        {
            string blueprintId = context.BlueprintId;
            metadataRequestInFlight = true;
            nextMetadataRetry = EditorApplication.timeSinceStartup + MetadataRetryInterval;

            bool started = ProjectContext.TryFetchMetadata(
                context,
                metadata => CompleteMetadataFetch(blueprintId, metadata),
                error => FailMetadataFetch(blueprintId, error));
            if (!started)
                metadataRequestInFlight = false;
        }

        private static void CompleteMetadataFetch(string blueprintId, ProjectMetadata metadata)
        {
            metadataRequestInFlight = false;
            if (!IsCurrentBlueprint(blueprintId))
                return;

            metadataImageUrl = metadata?.ImageUrl;
            metadataDisplayName = metadata?.DisplayName;
            lastWarningKey = null;
            RefreshNow(false);
        }

        private static void FailMetadataFetch(string blueprintId, string error)
        {
            metadataRequestInFlight = false;
            if (!IsCurrentBlueprint(blueprintId))
                return;

            string message = string.IsNullOrEmpty(error) ? "unknown API error" : error;
            Status = "Using local project details; VRChat metadata fetch failed and will retry later.";
            WarnOnce("metadata:" + blueprintId, Status + " " + message);
            RefreshNow(false);
        }

        private static bool IsCurrentBlueprint(string blueprintId)
        {
            ProjectContext context = ProjectContext.Resolve();
            return initialized &&
                   blueprintId == activeBlueprintId &&
                   context.BlueprintId == blueprintId;
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
            if (initialized)
            {
                try
                {
                    DiscordRpcNative.ClearPresence();
                    DiscordRpcNative.Shutdown();
                }
                catch
                {
                    // Native plugins can unload before Unity's managed domain callbacks.
                }
            }

            initialized = false;
            activeContextIdentity = null;
            activeBlueprintId = null;
            lastPresenceSignature = null;
            ResetMetadata();
        }

        private static void QueueRefresh()
        {
            SceneStatisticsCache.MarkDirty();
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

        private static void ScheduleNextStatRotation()
        {
            int seconds = Mathf.Clamp(VRCUnityDiscordRPCSettings.instance.statCycleSeconds, 5, 120);
            nextStatRotation = EditorApplication.timeSinceStartup + seconds;
        }
    }
}
