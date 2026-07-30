using System;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using VRC.Core;

namespace AITSYS.VRCUnity.DiscordRPC
{
    internal enum VrcProjectType
    {
        Unsupported,
        World,
        Avatar
    }

    internal sealed class ProjectContext
    {
        internal VrcProjectType ProjectType;
        internal Component Descriptor;
        internal PipelineManager Pipeline;
        internal string Reason;

        internal static VrcProjectType DetectInstalledSdk(out string reason)
        {
            bool hasWorlds = FindLoadedType("VRC.SDK3.Components.VRCSceneDescriptor") != null;
            bool hasAvatars = FindLoadedType("VRC.SDK3.Avatars.Components.VRCAvatarDescriptor") != null;

            if (hasWorlds && hasAvatars)
            {
                reason = "Both the Worlds and Avatars SDK packages are installed. This project is ambiguous.";
                return VrcProjectType.Unsupported;
            }

            if (hasWorlds)
            {
            reason = "VRChat Worlds SDK detected.";
            return VrcProjectType.World;
            }

            if (hasAvatars)
            {
            reason = "VRChat Avatars SDK detected.";
            return VrcProjectType.Avatar;
            }

            reason = "No supported VRChat SDK package is installed.";
            return VrcProjectType.Unsupported;
        }

        internal static bool TryFind(out ProjectContext context)
        {
            context = new ProjectContext();
            context.ProjectType = DetectInstalledSdk(out context.Reason);
            if (context.ProjectType == VrcProjectType.Unsupported)
                return false;

            string descriptorTypeName = context.ProjectType == VrcProjectType.World
                ? "VRC.SDK3.Components.VRCSceneDescriptor"
                : "VRC.SDK3.Avatars.Components.VRCAvatarDescriptor";

            context.Descriptor = FindSceneComponent(descriptorTypeName);
            if (context.Descriptor == null)
            {
                context.Reason = "The SDK is installed, but no matching descriptor exists in a loaded scene.";
                return false;
            }

            context.Pipeline = context.Descriptor.GetComponent<PipelineManager>();
            if (context.Pipeline == null)
                context.Pipeline = FindPipelineInScene(context.Descriptor.gameObject.scene);
            if (context.Pipeline == null)
                context.Pipeline = FindSceneObject<PipelineManager>();

            if (context.Pipeline == null)
            {
                context.Reason = "A descriptor was found, but no Pipeline Manager exists in a loaded scene.";
                return false;
            }

            context.Reason = context.ProjectType + " context is ready.";
            return true;
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
