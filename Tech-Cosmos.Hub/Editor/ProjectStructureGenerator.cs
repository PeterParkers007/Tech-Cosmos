#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace TechCosmos.Hub.Editor
{
    public sealed class ProjectStructureResult
    {
        public int FoldersCreated;
        public int FoldersSkipped;
        public int FilesCreated;
        public List<string> CreatedPaths = new();
        public List<string> Messages = new();
    }

    public sealed class ProjectStructureOptions
    {
        public string RootOverride;
        public bool WriteGitKeep = true;
        public bool WriteReadme = true;
        public bool WriteAsmdefStubs;
        public string ProjectDisplayName = "My Game";
    }

    public static class ProjectStructureGenerator
    {
        public static ProjectStructureResult Generate(
            ProjectStructurePreset preset,
            ProjectStructureOptions options = null)
        {
            options ??= new ProjectStructureOptions();
            var result = new ProjectStructureResult();

            if (preset == null)
                throw new ArgumentNullException(nameof(preset));

            GenerateRoot(preset.root, preset.folders, options, result);

            if (preset.extraRoots != null)
            {
                foreach (var extra in preset.extraRoots)
                {
                    if (extra?.folders == null || extra.folders.Length == 0) continue;
                    GenerateRoot(extra.root, extra.folders, options, result);
                }
            }

            AssetDatabase.Refresh();
            LogResult(result);
            return result;
        }

        private static void GenerateRoot(
            string root,
            string[] folders,
            ProjectStructureOptions options,
            ProjectStructureResult result)
        {
            var rootPath = NormalizeAssetPath(string.IsNullOrEmpty(options.RootOverride) ? root : options.RootOverride);
            EnsureAssetFolder(rootPath, result);

            if (options.WriteReadme)
                TryWriteReadme(rootPath, options, result);

            if (options.WriteAsmdefStubs)
                TryWriteAsmdefStubs(rootPath, result);

            var leafFolders = FindLeafFolders(folders);
            foreach (var relative in folders)
            {
                var folderPath = CombineAssetPath(rootPath, relative);
                if (AssetDatabase.IsValidFolder(folderPath))
                {
                    result.FoldersSkipped++;
                    continue;
                }

                EnsureAssetFolder(folderPath, result);
                result.FoldersCreated++;
                result.CreatedPaths.Add(folderPath + "/");
            }

            if (options.WriteGitKeep)
            {
                foreach (var leaf in leafFolders)
                {
                    var folderPath = CombineAssetPath(rootPath, leaf);
                    if (!AssetDatabase.IsValidFolder(folderPath)) continue;
                    WriteGitKeep(folderPath, result);
                }
            }
        }

        private static HashSet<string> FindLeafFolders(string[] folders)
        {
            var set = new HashSet<string>(folders.Where(f => !string.IsNullOrWhiteSpace(f)));
            var leaves = new HashSet<string>();

            foreach (var folder in set)
            {
                bool isParent = false;
                foreach (var other in set)
                {
                    if (other == folder) continue;
                    if (other.StartsWith(folder + "/", StringComparison.Ordinal))
                    {
                        isParent = true;
                        break;
                    }
                }

                if (!isParent)
                    leaves.Add(folder);
            }

            return leaves;
        }

        private static void TryWriteReadme(string rootPath, ProjectStructureOptions options, ProjectStructureResult result)
        {
            var readmeAsset = CombineAssetPath(rootPath, "README.md");
            var readmeFull = ToFullPath(readmeAsset);
            if (File.Exists(readmeFull)) return;

            var templatePath = Path.Combine(HubPaths.TemplatesDir, "ProjectReadme.txt");
            var content = File.Exists(templatePath)
                ? File.ReadAllText(templatePath).Replace("{{PROJECT_NAME}}", options.ProjectDisplayName)
                : $"# {options.ProjectDisplayName}\n";

            File.WriteAllText(readmeFull, content, Encoding.UTF8);
            result.FilesCreated++;
            result.CreatedPaths.Add(readmeAsset);
        }

        private static void TryWriteAsmdefStubs(string rootPath, ProjectStructureResult result)
        {
            WriteTemplateIfMissing(rootPath, "TechCosmos.Game.asmdef", "GameAsmdef.txt", result);

            var editorFolder = CombineAssetPath(rootPath, "Editor");
            if (AssetDatabase.IsValidFolder(editorFolder))
                WriteTemplateIfMissing(editorFolder, "TechCosmos.Game.Editor.asmdef", "GameEditorAsmdef.txt", result);
        }

        private static void WriteTemplateIfMissing(
            string folderAssetPath,
            string fileName,
            string templateName,
            ProjectStructureResult result)
        {
            var assetPath = CombineAssetPath(folderAssetPath, fileName);
            var fullPath = ToFullPath(assetPath);
            if (File.Exists(fullPath)) return;

            var templatePath = Path.Combine(HubPaths.TemplatesDir, templateName);
            if (!File.Exists(templatePath)) return;

            File.WriteAllText(fullPath, File.ReadAllText(templatePath), Encoding.UTF8);
            result.FilesCreated++;
            result.CreatedPaths.Add(assetPath);
        }

        private static void WriteGitKeep(string folderAssetPath, ProjectStructureResult result)
        {
            var assetPath = CombineAssetPath(folderAssetPath, ".gitkeep");
            var fullPath = ToFullPath(assetPath);
            if (File.Exists(fullPath)) return;

            File.WriteAllText(fullPath, string.Empty, Encoding.UTF8);
            result.FilesCreated++;
        }

        public static void EnsureAssetFolder(string assetPath, ProjectStructureResult result = null)
        {
            assetPath = NormalizeAssetPath(assetPath);
            if (AssetDatabase.IsValidFolder(assetPath)) return;

            var parts = assetPath.Split('/');
            if (parts.Length < 2 || parts[0] != "Assets")
                throw new InvalidOperationException($"无效 Asset 路径: {assetPath}");

            var current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                var next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                {
                    if (!AssetDatabase.IsValidFolder(current))
                        throw new InvalidOperationException($"父目录不存在: {current}");

                    var guid = AssetDatabase.CreateFolder(current, parts[i]);
                    if (string.IsNullOrEmpty(guid))
                        throw new InvalidOperationException($"创建文件夹失败: {next}");

                    result?.CreatedPaths.Add(next + "/");
                }

                current = next;
            }
        }

        public static string NormalizeAssetPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return "Assets/_Game";
            return path.Replace('\\', '/').TrimEnd('/');
        }

        public static string CombineAssetPath(string root, string relative)
            => NormalizeAssetPath(root) + "/" + relative.Replace('\\', '/').Trim('/');

        private static string ToFullPath(string assetPath)
            => Path.GetFullPath(Path.Combine(Application.dataPath, "..", assetPath));

        private static void LogResult(ProjectStructureResult result)
        {
            Debug.Log(
                $"[Tech-Cosmos Hub] 项目结构已生成：新建文件夹 {result.FoldersCreated}，跳过 {result.FoldersSkipped}，文件 {result.FilesCreated}。");
        }

        public static ProjectStructurePreset FindPreset(ProjectStructureFile file, string id)
        {
            if (file?.presets == null) return null;
            foreach (var p in file.presets)
                if (p.id == id) return p;
            return null;
        }
    }
}
#endif
