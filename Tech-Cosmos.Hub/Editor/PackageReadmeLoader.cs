using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace TechCosmos.Hub.Editor
{
    public static class PackageReadmeLoader
    {
        private static readonly Dictionary<string, string> Cache = new();

        public static string GetReadmeAssetPath(PackageCatalogEntry entry, PackageCatalogFile catalog)
        {
            if (entry == null || catalog == null) return null;
            var full = Path.Combine(HubPaths.ProjectRoot, catalog.frameworkRoot, entry.folder, "README.md");
            if (!File.Exists(full)) return null;
            return $"{catalog.frameworkRoot}/{entry.folder}/README.md".Replace("\\", "/");
        }

        public static string LoadPreview(PackageCatalogEntry entry, PackageCatalogFile catalog, int maxChars = 12000)
        {
            if (entry == null) return string.Empty;

            var cacheKey = entry.id ?? entry.folder;
            if (Cache.TryGetValue(cacheKey, out var cached))
                return cached;

            var path = Path.Combine(HubPaths.ProjectRoot, catalog.frameworkRoot, entry.folder, "README.md");
            string text;

            if (File.Exists(path))
            {
                text = File.ReadAllText(path);
                text = SimplifyMarkdown(text);
            }
            else
            {
                var sb = new StringBuilder();
                sb.AppendLine($"# {entry.displayName}");
                sb.AppendLine();
                if (!string.IsNullOrEmpty(entry.description))
                    sb.AppendLine(entry.description);
                sb.AppendLine();
                sb.AppendLine($"包 ID: {entry.id}");
                sb.AppendLine($"目录: {catalog.frameworkRoot}/{entry.folder}");
                sb.AppendLine();
                sb.AppendLine("（该包目录下暂无 README.md，以上为目录简介。）");
                text = sb.ToString();
            }

            if (text.Length > maxChars)
                text = text.Substring(0, maxChars) + "\n\n…（已截断，点击「打开 README」查看完整文件）";

            Cache[cacheKey] = text;
            return text;
        }

        public static void ClearCache() => Cache.Clear();

        private static string SimplifyMarkdown(string markdown)
        {
            if (string.IsNullOrEmpty(markdown)) return string.Empty;

            var lines = markdown.Replace("\r\n", "\n").Split('\n');
            var sb = new StringBuilder();

            foreach (var raw in lines)
            {
                var line = raw;

                if (Regex.IsMatch(line, @"^#{1,6}\s+"))
                    line = Regex.Replace(line, @"^#{1,6}\s+", "▎ ");

                line = Regex.Replace(line, @"\*\*([^*]+)\*\*", "$1");
                line = Regex.Replace(line, @"`([^`]+)`", "$1");
                line = Regex.Replace(line, @"\[([^\]]+)\]\([^)]+\)", "$1");

                sb.AppendLine(line);
            }

            return sb.ToString().Trim();
        }
    }
}
