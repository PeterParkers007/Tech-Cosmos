#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace TechCosmos.Hub.Editor
{
    /// <summary>
    /// README Emoji 专用字体。正文走 USS/HubColors，避免 dynamic font 导致文字不可见。
    /// </summary>
    internal static class HubMarkdownFonts
    {
        private static readonly Dictionary<int, Font> EmojiFonts = new();

        public static void ApplyEmoji(Label label, string className = null)
        {
            if (label == null) return;

            className ??= JoinClasses(label);
            var size = ResolveSize(label, className);
            var font = GetEmojiFont(size);
            if (font == null) return;

            // 仅设置 unityFont；FontDefinition.FromFont(dynamic) 在部分 Unity 版本会导致 Label 空白。
            label.style.unityFont = font;
        }

        public static void ApplyEmojiTree(VisualElement root)
        {
            if (root == null) return;
            root.Query<Label>(className: "hub-md-emoji").ForEach(label => ApplyEmoji(label));
        }

        private static Font GetEmojiFont(int size)
        {
            if (EmojiFonts.TryGetValue(size, out var cached) && cached != null)
                return cached;

            cached = Font.CreateDynamicFontFromOSFont(EmojiFontNames(), size)
                ?? Font.CreateDynamicFontFromOSFont("Arial", size);
            EmojiFonts[size] = cached;
            return cached;
        }

        private static string[] EmojiFontNames()
        {
            switch (Application.platform)
            {
                case RuntimePlatform.WindowsEditor:
                case RuntimePlatform.WindowsPlayer:
                    return new[] { "Segoe UI Emoji", "Segoe UI Symbol" };
                case RuntimePlatform.OSXEditor:
                case RuntimePlatform.OSXPlayer:
                    return new[] { "Apple Color Emoji" };
                default:
                    return new[] { "Noto Color Emoji", "Segoe UI Emoji" };
            }
        }

        private static int ResolveSize(Label label, string className)
        {
            var fontSize = label.style.fontSize;
            if (fontSize.keyword != StyleKeyword.Undefined
                && fontSize.value.unit == LengthUnit.Pixel
                && fontSize.value.value > 0f)
                return Mathf.Clamp(Mathf.RoundToInt(fontSize.value.value), 10, 32);

            return SizeFromClassName(className);
        }

        private static int SizeFromClassName(string className)
        {
            if (string.IsNullOrEmpty(className)) return 13;
            if (className.Contains("hub-md-h1")) return 24;
            if (className.Contains("hub-md-h2")) return 19;
            if (className.Contains("hub-md-h3")) return 16;
            return 13;
        }

        private static string JoinClasses(VisualElement element)
        {
            if (element == null) return string.Empty;
            return string.Join(" ", element.GetClasses());
        }
    }
}
#endif
