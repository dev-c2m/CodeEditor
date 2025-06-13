using C2M.CodeEditor;
using System;
using System.Text.RegularExpressions;
using UnityEngine;

public class CodeHighlighter
{
    private LanguageSetting languageSetting;
    private Action<string> updateHighlightTextAction;

    public CodeHighlighter(LanguageSetting languageSetting, Action<string> updateHighlightTextAction)
    {
        this.languageSetting = languageSetting;
        this.updateHighlightTextAction = updateHighlightTextAction;
    }

    private string GetHighlightText(string text)
    {
        if (string.IsNullOrEmpty(text))
            return string.Empty;

        string pattern = languageSetting.GetHighlightPattern();

        return Regex.Replace(text, pattern, (m =>
        {
            string input = m.Value;

            if (m.Groups["comment"].Success)
                return $"<color={languageSetting.GetCommentColor()}>" + input + "</color>";

            if (m.Groups["keyword"].Success)
                return $"<color={languageSetting.GetKeywordColor()}>" + input + "</color>";

            if (m.Groups["symbol"].Success)
                return $"<color={languageSetting.GetSymbolColor()}>" + input + "</color>";

            return input;
        }));
    }
    
    public void UpdateTextHighlight(string text)
    {
        if (languageSetting == null)
            return;

        string result = GetHighlightText(text);
        updateHighlightTextAction?.Invoke(result);
    }
}
