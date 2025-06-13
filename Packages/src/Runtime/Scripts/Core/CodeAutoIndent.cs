using System;
using System.Collections;
using System.Text.RegularExpressions;
using UnityEngine;


namespace C2M.CodeEditor
{
    public class CodeAutoIndent
    {
        private LanguageSetting languageSetting;
        private Action<string, int> indentAction;
        private Action<bool> setIgnoreValueChangedAction;

        public CodeAutoIndent(LanguageSetting languageSetting, Action<string, int> indentAction, Action<bool> setIgnoreValueChangedAction)
        {
            this.languageSetting = languageSetting;
            this.indentAction = indentAction;
            this.setIgnoreValueChangedAction = setIgnoreValueChangedAction;
        }

        public void GetAutoIndentText(string inputText, int caretPosition)
        {
            setIgnoreValueChangedAction?.Invoke(true);
            CoroutineUtil.StartCoroutine(Indent(inputText, caretPosition));
        }

        private IEnumerator Indent(string inputText, int caretPosition)
        {
            yield return null;

            string resultText = inputText;
            int resultCaretPosition = 0;

            if (caretPosition <= 1 || inputText.Length == 0 || inputText[caretPosition - 1] != '\n')
            {
                indentAction?.Invoke(resultText, resultCaretPosition);
                setIgnoreValueChangedAction?.Invoke(false);
                yield break;
            }

            int prevLineStartIndex = inputText.LastIndexOf('\n', caretPosition - 2);
            prevLineStartIndex = (prevLineStartIndex == -1) ? 0 : prevLineStartIndex + 1;
            string previousLine = inputText.Substring(prevLineStartIndex, (caretPosition - 1) - prevLineStartIndex);

            string indentToApply = GetLeadingWhitespace(previousLine);
            Regex regex = languageSetting.GetAutoIndentPattern();

            if (regex != null)
            {
                if (regex.IsMatch(previousLine))
                {
                    indentToApply += languageSetting.GetIndentString();
                }
            }

            if (!string.IsNullOrEmpty(indentToApply))
            {
                resultText = inputText.Insert(caretPosition, indentToApply);
                resultCaretPosition = caretPosition + indentToApply.Length;
            }

            indentAction?.Invoke(resultText, resultCaretPosition);
            setIgnoreValueChangedAction?.Invoke(false);
        }

        public void UnIndent(string currentText, int caretPos)
        {
            int currentLineStartIndex = currentText.LastIndexOf('\n', caretPos - 2);
            currentLineStartIndex = (currentLineStartIndex == -1) ? 0 : currentLineStartIndex + 1;
            string currentLineTextUpToCaret = currentText.Substring(currentLineStartIndex, caretPos - currentLineStartIndex);
            string resultText = currentText;
            int resultCaretPosition = 0;
            string indentString = languageSetting.GetIndentString();

            if (!languageSetting.GetAutoUnIndentPattern().IsMatch(currentLineTextUpToCaret))
                return;

            string leadingWhitespace = GetLeadingWhitespace(currentLineTextUpToCaret);
            if (leadingWhitespace.Length >= indentString.Length &&
                leadingWhitespace.EndsWith(indentString))
            {

                string newLeadingWhitespace = leadingWhitespace.Substring(0, leadingWhitespace.Length - indentString.Length);
                string contentMatched = currentLineTextUpToCaret.Substring(leadingWhitespace.Length);
                string textAfterAffectedPart = currentText.Substring(caretPos);

                resultText = currentText.Substring(0, currentLineStartIndex) + newLeadingWhitespace + contentMatched + textAfterAffectedPart;
                resultCaretPosition = caretPos - indentString.Length;
            }
            indentAction?.Invoke(resultText, resultCaretPosition);
        }

        private string GetLeadingWhitespace(string line)
        {
            int i = 0;
            while (i < line.Length && char.IsWhiteSpace(line[i]))
            {
                i++;
            }
            return line.Substring(0, i);
        }
    }
}
