#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine.UIElements;
using UpmPackageInfo = UnityEditor.PackageManager.PackageInfo;

namespace TechCosmos.Hub.Editor
{
    /// <summary>
    /// 仅负责从 UPM / 本地路径加载 TechCosmosHub.uss，不写入 inline 布局样式（inline 会覆盖 USS 导致排版退化）。
    /// </summary>
    internal static class HubTheme
    {
        private const string StylesheetRelative = "Editor/UI/TechCosmosHub.uss";

        public static StyleSheet Load()
        {
            foreach (var path in EnumerateStylesheetAssetPaths())
            {
                var sheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(path);
                if (sheet != null)
                    return sheet;
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
                return true;

            current = Load();
            if (current == null)
                return false;

            if (!root.styleSheets.Contains(current))
                root.styleSheets.Add(current);
            return true;
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
