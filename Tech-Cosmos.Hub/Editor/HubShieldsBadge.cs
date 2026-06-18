#if UNITY_EDITOR
using System;
using System.Text.RegularExpressions;

namespace TechCosmos.Hub.Editor
{
    internal static class HubShieldsBadge
    {
        private static readonly Regex ShieldsPathRegex = new(
            @"img\.shields\.io/badge/(.+?)(?:\.svg|\.png)?(?:\?|$)",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public static string Format(string imageUrl, string alt)
        {
            if (string.IsNullOrEmpty(imageUrl))
                return string.IsNullOrWhiteSpace(alt) ? "Badge" : alt;

            if (!TryParseShields(imageUrl, out var left, out var right))
                return string.IsNullOrWhiteSpace(alt) ? "Badge" : alt;

            return string.IsNullOrEmpty(right) ? left : $"{left} · {right}";
        }

        public static string ResolveBadgeClass(string imageUrl)
        {
            if (string.IsNullOrEmpty(imageUrl))
                return "hub-badge--local";

            if (!TryParseShields(imageUrl, out _, out _, out var color))
                return "hub-badge--local";

            return color switch
            {
                "brightgreen" or "green" => "hub-badge--installed",
                "blue" => "hub-badge--ready",
                "yellow" or "orange" => "hub-badge--pending",
                "red" => "hub-badge--missing",
                _ => "hub-badge--local"
            };
        }

        private static bool TryParseShields(string imageUrl, out string left, out string right)
        {
            return TryParseShields(imageUrl, out left, out right, out _);
        }

        private static bool TryParseShields(string imageUrl, out string left, out string right, out string color)
        {
            left = right = color = string.Empty;
            var match = ShieldsPathRegex.Match(imageUrl);
            if (!match.Success) return false;

            var body = DecodeToken(match.Groups[1].Value);
            var parts = body.Split('-');
            if (parts.Length < 2) return false;

            if (parts.Length == 2)
            {
                left = parts[0];
                right = parts[1];
                return true;
            }

            color = parts[parts.Length - 1].ToLowerInvariant();
            left = parts[0];
            right = DecodeToken(string.Join("-", parts, 1, parts.Length - 2));
            return true;
        }

        private static string DecodeToken(string token)
        {
            if (string.IsNullOrEmpty(token)) return string.Empty;
            return Uri.UnescapeDataString(token.Replace('_', ' ')).Replace("%2B", "+");
        }
    }
}
#endif
