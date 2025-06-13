using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

namespace C2M.CodeEditor
{
    [CreateAssetMenu(fileName = "Language Setting", menuName = "Code Editor/Language Setting")]

    public class LanguageSetting : ScriptableObject
    {
        private enum LanguageType
        {
            CSharp,
        }

        private enum IndentType
        {
            Tab,
            Space
        }

        [SerializeField]
        [TextArea(1, 10)]
        private string keywords;
        [SerializeField]
        private Color keywordColor;
        [SerializeField]
        private List<StaticMemberData> subKeywords;
        [SerializeField]
        [TextArea(1, 10)]
        private string symbol;
        [SerializeField]
        private Color symbolColor;
        [SerializeField]
        private string lineComment;
        [SerializeField]
        private string multiLineCommentStart;
        [SerializeField]
        private string multiLineCommentEnd;
        [SerializeField]
        private Color commentColor;
        [SerializeField]
        private LanguageType autoIndentLanguage;
        [SerializeField]
        private IndentType indentType;
        [SerializeField]
        [TextArea(1, 10)]
        private string autoIndentChars;
        [SerializeField]
        [TextArea(1, 10)]
        private string autoUnIndentChars;

        private string commentColorHex;
        private string keywordColorHex;
        private string symbolColorHex;
        private string lineCommentPattern;
        private string multiLinePattern;
        private string keywordPattern;
        private string symbolPattern;
        private string autoIndentPattern;
        private string autoUnIndentPattern;

        private readonly Char[] splitChars = { ' ', ',', '\n', '\r' };

        private string ColorToHex(Color color)
        {
            return "#" + ColorUtility.ToHtmlStringRGB(color);
        }

        private void InitColor()
        {
            commentColorHex = ColorToHex(commentColor);
            keywordColorHex = ColorToHex(keywordColor);
            symbolColorHex = ColorToHex(symbolColor);
        }

        private void InitPattern()
        {
            if (!string.IsNullOrEmpty(lineComment))
            {
                lineCommentPattern = $"{Regex.Escape(lineComment)}.*";
            }

            if (!string.IsNullOrEmpty(multiLineCommentStart) && !string.IsNullOrEmpty(multiLineCommentEnd))
            {
                multiLinePattern = $"{Regex.Escape(multiLineCommentStart)}[\\s\\S]*?(?:{Regex.Escape(multiLineCommentEnd)}|$)";
            }

            if (!string.IsNullOrEmpty(keywords))
            {
                keywordPattern = keywords.Split(splitChars, StringSplitOptions.RemoveEmptyEntries)
                                     .Select(k => k.Trim())
                                     .Where(k => !string.IsNullOrEmpty(k))
                                     .Distinct()
                                     .Select(k => $@"\b{Regex.Escape(k)}\b")
                                     .Aggregate((k1, k2) => $"{k1}|{k2}");
            }

            symbolPattern = symbol.Split(splitChars, StringSplitOptions.RemoveEmptyEntries)
                                     .Select(c => c.Trim())
                                     .Where(c => !string.IsNullOrEmpty(c))
                                     .Distinct()
                                     .Select(Regex.Escape)
                                     .Aggregate((c1, c2) => $"{c1}|{c2}");

            autoIndentPattern = autoIndentChars.Split(splitChars, StringSplitOptions.RemoveEmptyEntries)
                                     .Select(c => c.Trim())
                                     .Where(c => !string.IsNullOrEmpty(c))
                                     .Distinct()
                                     .Select(c => $@"{Regex.Escape(c)}\s*$")
                                     .Aggregate((c1, c2) => $"{c1}|{c2}");

            autoUnIndentPattern = autoUnIndentChars.Split(splitChars, StringSplitOptions.RemoveEmptyEntries)
                                     .Select(c => c.Trim())
                                     .Where(c => !string.IsNullOrEmpty(c))
                                     .Distinct()
                                     .Select(s => $@"^\s*{Regex.Escape(s)}")
                                     .Aggregate((c1, c2) => $"{c1}|{c2}");
        }

        public void Init()
        {
            InitColor();
            InitPattern();
        }

        public string GetHighlightPattern()
        {
            return $"(?<comment>({lineCommentPattern})|({multiLinePattern}))|(?<keyword>{keywordPattern})|(?<symbol>{symbolPattern})";
        }

        public Regex GetAutoIndentPattern()
        {
            if (string.IsNullOrEmpty(autoIndentChars))
                return null;

            return new Regex(autoIndentPattern, RegexOptions.Compiled | RegexOptions.RightToLeft); ;
        }

        public Regex GetAutoUnIndentPattern()
        {
            if (string.IsNullOrEmpty(autoUnIndentChars))
                return null;

            return new Regex(autoUnIndentPattern, RegexOptions.Compiled);
        }

        public string GetCommentColor()
        {
            return commentColorHex;
        }

        public string GetKeywordColor()
        {
            return keywordColorHex;
        }

        public string GetSymbolColor()
        {
            return symbolColorHex;
        }

        public string GetKeywords()
        {
            return keywords;
        }

        public List<StaticMemberData> GetStaticMembers()
        {
            return subKeywords;
        }

        public Char[] GetSplitChars()
        {
            return splitChars;
        }

        public string GetIndentString()
        {
            if (indentType == IndentType.Tab)
            {
                return Constants.STRING_TAB;
            }
            else
            {
                return new string(Constants.CHAR_SPACE, 4);
            }
        }
    }
}
