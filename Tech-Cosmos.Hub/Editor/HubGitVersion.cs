#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;

namespace TechCosmos.Hub.Editor
{
    internal static class HubGitVersion
    {
        public const string LatestLabel = "最新 (main/HEAD)";

        private const string PrefsGitTagPrefix = "TechCosmos.Hub.GitTag.";
        private static readonly Dictionary<string, List<string>> TagCache = new();

        public static string GetSelectedTag(string packageId)
            => EditorPrefs.GetString(PrefsGitTagPrefix + packageId, "");

        public static void SetSelectedTag(string packageId, string tag)
            => EditorPrefs.SetString(PrefsGitTagPrefix + packageId, tag ?? "");

        public static string GetEffectiveSelectedTag(string packageId)
        {
            var pref = GetSelectedTag(packageId);
            if (!string.IsNullOrEmpty(pref))
                return pref;

            if (ManifestHelper.TryGetDependency(packageId, out var source)
                && TryParseTag(source, out var installedTag)
                && !string.IsNullOrEmpty(installedTag))
                return installedTag;

            return "";
        }

        public static string ResolveGitSource(PackageCatalogEntry entry)
        {
            if (entry == null || string.IsNullOrEmpty(entry.gitUrl))
                return null;

            return BuildSource(entry.gitUrl, GetEffectiveSelectedTag(entry.id));
        }

        public static string StripTag(string sourceUrl)
        {
            if (string.IsNullOrEmpty(sourceUrl))
                return sourceUrl;

            var hash = sourceUrl.IndexOf('#');
            return hash < 0 ? sourceUrl : sourceUrl.Substring(0, hash);
        }

        public static bool TryParseTag(string sourceUrl, out string tag)
        {
            tag = null;
            if (string.IsNullOrEmpty(sourceUrl))
                return false;

            var hash = sourceUrl.IndexOf('#');
            if (hash < 0 || hash >= sourceUrl.Length - 1)
                return false;

            tag = sourceUrl.Substring(hash + 1);
            return !string.IsNullOrEmpty(tag);
        }

        public static string BuildSource(string gitUrl, string tag)
        {
            var baseUrl = StripTag(gitUrl);
            return string.IsNullOrEmpty(tag) ? baseUrl : $"{baseUrl}#{tag}";
        }

        public static string GetRepositoryCloneUrl(string sourceUrl)
        {
            var url = StripTag(sourceUrl);
            var pathIdx = url.IndexOf("?path=", StringComparison.OrdinalIgnoreCase);
            return pathIdx < 0 ? url : url.Substring(0, pathIdx);
        }

        public static List<string> FetchTags(string gitUrl)
        {
            var repoUrl = GetRepositoryCloneUrl(gitUrl);
            if (string.IsNullOrEmpty(repoUrl))
                return new List<string>();

            if (TagCache.TryGetValue(repoUrl, out var cached))
                return new List<string>(cached);

            var tags = new List<string>();
            try
            {
                var output = RunGit($"ls-remote --tags \"{repoUrl}\"");
                foreach (var line in output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    var parts = line.Trim().Split('\t');
                    if (parts.Length < 2)
                        continue;

                    var refName = parts[1];
                    if (!refName.StartsWith("refs/tags/", StringComparison.Ordinal))
                        continue;
                    if (refName.EndsWith("^{}", StringComparison.Ordinal))
                        continue;

                    var tag = refName.Substring("refs/tags/".Length);
                    if (!string.IsNullOrEmpty(tag))
                        tags.Add(tag);
                }
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogWarning($"[Tech-Cosmos Hub] 获取 Git 标签失败 ({repoUrl}): {ex.Message}");
            }

            tags = SortTagsDescending(tags);
            TagCache[repoUrl] = tags;
            return new List<string>(tags);
        }

        public static void ClearTagCache() => TagCache.Clear();

        private static List<string> SortTagsDescending(List<string> tags)
        {
            return tags
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderByDescending(TryParseSemVer)
                .ThenByDescending(t => t, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private static Version TryParseSemVer(string tag)
        {
            if (string.IsNullOrEmpty(tag))
                return new Version(0, 0, 0);

            var normalized = tag.Trim();
            if (normalized.StartsWith("v", StringComparison.OrdinalIgnoreCase))
                normalized = normalized.Substring(1);

            var match = Regex.Match(normalized, @"^(\d+)(?:\.(\d+))?(?:\.(\d+))?");
            if (!match.Success)
                return new Version(0, 0, 0);

            var major = int.Parse(match.Groups[1].Value);
            var minor = match.Groups[2].Success ? int.Parse(match.Groups[2].Value) : 0;
            var patch = match.Groups[3].Success ? int.Parse(match.Groups[3].Value) : 0;
            return new Version(major, minor, patch);
        }

        private static string RunGit(string arguments)
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

            var stdout = process.StandardOutput.ReadToEnd();
            var stderr = process.StandardError.ReadToEnd();
            process.WaitForExit();
            if (process.ExitCode != 0)
                throw new InvalidOperationException($"git 执行失败 (code {process.ExitCode}): {stderr}");

            return stdout;
        }
    }
}
#endif
