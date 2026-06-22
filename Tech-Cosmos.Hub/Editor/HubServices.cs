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

        public static GlueTemplateMetaFile LoadTemplatesMeta()
        {
            if (!File.Exists(HubPaths.TemplatesMetaJson))
                return new GlueTemplateMetaFile();
            var json = File.ReadAllText(HubPaths.TemplatesMetaJson);
            return JsonUtility.FromJson<GlueTemplateMetaFile>(json) ?? new GlueTemplateMetaFile();
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

        public static void SetDependency(string packageId, string source)
        {
            if (string.IsNullOrEmpty(packageId))
                throw new ArgumentException("packageId 不能为空", nameof(packageId));
            if (string.IsNullOrEmpty(source))
                throw new ArgumentException("source 不能为空", nameof(source));

            if (HasDependency(packageId))
                RemoveDependency(packageId);
            AddDependency(packageId, source);
        }

        public static bool TryGetDependency(string packageId, out string source)
        {
            source = null;
            var deps = ReadDependencies();
            if (!deps.TryGetValue(packageId, out var value)) return false;
            source = value;
            return true;
        }
    }

    public enum PackagePresence
    {
        NotAvailable,
        LocalOnly,
        InManifest,
        AssetsEmbedded
    }

    public static class PackageDetector
    {
        public static PackagePresence GetPresence(PackageCatalogEntry entry, PackageCatalogFile catalog)
        {
            if (ManifestHelper.HasDependency(entry.id))
                return PackagePresence.InManifest;

            if (PackageAssetsImporter.IsInstalled(entry))
                return PackagePresence.AssetsEmbedded;

            var localPath = Path.Combine(HubPaths.ProjectRoot, catalog.frameworkRoot, entry.folder);
            var hasLocal = Directory.Exists(localPath) && File.Exists(Path.Combine(localPath, "package.json"));
            if (hasLocal) return PackagePresence.LocalOnly;

            var legacyPath = Path.Combine(HubPaths.ProjectRoot, HubSettings.LegacyFrameworkRoot, entry.folder);
            if (Directory.Exists(legacyPath) && File.Exists(Path.Combine(legacyPath, "package.json")))
                return PackagePresence.LocalOnly;

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

        public static string ResolvePackageRoot(PackageCatalogEntry entry, PackageCatalogFile catalog)
        {
            if (PackageAssetsImporter.IsInstalled(entry))
                return HubSettings.AssetsPackageRoot;
            if (Directory.Exists(Path.Combine(HubPaths.ProjectRoot, catalog.frameworkRoot, entry.folder)))
                return catalog.frameworkRoot;
            if (Directory.Exists(Path.Combine(HubPaths.ProjectRoot, HubSettings.LegacyFrameworkRoot, entry.folder)))
                return HubSettings.LegacyFrameworkRoot;
            return HubSettings.GetEffectiveFrameworkRoot(catalog);
        }

        public static bool CanImport(PackageCatalogEntry entry, PackageCatalogFile catalog)
        {
            return PackageDependencyResolver.Evaluate(entry, catalog).CanImport;
        }

        public static bool CanImportSelf(PackageCatalogEntry entry, PackageCatalogFile catalog)
        {
            if (entry == null || catalog == null) return false;

            var presence = GetPresence(entry, catalog);

            if (HubSettings.ImportMode == HubImportMode.AssetsEmbed)
            {
                if (presence == PackagePresence.AssetsEmbedded) return false;
                return !string.IsNullOrEmpty(entry.gitUrl) || presence == PackagePresence.LocalOnly
                       || PackageAssetsImporter.ResolveReadmeFullPath(entry, catalog) != null
                       || Directory.Exists(Path.Combine(HubPaths.ProjectRoot, catalog.frameworkRoot, entry.folder));
            }

            if (presence == PackagePresence.InManifest) return false;
            return !string.IsNullOrEmpty(entry.gitUrl) || presence == PackagePresence.LocalOnly;
        }

        public static bool CanUpdate(PackageCatalogEntry entry, PackageCatalogFile catalog)
        {
            if (entry == null || catalog == null) return false;

            var presence = GetPresence(entry, catalog);
            if (HubSettings.ImportMode == HubImportMode.AssetsEmbed)
                return presence == PackagePresence.AssetsEmbedded;

            return presence == PackagePresence.InManifest;
        }
    }

    public static class PackageInstaller
    {
        public static void ImportPackage(PackageCatalogEntry entry, PackageCatalogFile catalog)
        {
            ImportPackageWithDependencies(entry, catalog);
        }

        public static PackageBatchImportResult ImportPackageWithDependencies(
            PackageCatalogEntry entry, PackageCatalogFile catalog)
        {
            var plan = PackageDependencyResolver.Evaluate(entry, catalog);
            if (!plan.CanImport)
                throw new InvalidOperationException(plan.BlockReason ?? $"无法导入 {entry.displayName}。");

            var result = new PackageBatchImportResult();
            foreach (var pkg in plan.ImportOrder)
            {
                try
                {
                    ImportPackageSingle(pkg, catalog);
                    result.Succeeded.Add(pkg.displayName ?? pkg.id);
                }
                catch (Exception ex)
                {
                    result.Failed.Add($"{pkg.displayName}: {ex.Message}");
                    break;
                }
            }

            return result;
        }

        private static void ImportPackageSingle(PackageCatalogEntry entry, PackageCatalogFile catalog)
        {
            if (HubSettings.ImportMode == HubImportMode.AssetsEmbed)
            {
                PackageAssetsImporter.ImportToAssets(entry, catalog);
                return;
            }

            if (ManifestHelper.HasDependency(entry.id))
                return;

            if (!string.IsNullOrEmpty(entry.gitUrl))
            {
                ManifestHelper.AddDependency(entry.id, HubGitVersion.ResolveGitSource(entry));
                return;
            }

            var presence = PackageDetector.GetPresence(entry, catalog);
            if (presence == PackagePresence.LocalOnly)
            {
                var root = PackageDetector.ResolvePackageRoot(entry, catalog);
                var relative = $"file:../{root}/{entry.folder}".Replace("\\", "/");
                ManifestHelper.AddDependency(entry.id, relative);
                return;
            }

            throw new InvalidOperationException(
                $"无法 Git 导入 {entry.displayName}：请配置 gitUrl，或先将包放入 {catalog.frameworkRoot}。");
        }

        public static void RemovePackage(PackageCatalogEntry entry)
        {
            if (HubSettings.ImportMode == HubImportMode.AssetsEmbed)
                PackageAssetsImporter.RemoveFromAssets(entry);
            else
                RemoveFromManifest(entry.id);
        }

        public static void RemoveFromManifest(string packageId)
        {
            ManifestHelper.RemoveDependency(packageId);
        }

        public static bool TryResolveImportSource(
            PackageCatalogEntry entry, PackageCatalogFile catalog, out string source)
        {
            source = null;
            if (entry == null || catalog == null) return false;

            if (!string.IsNullOrEmpty(entry.gitUrl))
            {
                source = HubGitVersion.ResolveGitSource(entry);
                return true;
            }

            var presence = PackageDetector.GetPresence(entry, catalog);
            if (presence == PackagePresence.LocalOnly)
            {
                var root = PackageDetector.ResolvePackageRoot(entry, catalog);
                source = $"file:../{root}/{entry.folder}".Replace("\\", "/");
                return true;
            }

            var localPath = Path.Combine(HubPaths.ProjectRoot, catalog.frameworkRoot, entry.folder);
            if (Directory.Exists(localPath) && File.Exists(Path.Combine(localPath, "package.json")))
            {
                source = $"file:../{catalog.frameworkRoot}/{entry.folder}".Replace("\\", "/");
                return true;
            }

            var legacyPath = Path.Combine(HubPaths.ProjectRoot, HubSettings.LegacyFrameworkRoot, entry.folder);
            if (Directory.Exists(legacyPath) && File.Exists(Path.Combine(legacyPath, "package.json")))
            {
                source = $"file:../{HubSettings.LegacyFrameworkRoot}/{entry.folder}".Replace("\\", "/");
                return true;
            }

            return false;
        }

        public static void UpdatePackage(PackageCatalogEntry entry, PackageCatalogFile catalog)
        {
            if (!PackageDetector.CanUpdate(entry, catalog))
                throw new InvalidOperationException($"{entry.displayName} 未安装，无法更新。");

            if (HubSettings.ImportMode == HubImportMode.AssetsEmbed)
            {
                PackageAssetsImporter.UpdateInAssets(entry, catalog);
                return;
            }

            if (!TryResolveImportSource(entry, catalog, out var source))
                throw new InvalidOperationException(
                    $"无法更新 {entry.displayName}：请配置 gitUrl，或先将包放入 {catalog.frameworkRoot}。");

            ManifestHelper.SetDependency(entry.id, source);
        }

        public static PackageBatchImportResult ImportPackages(
            IEnumerable<PackageCatalogEntry> entries, PackageCatalogFile catalog)
        {
            var result = new PackageBatchImportResult();
            if (entries == null) return result;

            var order = PackageDependencyResolver.BuildBatchImportOrder(entries, catalog);
            foreach (var entry in order)
            {
                try
                {
                    ImportPackageSingle(entry, catalog);
                    result.Succeeded.Add(entry.displayName ?? entry.id);
                }
                catch (Exception ex)
                {
                    result.Failed.Add($"{entry.displayName}: {ex.Message}");
                }
            }

            foreach (var entry in entries)
            {
                if (entry == null) continue;
                var plan = PackageDependencyResolver.Evaluate(entry, catalog);
                if (!plan.CanImport && !result.Succeeded.Contains(entry.displayName ?? entry.id))
                    result.Skipped.Add(entry.displayName ?? entry.id);
            }

            return result;
        }
    }

    public sealed class PackageBatchImportResult
    {
        public readonly List<string> Succeeded = new();
        public readonly List<string> Skipped = new();
        public readonly List<string> Failed = new();
        public bool HasWork => Succeeded.Count > 0 || Failed.Count > 0;
    }
}
