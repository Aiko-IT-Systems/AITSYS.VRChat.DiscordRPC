using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using VRC.Core;

namespace AITSYS.VRCUnity.DiscordRPC
{
    [InitializeOnLoad]
    internal static class VrcProjectContextProvider
    {
        private const string WorldDescriptorType = "VRC.SDK3.Components.VRCSceneDescriptor";
        private const string AvatarDescriptorType = "VRC.SDK3.Avatars.Components.VRCAvatarDescriptor";

        static VrcProjectContextProvider()
        {
            ProjectContext.RegisterProvider(Resolve, TryFetchMetadata);
        }

        private static ProjectContext Resolve()
        {
            bool hasWorlds = FindLoadedType(WorldDescriptorType) != null;
            bool hasAvatars = FindLoadedType(AvatarDescriptorType) != null;
            if (!hasWorlds && !hasAvatars)
                return ProjectContext.CreateUnity("VRChat Base SDK detected; using standard Unity mode.");

            Component worldDescriptor = hasWorlds ? FindSceneComponent(WorldDescriptorType) : null;
            Component avatarDescriptor = hasAvatars ? FindSceneComponent(AvatarDescriptorType) : null;
            if (worldDescriptor != null && avatarDescriptor != null)
                return ProjectContext.CreateUnity("World and Avatar descriptors are both loaded; using standard Unity mode.");

            Component descriptor = worldDescriptor != null ? worldDescriptor : avatarDescriptor;
            if (descriptor == null)
                return ProjectContext.CreateUnity("VRChat SDK detected, but no matching descriptor is loaded.");

            RpcProjectType projectType = worldDescriptor != null ? RpcProjectType.World : RpcProjectType.Avatar;
            PipelineManager pipeline = descriptor.GetComponent<PipelineManager>();
            if (pipeline == null)
                pipeline = FindPipelineInScene(descriptor.gameObject.scene);
            if (pipeline == null)
                pipeline = FindSceneObject<PipelineManager>();
            if (pipeline == null)
                return ProjectContext.CreateUnity("VRChat descriptor found without a Pipeline Manager; using standard Unity mode.");

            string blueprintId = pipeline.blueprintId ?? string.Empty;
            Scene scene = descriptor.gameObject.scene;
            return new ProjectContext
            {
                ProjectType = projectType,
                DisplayName = descriptor.name,
                BlueprintId = blueprintId,
                Reason = projectType + " context is ready.",
                Identity = "vrc:" + projectType + ":" + descriptor.GetInstanceID() + ":" + blueprintId + ":" + scene.path
            };
        }

        private static bool TryFetchMetadata(
            ProjectContext context,
            Action<ProjectMetadata> onSuccess,
            Action<string> onError)
        {
            if (context.ProjectType == RpcProjectType.World)
            {
                var requested = new ApiWorld { id = context.BlueprintId };
                requested.Fetch(
                    container =>
                    {
                        ApiWorld world = container != null && container.Model is ApiWorld fetched ? fetched : requested;
                        onSuccess(new ProjectMetadata { DisplayName = world.name, ImageUrl = world.imageUrl });
                    },
                    container => onError(GetError(container)),
                    null,
                    true);
                return true;
            }

            if (context.ProjectType == RpcProjectType.Avatar)
            {
                var requested = new ApiAvatar { id = context.BlueprintId };
                requested.Fetch(
                    container =>
                    {
                        ApiAvatar avatar = container != null && container.Model is ApiAvatar fetched ? fetched : requested;
                        onSuccess(new ProjectMetadata { DisplayName = avatar.name, ImageUrl = avatar.imageUrl });
                    },
                    container => onError(GetError(container)),
                    null,
                    true);
                return true;
            }

            return false;
        }

        private static string GetError(ApiContainer container)
        {
            return container == null || string.IsNullOrEmpty(container.Error)
                ? "unknown API error"
                : container.Error;
        }

        private static Component FindSceneComponent(string fullTypeName)
        {
            Component fallback = null;
            Scene activeScene = SceneManager.GetActiveScene();
            MonoBehaviour[] behaviours = Resources.FindObjectsOfTypeAll<MonoBehaviour>();

            foreach (MonoBehaviour behaviour in behaviours)
            {
                if (behaviour == null || behaviour.GetType().FullName != fullTypeName || !IsLoadedSceneObject(behaviour))
                    continue;

                if (behaviour.gameObject.scene == activeScene)
                    return behaviour;

                if (fallback == null)
                    fallback = behaviour;
            }

            return fallback;
        }

        private static Type FindLoadedType(string fullTypeName)
        {
            foreach (System.Reflection.Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type type = assembly.GetType(fullTypeName, false);
                if (type != null)
                    return type;
            }

            return null;
        }

        private static PipelineManager FindPipelineInScene(Scene scene)
        {
            PipelineManager[] managers = Resources.FindObjectsOfTypeAll<PipelineManager>();
            foreach (PipelineManager manager in managers)
            {
                if (manager != null && IsLoadedSceneObject(manager) && manager.gameObject.scene == scene)
                    return manager;
            }

            return null;
        }

        private static T FindSceneObject<T>() where T : Component
        {
            T[] objects = Resources.FindObjectsOfTypeAll<T>();
            foreach (T item in objects)
            {
                if (item != null && IsLoadedSceneObject(item))
                    return item;
            }

            return null;
        }

        private static bool IsLoadedSceneObject(Component component)
        {
            Scene scene = component.gameObject.scene;
            return scene.IsValid() && scene.isLoaded && !EditorSceneManager.IsPreviewScene(scene);
        }
    }
}
