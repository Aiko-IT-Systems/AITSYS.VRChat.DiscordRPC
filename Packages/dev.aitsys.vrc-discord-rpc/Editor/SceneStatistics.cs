using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace AITSYS.VRCUnity.DiscordRPC
{
    [Flags]
    internal enum RpcStatFlags
    {
        None = 0,
        Objects = 1 << 0,
        MeshesAndRenderers = 1 << 1,
        TrianglesAndMaterials = 1 << 2,
        Lights = 1 << 3,
        LastBuildSize = 1 << 4,
        All = Objects | MeshesAndRenderers | TrianglesAndMaterials | Lights | LastBuildSize
    }

    internal sealed class SceneStatistics
    {
        internal long ObjectCount { get; set; }
        internal long MeshCount { get; set; }
        internal long RendererCount { get; set; }
        internal long TriangleCount { get; set; }
        internal long MaterialCount { get; set; }
        internal long LightCount { get; set; }

        internal static SceneStatistics Capture()
        {
            var statistics = new SceneStatistics();
            var materialIds = new HashSet<int>();
            var transforms = new Stack<Transform>();

            for (int sceneIndex = 0; sceneIndex < SceneManager.sceneCount; sceneIndex++)
            {
                Scene scene = SceneManager.GetSceneAt(sceneIndex);
                if (!scene.IsValid() || !scene.isLoaded || EditorSceneManager.IsPreviewScene(scene))
                    continue;

                foreach (GameObject root in scene.GetRootGameObjects())
                    transforms.Push(root.transform);
            }

            while (transforms.Count > 0)
            {
                Transform transform = transforms.Pop();
                GameObject gameObject = transform.gameObject;
                statistics.ObjectCount++;

                for (int childIndex = 0; childIndex < transform.childCount; childIndex++)
                    transforms.Push(transform.GetChild(childIndex));

                foreach (MeshFilter meshFilter in gameObject.GetComponents<MeshFilter>())
                {
                    statistics.MeshCount++;
                    statistics.TriangleCount += CountTriangles(meshFilter.sharedMesh);
                }

                foreach (SkinnedMeshRenderer skinnedRenderer in gameObject.GetComponents<SkinnedMeshRenderer>())
                {
                    statistics.MeshCount++;
                    statistics.TriangleCount += CountTriangles(skinnedRenderer.sharedMesh);
                }

                Renderer[] renderers = gameObject.GetComponents<Renderer>();
                statistics.RendererCount += renderers.Length;
                foreach (Renderer renderer in renderers)
                {
                    foreach (Material material in renderer.sharedMaterials)
                    {
                        if (material != null)
                            materialIds.Add(material.GetInstanceID());
                    }
                }

                statistics.LightCount += gameObject.GetComponents<Light>().Length;
            }

            statistics.MaterialCount = materialIds.Count;
            return statistics;
        }

        internal List<string> BuildLines(RpcStatFlags flags, long lastBuildSizeBytes)
        {
            var lines = new List<string>(5);

            if ((flags & RpcStatFlags.Objects) != 0)
                lines.Add(FormatCount(ObjectCount) + " Objects");

            if ((flags & RpcStatFlags.MeshesAndRenderers) != 0)
                lines.Add(FormatCount(MeshCount) + " Meshes | " + FormatCount(RendererCount) + " Renderers");

            if ((flags & RpcStatFlags.TrianglesAndMaterials) != 0)
                lines.Add(FormatCount(TriangleCount) + " Triangles | " + FormatCount(MaterialCount) + " Materials");

            if ((flags & RpcStatFlags.Lights) != 0 && LightCount > 0)
                lines.Add(FormatCount(LightCount) + " Lights");

            if ((flags & RpcStatFlags.LastBuildSize) != 0 && lastBuildSizeBytes > 0)
                lines.Add("Build Size: " + FormatBytes(lastBuildSizeBytes));

            return lines;
        }

        internal static string FormatCount(long value)
        {
            return value.ToString("N0", CultureInfo.InvariantCulture);
        }

        internal static string FormatBytes(long bytes)
        {
            string[] units = { "B", "KB", "MB", "GB", "TB" };
            double value = Math.Max(0, bytes);
            int unitIndex = 0;
            while (value >= 1024d && unitIndex < units.Length - 1)
            {
                value /= 1024d;
                unitIndex++;
            }

            string format = unitIndex == 0 ? "0" : "0.##";
            return value.ToString(format, CultureInfo.InvariantCulture) + " " + units[unitIndex];
        }

        private static long CountTriangles(Mesh mesh)
        {
            if (mesh == null)
                return 0;

            long triangles = 0;
            for (int subMeshIndex = 0; subMeshIndex < mesh.subMeshCount; subMeshIndex++)
            {
                if (mesh.GetTopology(subMeshIndex) == MeshTopology.Triangles)
                    triangles += (long)mesh.GetIndexCount(subMeshIndex) / 3;
            }

            return triangles;
        }
    }

    internal static class SceneStatisticsCache
    {
        private static SceneStatistics statistics;
        private static bool dirty = true;
        private static bool refreshOnNextRead;
        private static int lineIndex;

        internal static string CurrentLine(VRCUnityDiscordRPCSettings settings)
        {
            List<string> lines = GetLines(settings);
            if (lines.Count == 0)
                return string.Empty;

            lineIndex %= lines.Count;
            return lines[lineIndex];
        }

        internal static void Advance()
        {
            lineIndex++;
            refreshOnNextRead |= dirty;
        }

        internal static void MarkDirty()
        {
            dirty = true;
        }

        internal static void Refresh(bool resetCycle)
        {
            dirty = true;
            refreshOnNextRead = true;
            if (resetCycle)
                lineIndex = 0;
        }

        internal static void RecordBuildArtifact(string path)
        {
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
                return;

            try
            {
                RecordBuildSize(new FileInfo(path).Length);
            }
            catch (IOException)
            {
                // A successful SDK callback matters more than an optional file-size statistic.
            }
            catch (UnauthorizedAccessException)
            {
                // Some custom builders clean or lock their artifact before this callback returns.
            }
        }

        internal static void RecordBuildSize(long bytes)
        {
            if (bytes <= 0)
                return;

            VRCUnityDiscordRPCSettings.instance.SetLastBuildSize(bytes);
            lineIndex = 0;
        }

        private static List<string> GetLines(VRCUnityDiscordRPCSettings settings)
        {
            if (statistics == null || refreshOnNextRead)
            {
                statistics = SceneStatistics.Capture();
                dirty = false;
                refreshOnNextRead = false;
            }

            return statistics.BuildLines(settings.statFlags, settings.lastBuildSizeBytes);
        }
    }
}
