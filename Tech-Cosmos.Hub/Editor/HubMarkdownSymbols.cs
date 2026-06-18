#if UNITY_EDITOR
using System.Collections.Generic;

namespace TechCosmos.Hub.Editor
{
    /// <summary>
    /// README 中 Unity UI Toolkit 无法可靠渲染的 Emoji → 可显示符号。
    /// </summary>
    internal static class HubMarkdownSymbols
    {
        private readonly struct SymbolEntry
        {
            public readonly string Display;
            public readonly string StyleClass;

            public SymbolEntry(string display, string styleClass)
            {
                Display = display;
                StyleClass = styleClass;
            }
        }

        private static readonly Dictionary<string, SymbolEntry> Map = new()
        {
            ["✅"] = new("✓", "hub-md-symbol--ok"),
            ["✔"] = new("✓", "hub-md-symbol--ok"),
            ["✔️"] = new("✓", "hub-md-symbol--ok"),
            ["☑"] = new("✓", "hub-md-symbol--ok"),
            ["☑️"] = new("✓", "hub-md-symbol--ok"),
            ["❌"] = new("✗", "hub-md-symbol--fail"),
            ["✘"] = new("✗", "hub-md-symbol--fail"),
            ["☒"] = new("✗", "hub-md-symbol--fail"),
            ["⚠"] = new("!", "hub-md-symbol--warn"),
            ["⚠️"] = new("!", "hub-md-symbol--warn"),
            ["➕"] = new("+", null),
            ["➖"] = new("-", null),
            ["✨"] = new("*", null),
            ["⭐"] = new("*", "hub-md-symbol--star"),
            ["🌟"] = new("*", "hub-md-symbol--star"),
        };

        /// <summary>
        /// 将 Emoji 替换为可渲染符号。返回 false 表示应跳过（不显示方框）。
        /// </summary>
        public static bool TrySubstitute(string emoji, out string display, out string styleClass)
        {
            display = null;
            styleClass = null;
            if (string.IsNullOrEmpty(emoji)) return false;

            if (Map.TryGetValue(emoji, out var entry))
            {
                display = entry.Display;
                styleClass = entry.StyleClass;
                return true;
            }

            return false;
        }
    }
}
#endif
