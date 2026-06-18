#if UNITY_EDITOR
using System;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UIElements;

namespace TechCosmos.Hub.Editor
{
    /// <summary>
    /// README 行内 Markdown：Emoji 独立 Label、shields 徽章、链接/粗体/代码。
    /// </summary>
    internal static class HubMarkdownInline
    {
        private static readonly Regex LinkedImageRegex =
            new(@"\[!\[([^\]]*)\]\(([^)]+)\)\]\(([^)]+)\)", RegexOptions.Compiled);

        private static readonly Regex ImageRegex =
            new(@"!\[([^\]]*)\]\(([^)]+)\)", RegexOptions.Compiled);

        private static readonly Regex LinkRegex =
            new(@"(?<!!)\[([^\]]+)\]\(([^)]+)\)", RegexOptions.Compiled);

        private static readonly Regex CodeRegex = new(@"`([^`]+)`", RegexOptions.Compiled);
        private static readonly Regex BoldRegex = new(@"\*\*([^*]+)\*\*", RegexOptions.Compiled);
        private static readonly Regex ItalicRegex = new(@"(?<!\*)\*([^*]+)\*(?!\*)", RegexOptions.Compiled);

        public static bool ContainsEmoji(string text) => HubMarkdownEmoji.ContainsEmoji(text);

        public static VisualElement Render(string markdown, string className, string baseDirectory)
        {
            if (string.IsNullOrEmpty(markdown))
                return CreateBlockLabel(string.Empty, className);

            if (!NeedsFragmentedLayout(markdown))
                return CreateBlockLabel(markdown, className);

            var row = new VisualElement();
            row.AddToClassList("hub-md-inline");
            HubMarkdownLayout.ApplyInlineRow(row);
            ApplyContainerSpacing(row, className);

            if (className != null && className.Contains("hub-md-li-text"))
            {
                row.style.flexGrow = 1;
                row.style.flexShrink = 1;
                row.style.minWidth = 0;
            }

            ParseAndAppend(row, markdown, className, baseDirectory);
            if (row.childCount == 0)
                row.Add(CreateTextLabel(markdown, className));

            return row;
        }

        private static Label CreateBlockLabel(string text, string className)
        {
            var label = new Label(text) { text = text, enableRichText = false };
            label.style.whiteSpace = WhiteSpace.Normal;
            label.style.flexShrink = 0;
            label.style.flexGrow = 0;
            label.style.overflow = Overflow.Visible;
            HubColors.ApplyMarkdownBlock(label, className);
            return label;
        }

        private static void ApplyContainerSpacing(VisualElement element, string className)
        {
            if (element == null || string.IsNullOrEmpty(className)) return;

            if (className.Contains("hub-md-p") || className.Contains("hub-md-quote-line"))
                element.style.marginBottom = 10;
            else if (className.Contains("hub-md-h1"))
            {
                element.style.marginTop = 4;
                element.style.marginBottom = 12;
            }
            else if (className.Contains("hub-md-h2"))
            {
                element.style.marginTop = 20;
                element.style.marginBottom = 10;
            }
            else if (className.Contains("hub-md-h3"))
            {
                element.style.marginTop = 16;
                element.style.marginBottom = 8;
            }
        }

        private static bool NeedsFragmentedLayout(string markdown)
        {
            if (string.IsNullOrEmpty(markdown)) return false;
            if (HubMarkdownEmoji.ContainsEmoji(markdown)) return true;
            return LinkedImageRegex.IsMatch(markdown)
                || ImageRegex.IsMatch(markdown)
                || LinkRegex.IsMatch(markdown)
                || CodeRegex.IsMatch(markdown)
                || BoldRegex.IsMatch(markdown)
                || ItalicRegex.IsMatch(markdown);
        }

        private static void ParseAndAppend(
            VisualElement container,
            string text,
            string className,
            string baseDirectory,
            FontStyle fontStyle = FontStyle.Normal)
        {
            while (!string.IsNullOrEmpty(text))
            {
                if (!TryFindNextToken(text, out var index, out var length, out var kind, out var match))
                {
                    AppendPlainWithEmoji(container, text, className, fontStyle);
                    return;
                }

                if (index > 0)
                    AppendPlainWithEmoji(container, text.Substring(0, index), className, fontStyle);

                switch (kind)
                {
                    case InlineTokenKind.LinkedImage:
                        container.Add(CreateLinkedBadge(
                            match.Groups[1].Value,
                            match.Groups[2].Value,
                            match.Groups[3].Value,
                            baseDirectory));
                        break;
                    case InlineTokenKind.Image:
                        container.Add(CreateBadge(match.Groups[1].Value, match.Groups[2].Value, null, baseDirectory));
                        break;
                    case InlineTokenKind.Link:
                        container.Add(CreateLinkLabel(
                            match.Groups[1].Value,
                            match.Groups[2].Value,
                            className,
                            baseDirectory,
                            fontStyle));
                        break;
                    case InlineTokenKind.Code:
                        container.Add(CreateCodeLabel(match.Groups[1].Value, className));
                        break;
                    case InlineTokenKind.Bold:
                        ParseAndAppend(container, match.Groups[1].Value, className, baseDirectory, FontStyle.Bold);
                        break;
                    case InlineTokenKind.Italic:
                        ParseAndAppend(container, match.Groups[1].Value, className, baseDirectory, FontStyle.Italic);
                        break;
                }

                text = text.Substring(index + length);
            }
        }

