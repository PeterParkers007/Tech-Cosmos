#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor.PackageManager;

namespace TechCosmos.Hub.Editor
{
    public static class PackageReadmeLoader
    {
        private static readonly Dictionary<string, string> Cache = new();

        public static string GetReadmeAssetPath(PackageCatalogEntry entry, PackageCatalogFile catalog)
        {
            if (entry == null || catalog == null) return null;

            var upmAsset = TryGetUpmReadmeAssetPath(entry);
            if (!string.IsNullOrEmpty(upmAsset)) return upmAsset;

            var full = ResolveReadmeFullPath(entry, catalog);
            if (string.IsNullOrEmpty(full)) return null;

            return ToProjectRelativeAssetPath(full);
        }

        public static string LoadPreview(PackageCatalogEntry entry, PackageCatalogFile catalog)
        {
            if (entry == null) return string.Empty;

            var cacheKey = entry.id ?? entry.folder;
            if (Cache.TryGetValue(cacheKey, out var cached))
                return cached;

            var path = ResolveReadmeFullPath(entry, catalog);
            string text;

            if (!string.IsNullOrEmpty(path) && File.Exists(path))
                text = HubMarkdownRenderer.NormalizeLineEndings(File.ReadAllText(path));
            else
            {
                var root = PackageDetector.ResolvePackageRoot(entry, catalog);
                var sb = new StringBuilder();
                sb.AppendLine($"# {entry.displayName}");
                sb.AppendLine();
                if (!string.IsNullOrEmpty(entry.description))
                    sb.AppendLine(entry.description);
                sb.AppendLine();
                sb.AppendLine($"包 ID: `{entry.id}`");
                sb.AppendLine($"目录: `{root}/{entry.folder}`");
                sb.AppendLine();
                sb.AppendLine("（未找到 README.md。若已通过 Git UPM 导入，请点「解析 Packages」后刷新 Hub。）");
                text = sb.ToString();
            }

            Cache[cacheKey] = text;
            return text;
        }

        public static void ClearCache() => Cache.Clear();

        public static string ResolveReadmeFullPath(PackageCatalogEntry entry, PackageCatalogFile catalog)
        {
            if (entry == null) return null;

            var upm = TryGetUpmReadmeFullPath(entry);
            if (!string.IsNullOrEmpty(upm)) return upm;

            if (PackageAssetsImporter.IsInstalled(entry))
            {
                var embedded = Path.Combine(PackageAssetsImporter.GetPackageFullPath(entry), "README.md");
                if (File.Exists(embedded)) return embedded;
            }

            foreach (var dir in EnumeratePackageDirectories(entry, catalog))
            {
                var readme = Path.Combine(dir, "README.md");
                if (File.Exists(readme)) return readme;
            }

            return null;
        }

        private static IEnumerable<string> EnumeratePackageDirectories(PackageCatalogEntry entry, PackageCatalogFile catalog)
        {
            if (catalog != null && !string.IsNullOrEmpty(catalog.frameworkRoot))
                yield return Path.Combine(HubPaths.ProjectRoot, catalog.frameworkRoot, entry.folder);

            yield return Path.Combine(HubPaths.ProjectRoot, HubSettings.LegacyFrameworkRoot, entry.folder);
            yield return Path.Combine(HubPaths.ProjectRoot, HubSettings.AssetsPackageRoot, entry.folder);
        }

        private static string TryGetUpmReadmeFullPath(PackageCatalogEntry entry)
        {
            var info = FindPackageInfo(entry);
            if (info == null || string.IsNullOrEmpty(info.resolvedPath)) return null;

            var readme = Path.Combine(info.resolvedPath, "README.md");
            return File.Exists(readme) ? readme : null;
        }

        private static string TryGetUpmReadmeAssetPath(PackageCatalogEntry entry)
        {
            var info = FindPackageInfo(entry);
            if (info == null || string.IsNullOrEmpty(info.assetPath)) return null;

            var assetPath = $"{info.assetPath}/README.md".Replace('\\', '/');
            var full = Path.Combine(HubPaths.ProjectRoot, assetPath);
            return File.Exists(full) ? assetPath : null;
        }

        private static UnityEditor.PackageManager.PackageInfo FindPackageInfo(PackageCatalogEntry entry)
        {
            if (string.IsNullOrEmpty(entry?.id)) return null;
            try
            {
                return UnityEditor.PackageManager.PackageInfo.FindForPackageName(entry.id);
            }
            catch
            {
                return null;
            }
        }

        private static string ToProjectRelativeAssetPath(string fullPath)
        {
            if (string.IsNullOrEmpty(fullPath)) return null;

            var root = Path.GetFullPath(HubPaths.ProjectRoot).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            var full = Path.GetFullPath(fullPath);
            if (!full.StartsWith(root, StringComparison.OrdinalIgnoreCase)) return null;

            return full.Substring(root.Length + 1).Replace('\\', '/');
        }
    }
}
#endif
