using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

namespace TechCosmos.Hub.Editor
{
    public static class HubDataLoader
    {
        public static PackageCatalogFile LoadCatalog()
        {
            if (!File.Exists(HubPaths.CatalogJson))
                return new PackageCatalogFile();
            var json = File.ReadAllText(HubPaths.CatalogJson);
            return JsonUtility.FromJson<PackageCatalogFile>(json) ?? new PackageCatalogFile();
        }

        public static GlueRecipeFile LoadRecipes()
        {
            if (!File.Exists(HubPaths.RecipesJson))
                return new GlueRecipeFile();
            var json = File.ReadAllText(HubPaths.RecipesJson);
            return JsonUtility.FromJson<GlueRecipeFile>(json) ?? new GlueRecipeFile();
        }

        public static ProjectStructureFile LoadProjectStructure()
        {
            if (!File.Exists(HubPaths.ProjectStructureJson))
                return new ProjectStructureFile();
            var json = File.ReadAllText(HubPaths.ProjectStructureJson);
            return JsonUtility.FromJson<ProjectStructureFile>(json) ?? new ProjectStructureFile();
        }
    }

    public static class ManifestHelper
    {
        public static Dictionary<string, string> ReadDependencies()
        {
            var result = new Dictionary<string, string>();
            if (!File.Exists(HubPaths.ManifestPath)) return result;

            var text = File.ReadAllText(HubPaths.ManifestPath);
            var match = Regex.Match(text, "\"dependencies\"\\s*:\\s*\\{([^}]*)\\}", RegexOptions.Singleline);
            if (!match.Success) return result;

            var body = match.Groups[1].Value;
            var pairRegex = new Regex("\"([^\"]+)\"\\s*:\\s*\"([^\"]*)\"");
            foreach (Match m in pairRegex.Matches(body))
                result[m.Groups[1].Value] = m.Groups[2].Value;

            return result;
        }

        public static bool HasDependency(string packageId) => ReadDependencies().ContainsKey(packageId);

        public static void AddDependency(string packageId, string source)
        {
            if (HasDependency(packageId)) return;

            var text = File.ReadAllText(HubPaths.ManifestPath);
            var insert = $"    \"{packageId}\": \"{source}\",\n";
            var idx = text.IndexOf("\"dependencies\"", StringComparison.Ordinal);
            if (idx < 0) throw new InvalidOperationException("manifest.json 缺少 dependencies 节点");

            var braceStart = text.IndexOf('{', idx);
            var insertAt = braceStart + 1;
            text = text.Insert(insertAt, "\n" + insert);
            File.WriteAllText(HubPaths.ManifestPath, text, Encoding.UTF8);
        }

        public static void RemoveDependency(string packageId)
        {
            if (!HasDependency(packageId)) return;

            var text = File.ReadAllText(HubPaths.ManifestPath);
            var pattern = $"\\s*\"{Regex.Escape(packageId)}\"\\s*:\\s*\"[^\"]*\"\\s*,?\\n?";
            text = Regex.Replace(text, pattern, string.Empty);
            File.WriteAllText(HubPaths.ManifestPath, text, Encoding.UTF8);
        }
    }

    public enum PackagePresence
    {
        NotAvailable,
        LocalOnly,
        InManifest
    }

    public static class PackageDetector
    {
        public static PackagePresence GetPresence(PackageCatalogEntry entry, PackageCatalogFile catalog)
        {
            var localPath = Path.Combine(HubPaths.ProjectRoot, catalog.frameworkRoot, entry.folder);
            var hasLocal = Directory.Exists(localPath) && File.Exists(Path.Combine(localPath, "package.json"));
            var inManifest = ManifestHelper.HasDependency(entry.id);

            if (inManifest) return PackagePresence.InManifest;
            if (hasLocal) return PackagePresence.LocalOnly;
            return PackagePresence.NotAvailable;
        }

        public static bool IsRequirementMet(string packageId, PackageCatalogFile catalog)
        {
            if (catalog.packages == null) return false;
            foreach (var pkg in catalog.packages)
            {
                if (pkg.id != packageId) continue;
                var presence = GetPresence(pkg, catalog);
                return presence != PackagePresence.NotAvailable;
            }
            return false;
        }
    }

    public static class PackageInstaller
    {
        public static void ImportPackage(PackageCatalogEntry entry, PackageCatalogFile catalog)
        {
            var presence = PackageDetector.GetPresence(entry, catalog);

            if (presence == PackagePresence.LocalOnly || presence == PackagePresence.InManifest)
            {
                var relative = $"file:../{catalog.frameworkRoot}/{entry.folder}".Replace("\\", "/");
                ManifestHelper.AddDependency(entry.id, relative);
                return;
            }

            if (!string.IsNullOrEmpty(entry.gitUrl))
            {
                ManifestHelper.AddDependency(entry.id, entry.gitUrl);
                return;
            }

            throw new InvalidOperationException($"无法导入 {entry.displayName}：本地无目录且未配置 gitUrl。");
        }

        public static void RemoveFromManifest(string packageId)
        {
            ManifestHelper.RemoveDependency(packageId);
        }
    }
}
