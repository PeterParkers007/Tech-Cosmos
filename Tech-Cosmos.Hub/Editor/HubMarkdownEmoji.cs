#if UNITY_EDITOR
using System.Globalization;

namespace TechCosmos.Hub.Editor
{
    /// <summary>
    /// Emoji 检测（不依赖 \p{Extended_Pictographic}，兼容 Unity Mono/.NET Standard 2.0）。
    /// </summary>
    internal static class HubMarkdownEmoji
    {
        public static bool ContainsEmoji(string text)
        {
            if (string.IsNullOrEmpty(text)) return false;

            var enumerator = StringInfo.GetTextElementEnumerator(text);
            while (enumerator.MoveNext())
            {
                if (IsEmojiTextElement(enumerator.GetTextElement()))
                    return true;
            }

            return false;
        }

        public static bool TryReadAt(string text, int index, out string emoji, out int length)
        {
            emoji = null;
            length = 0;
            if (index < 0 || index >= text.Length) return false;

            var enumerator = StringInfo.GetTextElementEnumerator(text, index);
            if (!enumerator.MoveNext()) return false;

            emoji = enumerator.GetTextElement();
            if (string.IsNullOrEmpty(emoji) || !IsEmojiTextElement(emoji)) return false;

            length = emoji.Length;
            return true;
        }

        private static bool IsEmojiTextElement(string textElement)
        {
            if (string.IsNullOrEmpty(textElement)) return false;

            if (textElement.IndexOf('\u200D') >= 0)
            {
                foreach (var part in SplitTextElements(textElement))
                {
                    if (part.Length == 1 && part[0] == '\u200D') continue;
                    if (IsEmojiCodePoint(GetCodePoint(part))) return true;
                }

                return false;
            }

            return IsEmojiCodePoint(GetCodePoint(textElement));
        }

        private static int GetCodePoint(string textElement)
        {
            if (textElement.Length == 1)
                return textElement[0];

            return char.ConvertToUtf32(textElement, 0);
        }

        private static bool IsEmojiCodePoint(int codePoint)
        {
            if (codePoint >= 0x1F300 && codePoint <= 0x1FAFF) return true;
            if (codePoint >= 0x2600 && codePoint <= 0x26FF) return true;
            if (codePoint >= 0x2700 && codePoint <= 0x27BF) return true;
            if (codePoint >= 0x2300 && codePoint <= 0x23FF) return true;
            if (codePoint >= 0x2B05 && codePoint <= 0x2B55) return true;
            if (codePoint >= 0x1F1E6 && codePoint <= 0x1F1FF) return true;
            if (codePoint == 0x24C2 || codePoint == 0x1F004 || codePoint == 0x1F0CF) return true;
            return false;
        }

        private static string[] SplitTextElements(string text)
        {
            var parts = new System.Collections.Generic.List<string>();
            var enumerator = StringInfo.GetTextElementEnumerator(text);
            while (enumerator.MoveNext())
                parts.Add(enumerator.GetTextElement());
            return parts.ToArray();
        }
    }
}
#endif
