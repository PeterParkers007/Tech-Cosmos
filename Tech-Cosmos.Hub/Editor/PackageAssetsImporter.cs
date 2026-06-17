#if UNITY_EDITOR
using System;
using System.Diagnostics;
using System.IO;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace TechCosmos.Hub.Editor
{
    public static class PackageAssetsImporter
    {
        public static void EnsureRoot()
        {
            var assetPath = HubSettings.AssetsPackageRoot;
            var full = Path.Combine(HubPaths.ProjectRoot, assetPath);
            if (!Directory.Exists(full))
            {
                Directory.CreateDirectory(full);
                AssetDatabase.Refresh();
            }
        }

        public static string GetPackageAssetPath(PackageCatalogEntry entry)
            => $"{HubSettings.AssetsPackageRoot}/{entry.folder}".Replace("\\", "/");

        public static string GetPackageFullPath(PackageCatalogEntry entry)
            => Path.Combine(HubPaths.ProjectRoot, HubSettings.AssetsPackageRoot, entry.folder);

        public static bool IsInstalled(PackageCatalogEntry entry)
        {
            if (entry == null) return false;
            var path = GetPackageFullPath(entry);
            return Directory.Exists(path) && File.Exists(Path.Combine(path, "package.json"));
        }

        public static void ImportToAssets(PackageCatalogEntry entry, PackageCatalogFile catalog)
        {
            if (entry == null) throw new ArgumentNullException(nameof(entry));
            if (IsInstalled(entry))
                throw new InvalidOperationException($"{entry.displayName} 已在 {HubSettings.AssetsPackageRoot} 中。");

            EnsureRoot();
            var target = GetPackageFullPath(entry);

            if (TryCopyFromPath(ResolveSourcePath(entry, catalog), target))
            {
                AssetDatabase.Refresh();
                Debug.Log($"[Tech-Cosmos Hub] 已复制到 Assets: {GetPackageAssetPath(entry)}");
                return;
            }

            if (!string.IsNullOrEmpty(entry.gitUrl))
            {
                CloneGitRepository(entry.gitUrl, target);
                AssetDatabase.Refresh();
                Debug.Log($"[Tech-Cosmos Hub] 已从 Git 克隆到 Assets: {GetPackageAssetPath(entry)}");
                return;
            }

            throw new InvalidOperationException(
                $"无法嵌入 {entry.displayName}：无本地源目录且 catalog 未配置 gitUrl。");
        }

        public static void RemoveFromAssets(PackageCatalogEntry entry)
        {
            if (entry == null || !IsInstalled(entry)) return;

            var assetPath = GetPackageAssetPath(entry);
            if (!AssetDatabase.DeleteAsset(assetPath))
            {
                var full = GetPackageFullPath(entry);
                if (Directory.Exists(full))
                    Directory.Delete(full, true);
                var meta = full + ".meta";
                if (File.Exists(meta)) File.Delete(meta);
                AssetDatabase.Refresh();
            }

            Debug.Log($"[Tech-Cosmos Hub] 已从 Assets 移除: {assetPath}");
        }

        public static string ResolveReadmeFullPath(PackageCatalogEntry entry, PackageCatalogFile catalog)
            => PackageReadmeLoader.ResolveReadmeFullPath(entry, catalog);

        private static string ResolveSourcePath(PackageCatalogEntry entry, PackageCatalogFile catalog)
        {
            var candidates = new[]
            {
                Path.Combine(HubPaths.ProjectRoot, catalog.frameworkRoot, entry.folder),
                Path.Combine(HubPaths.ProjectRoot, "Assets", "Framework", entry.folder)
            };

            foreach (var path in candidates)
            {
                if (Directory.Exists(path) && File.Exists(Path.Combine(path, "package.json")))
                    return path;
            }

            return null;
        }

        private static bool TryCopyFromPath(string source, string target)
        {
            if (string.IsNullOrEmpty(source) || !Directory.Exists(source)) return false;
            CopyDirectory(source, target);
            return true;
        }

        private static void CopyDirectory(string sourceDir, string targetDir)
        {
            Directory.CreateDirectory(targetDir);

            foreach (var file in Directory.GetFiles(sourceDir))
            {
                var name = Path.GetFileName(file);
                if (name == ".DS_Store") continue;
                File.Copy(file, Path.Combine(targetDir, name), true);
            }

            foreach (var dir in Directory.GetDirectories(sourceDir))
            {
                var name = Path.GetFileName(dir);
                if (name is ".git" or ".svn") continue;
                CopyDirectory(dir, Path.Combine(targetDir, name));
            }
        }

        private static void CloneGitRepository(string gitUrl, string targetDir)
        {
            var tempParent = Path.Combine(Path.GetTempPath(), "TechCosmosHub", Guid.NewGuid().ToString("N"));
            var tempClone = Path.Combine(tempParent, "repo");
            Directory.CreateDirectory(tempParent);

            try
            {
                RunGit($"clone --depth 1 \"{gitUrl}\" \"{tempClone}\"");
                if (!Directory.Exists(tempClone))
                    throw new InvalidOperationException($"Git 克隆失败: {gitUrl}");

                CopyDirectory(tempClone, targetDir);
            }
            finally
            {
                try
                {
                    if (Directory.Exists(tempParent))
                        Directory.Delete(tempParent, true);
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[Hub] 清理临时目录失败: {ex.Message}");
                }
            }
        }

        private static void RunGit(string arguments)
        {
            var psi = new ProcessStartInfo
            {
                FileName = "git",
                Arguments = arguments,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using var process = Process.Start(psi);
            if (process == null)
                throw new InvalidOperationException("无法启动 git，请确认已安装 Git 并加入 PATH。");

            var stderr = process.StandardError.ReadToEnd();
            process.WaitForExit();
            if (process.ExitCode != 0)
                throw new InvalidOperationException($"git 执行失败 (code {process.ExitCode}): {stderr}");
        }
    }
}
#endif
