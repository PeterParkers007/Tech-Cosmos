#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine.UIElements;
using UpmPackageInfo = UnityEditor.PackageManager.PackageInfo;

namespace TechCosmos.Hub.Editor
{
    internal static class HubTheme
    {
        private const string StylesheetRelative = "Editor/UI/TechCosmosHub.uss";
        private const string UxmlRelative = "Editor/UI/TechCosmosHub.uxml";
        public const string StylesheetGuid = "436c8836172968f488205d4b91707a69";

        public static bool IsLoaded { get; private set; }

        public static StyleSheet Load()
        {
            IsLoaded = false;

            foreach (var path in EnumerateStylesheetAssetPaths())
            {
                var sheet = LoadAtPath(path);
                if (sheet != null)
                {
                    IsLoaded = true;
                    return sheet;
                }
            }

            return null;
        }

        public static bool Attach(VisualElement root, ref StyleSheet current, bool forceReload)
        {
            if (root == null)
                return false;

            if (forceReload && current != null)
            {
                root.styleSheets.Remove(current);
                current = null;
            }

            if (current != null && root.styleSheets.Contains(current))
            {
                IsLoaded = true;
                return true;
            }

            current = Load();
            if (current == null)
                return false;

            if (!root.styleSheets.Contains(current))
                root.styleSheets.Add(current);
            return true;
        }

        public static bool TryCloneShell(VisualElement host, out VisualElement hubRoot)
        {
            hubRoot = null;
            if (host == null) return false;

            var uxmlPath = $"{HubPaths.HubAssetPath}/{UxmlRelative}".Replace('\\', '/');
            var tree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(uxmlPath);
            if (tree == null)
                return false;

            tree.CloneTree(host);
            hubRoot = host.Q<VisualElement>("hub-root");
            return hubRoot != null;
        }

        private static StyleSheet LoadAtPath(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath))
                return null;

            assetPath = assetPath.Replace('\\', '/');
            var sheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(assetPath);
            if (sheet != null)
                return sheet;

            var diskPath = Path.Combine(HubPaths.HubRoot, StylesheetRelative.Replace('/', Path.DirectorySeparatorChar));
            if (!File.Exists(diskPath))
                return null;

            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
            return AssetDatabase.LoadAssetAtPath<StyleSheet>(assetPath);
        }

        public static IEnumerable<string> EnumerateStylesheetAssetPaths()
        {
            var seen = new HashSet<string>();
            foreach (var path in CollectPaths())
            {
                if (string.IsNullOrEmpty(path)) continue;
                var normalized = path.Replace('\\', '/');
                if (seen.Add(normalized))
                    yield return normalized;
            }
        }

        private static IEnumerable<string> CollectPaths()
        {
            var results = new List<string>();
            var seen = new HashSet<string>();

            void Add(string path)
            {
                if (string.IsNullOrEmpty(path)) return;
                path = path.Replace('\\', '/');
                if (seen.Add(path))
                    results.Add(path);
            }

            Add(AssetDatabase.GUIDToAssetPath(StylesheetGuid));

            try
            {
                var info = UpmPackageInfo.FindForPackageName(HubCatalog.SelfPackageId);
                if (info != null && !string.IsNullOrEmpty(info.assetPath))
                    Add($"{info.assetPath}/{StylesheetRelative}");
            }
            catch
            {
                // ignored
            }

            Add($"{HubPaths.HubAssetPath}/{StylesheetRelative}");

            var guids = AssetDatabase.FindAssets("TechCosmosHub t:StyleSheet");
            foreach (var guid in guids)
                Add(AssetDatabase.GUIDToAssetPath(guid));

            return results;
        }
    }
}
#endif