        private static void AppendPlainWithEmoji(
            VisualElement container,
            string text,
            string className,
            FontStyle fontStyle)
        {
            if (string.IsNullOrEmpty(text)) return;

            var i = 0;
            while (i < text.Length)
            {
                if (TryReadEmoji(text, i, out var emoji, out var len))
                {
                    var emojiLabel = CreateEmojiLabel(emoji, className);
                    if (emojiLabel != null)
                        container.Add(emojiLabel);
                    i += len;
                    continue;
                }

                var start = i;
                i++;
                while (i < text.Length && !TryReadEmoji(text, i, out _, out _))
                    i++;

                var segment = text.Substring(start, i - start);
                if (!string.IsNullOrEmpty(segment))
                    container.Add(CreateTextLabel(segment, className, fontStyle));
            }
        }

        private static bool TryFindNextToken(
            string text,
            out int index,
            out int length,
            out InlineTokenKind kind,
            out Match match)
        {
            index = -1;
            length = 0;
            kind = InlineTokenKind.None;
            match = null;

            TryMatch(text, LinkedImageRegex, InlineTokenKind.LinkedImage, ref index, ref length, ref kind, ref match);
            TryMatch(text, ImageRegex, InlineTokenKind.Image, ref index, ref length, ref kind, ref match);
            TryMatch(text, LinkRegex, InlineTokenKind.Link, ref index, ref length, ref kind, ref match);
            TryMatch(text, CodeRegex, InlineTokenKind.Code, ref index, ref length, ref kind, ref match);
            TryMatch(text, BoldRegex, InlineTokenKind.Bold, ref index, ref length, ref kind, ref match);
            TryMatch(text, ItalicRegex, InlineTokenKind.Italic, ref index, ref length, ref kind, ref match);

            return index >= 0 && match != null;
        }

        private static void TryMatch(
            string text,
            Regex regex,
            InlineTokenKind tokenKind,
            ref int bestIndex,
            ref int bestLength,
            ref InlineTokenKind bestKind,
            ref Match bestMatch)
        {
            var m = regex.Match(text);
            if (!m.Success) return;
            if (bestIndex < 0 || m.Index < bestIndex)
            {
                bestIndex = m.Index;
                bestLength = m.Length;
                bestKind = tokenKind;
                bestMatch = m;
            }
        }

        private static bool TryReadEmoji(string text, int index, out string emoji, out int length)
            => HubMarkdownEmoji.TryReadAt(text, index, out emoji, out length);

        private static Label CreateTextLabel(string text, string className, FontStyle fontStyle = FontStyle.Normal)
        {
            var label = new Label(text) { text = text, enableRichText = false };
            if (fontStyle != FontStyle.Normal)
                label.style.unityFontStyleAndWeight = fontStyle;
            HubMarkdownRenderer.ConfigureInlineLabel(label, className);
            return label;
        }

        private static Label CreateEmojiLabel(string emoji, string className)
        {
            if (HubMarkdownSymbols.TrySubstitute(emoji, out var display, out var styleClass))
                return CreateSymbolLabel(display, className, styleClass);

            // 彩色 Emoji 在 UI Toolkit 动态字体下常显示为方框，未映射的 decorative emoji 直接省略。
            return null;
        }

        private static Label CreateSymbolLabel(string text, string className, string styleClass)
        {
            var label = new Label(text) { text = text, enableRichText = false };
            label.AddToClassList("hub-md-symbol");
            if (!string.IsNullOrEmpty(styleClass))
                label.AddToClassList(styleClass);
            HubMarkdownRenderer.ConfigureInlineLabel(label, className);
            HubColors.ApplyMarkdownSymbol(label, styleClass);
            return label;
        }

        private static Label CreateCodeLabel(string code, string className)
        {
            var label = new Label(code) { text = code, enableRichText = false };
            label.AddToClassList("hub-md-code-inline");
            HubMarkdownRenderer.ConfigureInlineLabel(label, className);
            return label;
        }

        private static Label CreateLinkLabel(
            string linkText,
            string url,
            string className,
            string baseDirectory,
            FontStyle fontStyle = FontStyle.Normal)
        {
            var label = new Label(linkText) { text = linkText, enableRichText = false };
            label.AddToClassList("hub-md-link-text");
            if (fontStyle != FontStyle.Normal)
                label.style.unityFontStyleAndWeight = fontStyle;
            HubMarkdownRenderer.ConfigureInlineLabel(label, className);
            HubColors.ApplyMarkdownLink(label);
            label.RegisterCallback<ClickEvent>(_ => HubMarkdownRenderer.OpenReadmeLink(url, baseDirectory));
            return label;
        }

        private static VisualElement CreateLinkedBadge(
            string alt,
            string imageUrl,
            string href,
            string baseDirectory)
        {
            return CreateBadge(alt, imageUrl, href, baseDirectory);
        }

        private static VisualElement CreateBadge(
            string alt,
            string imageUrl,
            string href,
            string baseDirectory)
        {
            var wrap = new VisualElement();
            wrap.AddToClassList("hub-md-badge-wrap");
            wrap.style.flexDirection = FlexDirection.Row;
            wrap.style.alignItems = Align.Center;

            var badgeClass = HubShieldsBadge.ResolveBadgeClass(imageUrl);
            var text = HubShieldsBadge.Format(imageUrl, alt);
            wrap.Add(HubUiFactory.Badge(text, badgeClass));

            if (!string.IsNullOrEmpty(href))
                wrap.RegisterCallback<ClickEvent>(_ => HubMarkdownRenderer.OpenReadmeLink(href, baseDirectory));

            return wrap;
        }

        private enum InlineTokenKind
        {
            None,
            LinkedImage,
            Image,
            Link,
            Code,
            Bold,
            Italic
        }
    }
}
#endif
