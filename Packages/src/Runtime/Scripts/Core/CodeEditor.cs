using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace C2M.CodeEditor
{
    public class CodeEditor : TMP_InputField
    {
        [SerializeField]
        private LanguageSetting languageSetting;
        [SerializeField]
        private Scrollbar horizontalScrollBar;
        [SerializeField]
        private CodeAutoComplete codeAutoComplete;
        [SerializeField]
        private TextMeshProUGUI highlightText;
        [SerializeField]
        private TextMeshProUGUI lineText;
        [SerializeField]
        private float undoIdleThresholdSeconds = 0.5f;

        private int selectionStart = 0;
        private int selectionEnd = 0;
        private string prevText = string.Empty;
        private int prevLineCount = 1;
        private int prevCaretPosition = 0;
        private int caretPositionOffset = 0;
        private bool ignoreOnValueChanged = false;
        private bool isDoneUpdateText = false;
        private CodeEditHistory editHistory;
        private CodeHighlighter codeHighlighter;
        private CodeAutoIndent autoIndent;
        private RectTransform caretRectTransform;
        private InputActionsManager inputActionsManager;
        private bool isRemoved = false;
        private bool isPaste = false;
        private float mainTextAnchorX = 0f;
        private Coroutine updateLineNumberCoroutine = null;
        private Coroutine nextFrameProcessCoroutine = null;
        private Vector2 lastCaretPosition;

        public string HighlightText => highlightText.text;
        public float UndoIdleThresholdSeconds => undoIdleThresholdSeconds;
        public LanguageSetting LanguageSetting => languageSetting;

        protected override void Awake()
        {
            base.Awake();

            if (!Application.isPlaying)
                return;
            
            languageSetting.Init();
            editHistory = new CodeEditHistory(undoIdleThresholdSeconds, OnUpdateTextAndSetCaretPositionImmediately);
            codeHighlighter = new CodeHighlighter(languageSetting, OnUpdateHighlightText);
            autoIndent = new CodeAutoIndent(languageSetting, OnUpdateTextAndSetCaretOffset, OnSetIgnoreValueChanged);
            inputActionsManager = InputActionsManager.Instance;

            prevText = text;
            codeHighlighter.UpdateTextHighlight(text);

            UpdateLineNumber();
        }

        protected override void Start()
        {
            base.Start();

#if UNITY_6000_0_OR_NEWER
            textComponent.textWrappingMode = TextWrappingModes.NoWrap;
#else
            textComponent.enableWordWrapping = false;
#endif           
            textViewport.Find("Caret")?.TryGetComponent<RectTransform>(out caretRectTransform);
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            if (!Application.isPlaying)
                return;

            onTextSelection.AddListener(OnTextSelection);
            onValueChanged.AddListener(OnValueChanged);
            horizontalScrollBar.onValueChanged.AddListener(OnScrollbarValueChange);
            verticalScrollbar.onValueChanged.AddListener(OnVerticalScrollBarValueChange);
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            if (!Application.isPlaying)
                return;

            onTextSelection.RemoveListener(OnTextSelection);
            onValueChanged.RemoveListener(OnValueChanged);
            horizontalScrollBar.onValueChanged.RemoveListener(OnScrollbarValueChange);
            verticalScrollbar.onValueChanged.RemoveListener(OnVerticalScrollBarValueChange);
        }

        public override void OnUpdateSelected(BaseEventData eventData)
        {
            if (codeAutoComplete.IsShow && inputActionsManager.IsInputAutoComplete)
                return;

            base.OnUpdateSelected(eventData);
        }

        public override void OnScroll(PointerEventData eventData)
        {
            base.OnScroll(eventData);

            AssignPositioningByMainTextComponent(highlightText.rectTransform);
        }

        public override void Rebuild(CanvasUpdate update)
        {
            base.Rebuild(update);

            switch (update)
            {
                case CanvasUpdate.LatePreRender:
                    GenerateCaret();
                    break;
            }

        }

        private void Update()
        {
            if (!isFocused)
                return;

            if (inputActionsManager.IsInputUndo)
            {
                Undo();
            }
            else if (inputActionsManager.IsInputRedo || inputActionsManager.IsInputRedo2)
            {
                Redo();
            }

            if (inputActionsManager.IsInputEnter)
            {
                autoIndent.GetAutoIndentText(text, caretPosition);
            }

            if(inputActionsManager.IsInputPaste)
            {
                isPaste = true;
            }
        }

        private void OnScrollbarValueChange(float value)
        {
            if (value < 0 || value > 1) 
                return;

            AdjustTextPositionRelativeToViewport(value);
        }

        private void OnVerticalScrollBarValueChange(float value)
        {
            if (lineText == null)
                return;

            UpdateLineTextRect();
            AssignPositioningByMainTextComponent(highlightText.rectTransform);
        }

        private void OnUpdateTextAndSetCaretOffset(string text, int addCaretPosition)
        {
            caretPositionOffset = addCaretPosition;
            UpdateText(text, false);
        }

        private void OnUpdateTextAndSetCaretPositionImmediately(string text, int caretPosition)
        {
            UpdateText(text, true);
            stringPosition = caretPosition;
        }

        private void OnUpdateHighlightText(string highlightText)
        {
            this.highlightText.text = highlightText;
        }

        private void OnSetIgnoreValueChanged(bool value)
        {
            ignoreOnValueChanged = value;
        }

        private void OnTextSelection(string s, int start, int end)
        {
            selectionStart = Mathf.Min(start, end);
            selectionEnd = Mathf.Max(start, end);
        }

        private void OnValueChanged(string newText)
        {
            if (ignoreOnValueChanged)
                return;

            if (inputActionsManager.IsInputPaste)
                return;

            if (!string.IsNullOrEmpty(newText))
            {
                newText = newText.Replace("\r\n", "\n").Replace('\r', '\n');
            }

            if (string.IsNullOrEmpty(newText))
            {
                if (codeAutoComplete.IsShow)
                {
                    codeAutoComplete.Hide();
                }

                highlightText.text = string.Empty;
                return;
            }

            BeforeProcess();
            MainProcess(newText);
            AfterProcess();
        }

        private void BeforeProcess()
        {
            if (isDoneUpdateText)
            {
                isDoneUpdateText = false;
            }

            ignoreOnValueChanged = true;
        }

        private void MainProcess(string newText)
        {
            if (newText.Contains(Constants.STRING_CARRIAGE_RETURN))
            {
                newText = newText.Replace(Constants.STRING_CARRIAGE_RETURN, "");
            }

            ProcessAutoUnIndent(newText);
            ProcessIndent();
            ProcessRemoved();
            ProcessCodeComplete(newText);

            if (!isDoneUpdateText)
            {
                codeHighlighter.UpdateTextHighlight(text);

                UpdateHistory();
                UpdateLineNumber();
                prevText = newText;
            }
        }

        private void AfterProcess()
        {
            ignoreOnValueChanged = false;
            isDoneUpdateText = false;
        }

        private void NextFrameProcess()
        {
            if (nextFrameProcessCoroutine != null)
            {
                StopCoroutine(nextFrameProcessCoroutine);
                nextFrameProcessCoroutine = null;
            }
            nextFrameProcessCoroutine = StartCoroutine(NextFrameProcessCoroutine());
        }

        private IEnumerator NextFrameProcessCoroutine()
        {
            yield return null;

            DelayedAdjustAndScroll();

            if (isPaste)
            {
                isPaste = false;
                onValueChanged.Invoke(text);
                ProcessRemoved();
            }
        }
                
        private void GenerateCaret()
        {
            Vector2 startCaretPosition = Vector2.zero;
            float height = 0;
            TMP_CharacterInfo currentCharacter;

            if (caretPositionInternal >= m_TextComponent.textInfo.characterInfo.Length || caretPositionInternal < 0)
                return;

            int currentLine = m_TextComponent.textInfo.characterInfo[caretPositionInternal].lineNumber;

            if (caretPositionInternal == m_TextComponent.textInfo.lineInfo[currentLine].firstCharacterIndex)
            {
                currentCharacter = m_TextComponent.textInfo.characterInfo[caretPositionInternal];
                height = currentCharacter.ascender - currentCharacter.descender;

                if (m_TextComponent.verticalAlignment == VerticalAlignmentOptions.Geometry)
                    startCaretPosition = new Vector2(currentCharacter.origin, 0 - height / 2);
                else
                    startCaretPosition = new Vector2(currentCharacter.origin, currentCharacter.descender);
            }
            else
            {
                currentCharacter = m_TextComponent.textInfo.characterInfo[caretPositionInternal - 1];
                height = currentCharacter.ascender - currentCharacter.descender;

                if (m_TextComponent.verticalAlignment == VerticalAlignmentOptions.Geometry)
                    startCaretPosition = new Vector2(currentCharacter.xAdvance, 0 - height / 2);
                else
                    startCaretPosition = new Vector2(currentCharacter.xAdvance, currentCharacter.descender);
            }

            if (isFocused && (startCaretPosition != lastCaretPosition || isRemoved))
            {
                float textWidth = textComponent.preferredWidth;
                float viewportWidth = textViewport.rect.width;
                float maxOffset = Mathf.Max(0, textWidth - viewportWidth);

                float firstCharX = 0f;
                if (textComponent.textInfo.characterCount > 0)
                    firstCharX = textComponent.textInfo.characterInfo[0].origin;

                float caretLocalX = startCaretPosition.x - firstCharX;
                float anchoredX = textComponent.rectTransform.anchoredPosition.x;

                float caretViewportX = caretLocalX + anchoredX;

                if (caretViewportX < 0)
                {
                    anchoredX -= caretViewportX;
                }
                else if (caretViewportX > viewportWidth)
                {
                    anchoredX -= (caretViewportX - viewportWidth);
                }

                anchoredX = Mathf.Clamp(anchoredX, -maxOffset, 0);

                textComponent.rectTransform.anchoredPosition = new Vector2(anchoredX, textComponent.rectTransform.anchoredPosition.y);

                AssignPositioningByMainTextComponent(highlightText.rectTransform);
                AssignPositioningByMainTextComponent(caretRectTransform);                
                UpdateLineTextRect();
                NextFrameProcess();
            }

            lastCaretPosition = startCaretPosition;
        }

        private void ProcessCodeComplete(string text)
        {
            if (isRemoved)
                return;

            Vector3 caretWorldPosition = GetCaretWorldPosition();
            codeAutoComplete.UpdateCompletePosition(caretWorldPosition);
            codeAutoComplete.UpdateSuggestions(text, caretPosition);
        }

        private void ProcessRemoved()
        {
            if (inputActionsManager.IsPressedBackspace)
            {
                isRemoved = true;
                mainTextAnchorX = textComponent.rectTransform.anchoredPosition.x;
            }

            Rect viewportRect = textViewport.rect;
            float size = viewportRect.width / textComponent.preferredWidth;

            horizontalScrollBar.size = size;
        }

        private void UpdateLineTextRect()
        {
            Vector2 pos = lineText.rectTransform.anchoredPosition;
            pos.y = textComponent.rectTransform.anchoredPosition.y;
            lineText.rectTransform.anchoredPosition = pos;
        }

        private Vector3 GetCaretWorldPosition()
        {
            int cp = Mathf.Clamp(caretPosition, 0, textComponent.textInfo.characterCount);
            Vector3 caretWorldPosition;

            if (cp == 0)
            {
                Vector3 firstCharBottomLeft = Vector3.zero;

                if (textComponent.textInfo.characterInfo.Length > 0)
                {
                    firstCharBottomLeft = textComponent.transform.TransformPoint(textComponent.textInfo.characterInfo[0].bottomLeft);
                }
                else if (textComponent.textInfo.lineInfo.Length > 0)
                {
                    firstCharBottomLeft = textComponent.transform.TransformPoint(new Vector3(textComponent.textInfo.lineInfo[0].ascender, textComponent.textInfo.lineInfo[0].baseline, 0));
                }
                else
                {
                    firstCharBottomLeft = m_RectTransform.TransformPoint(m_RectTransform.rect.min);
                }
                caretWorldPosition = firstCharBottomLeft;
            }
            else
            {
                TMP_CharacterInfo charInfo = textComponent.textInfo.characterInfo[cp - 1];
                Vector3 localPosition = new Vector3(charInfo.topRight.x, charInfo.descender, 0);

                if (cp == textComponent.textInfo.characterCount)
                {
                    localPosition = new Vector3(charInfo.topRight.x, charInfo.descender, 0);
                }

                caretWorldPosition = textComponent.transform.TransformPoint(localPosition);
            }

            return caretWorldPosition;
        }

        private void ProcessIndent()
        {
            bool IsIndent = inputActionsManager.IsInputTab;
            bool IsUnindent = inputActionsManager.IsInputUnIndent;

            if (!IsIndent && !IsUnindent)
                return;

            bool isSelectionAndInputKey = prevText.Length > text.Length;
            StringBuilder sb = new StringBuilder(prevText);
            List<int> lines = GetSelectionLines(isSelectionAndInputKey);

            if (IsUnindent)
            {
                Unindent(lines, isSelectionAndInputKey, ref sb);
            }
            else if (IsIndent)
            {
                if (!isSelectionAndInputKey || lines.Count <= 1)
                    return;

                Indent(lines, ref sb);
            }

            UpdateHistory();
            UpdateText(sb.ToString(), isSelectionAndInputKey);
        }

        private void ProcessAutoUnIndent(string text)
        {
            if (text.Length <= prevText.Length || caretPosition <= 1)
                return;

            if (caretPosition - 1 < 0 || caretPosition - 1 >= text.Length)
                return;

            if (text[caretPosition - 1] == Constants.CHAR_NEW_LINE)
                return;

            autoIndent.UnIndent(text, caretPosition);
        }

        private void Indent(List<int> lines, ref StringBuilder sb)
        {
            foreach (int i in lines)
            {
                sb.Insert(i + 1, Constants.CHAR_TAB);
                selectionEnd++;
            }

            selectionStart++;
        }


        private void DelayedAdjustAndScroll()
        {
            if (isRemoved)
            {
                isRemoved = false;
            }

            horizontalScrollBar.value = GetScrollPositionRelativeToViewport();
        }

        private float GetScrollPositionRelativeToViewport()
        {
            Rect viewportRect = textViewport.rect;

            float scrollPosition = -textComponent.rectTransform.anchoredPosition.x
             / (textComponent.preferredWidth - textViewport.rect.width);

            scrollPosition = Mathf.Clamp01(scrollPosition);
            scrollPosition = (int)((scrollPosition * 1000) + 0.5f) / 1000.0f;

            return scrollPosition;
        }

        private void Unindent(List<int> lines, bool isSelectionAndInputKey, ref StringBuilder sb)
        {
            foreach (int i in lines)
            {
                if (sb[i + 1] == Constants.CHAR_TAB)
                {
                    sb.Remove(i + 1, 1);
                    caretPositionOffset--;
                }
            }

            if (!isSelectionAndInputKey)
            {
                caretPositionOffset--;
            }
            else
            {
                selectionStart--;
                selectionEnd -= lines.Count;
            }
        }

        private void Undo()
        {
            if (editHistory.IsEmptyUndoList)
                return;

            ignoreOnValueChanged = true;

            editHistory.AddRedo(text, stringPosition);
            editHistory.Undo();

            ignoreOnValueChanged = false;
        }

        private void Redo()
        {
            if (editHistory.IsEmptyRedoList)
                return;

            ignoreOnValueChanged = true;

            editHistory.AddUndo(text, stringPosition);
            editHistory.Redo();

            ignoreOnValueChanged = false;
        }

        private void UpdateText(string text, bool isSelectionAndInputKey)
        {
            int preUpdateTextStringPosition = stringPosition;

            this.text = text;
            prevText = text;
            isDoneUpdateText = true;
            codeHighlighter.UpdateTextHighlight(text);

            UpdateCeret(preUpdateTextStringPosition, isSelectionAndInputKey);
            UpdateLineNumber();
        }

        private void UpdateCeret(int preUpdateTextStringPosition, bool isSelectionAndInputKey)
        {
            stringPosition = preUpdateTextStringPosition + caretPositionOffset;
            caretPositionOffset = 0;

            if (!isSelectionAndInputKey)
            {
                selectionStart = 0;
                selectionEnd = 0;
            }
            else
            {
                selectionAnchorPosition = selectionStart;
                selectionFocusPosition = selectionEnd;
            }
        }

        private void UpdateHistory()
        {
            if (!editHistory.CanUpdateUndo)
                return;

            editHistory.ClearRedo();
            editHistory.AddUndo(prevText, stringPosition);
            editHistory.UpdateTime();
        }

        private void UpdateLineNumber()
        {
            if (updateLineNumberCoroutine != null)
            {
                StopCoroutine(updateLineNumberCoroutine);
                updateLineNumberCoroutine = null;
            }

            updateLineNumberCoroutine = StartCoroutine(UpdateLineNumberCoroutine());
        }

        private IEnumerator UpdateLineNumberCoroutine()
        {
            yield return new WaitForEndOfFrame();

            int lineCount = textComponent.textInfo.lineCount;

            if (prevLineCount == lineCount)
                yield break;

            prevLineCount = lineCount;

            if (lineCount <= 0)
            {
                lineText.text = "1";
                yield break;
            }

            StringBuilder sb = new StringBuilder();

            for (int i = 1; i <= lineCount; ++i)
            {
                sb.Append(i).Append(Constants.CHAR_NEW_LINE);
            }

            if (sb.Length > 0)
            {
                sb.Length--;
            }

            lineText.text = sb.ToString();
        }

        private List<int> GetSelectionLines(bool isSelectionAndInputTab)
        {
            List<int> result = new List<int>();
            int index = 0;

            if (isSelectionAndInputTab)
            {
                for (index = selectionEnd - 1; index >= 0; --index)
                {
                    if (index < selectionStart && prevText[index] == Constants.CHAR_NEW_LINE)
                        break;

                    if (prevText[index] != Constants.CHAR_NEW_LINE)
                        continue;

                    result.Add(index);
                }

                result.Add(index);
            }
            else
            {
                for (index = stringPosition - 2; index >= 0; --index)
                {
                    if (prevText[index] != Constants.CHAR_NEW_LINE)
                        continue;

                    result.Add(index);
                    break;
                }

                if (result.Count == 0)
                {
                    result.Add(index);
                }
            }

            return result;
        }

        private void AdjustTextPositionRelativeToViewport(float relativePosition)
        {
            if (textComponent == null)
                return;

            TMP_TextInfo textInfo = textComponent.textInfo;

            // Check to make sure we have valid data and lines to query.
            if (textInfo == null || textInfo.lineInfo == null || textInfo.lineCount == 0 || textInfo.lineCount > textInfo.lineInfo.Length)
                return;

            float horizontalAlignmentOffset = 0;
            float textWidth = textComponent.preferredWidth;

            switch (textComponent.horizontalAlignment)
            {
                case HorizontalAlignmentOptions.Left:
                    horizontalAlignmentOffset = 0;
                    break;
                case HorizontalAlignmentOptions.Right:
                    horizontalAlignmentOffset = 1.0f;
                    break;
                case HorizontalAlignmentOptions.Center:
                case HorizontalAlignmentOptions.Justified:
                case HorizontalAlignmentOptions.Flush:
                    horizontalAlignmentOffset = 0.5f;
                    break;
                case HorizontalAlignmentOptions.Geometry:
                    horizontalAlignmentOffset = 0.5f;
                    textWidth = textComponent.bounds.size.x;
                    break;
            }

            textComponent.rectTransform.anchoredPosition = new Vector2(-(textWidth - textViewport.rect.width) * (relativePosition - horizontalAlignmentOffset), textComponent.rectTransform.anchoredPosition.y);

            AssignPositioningByMainTextComponent(highlightText.rectTransform);
            AssignPositioningByMainTextComponent(caretRectTransform);

            //Debug.Log("Text width: " + codeInput.textComponent.preferredWidth+ "  Viewport width: " + codeInput.textViewport.rect.width+ "  Adjusted RectTransform anchordedPosition:" + codeInput.textComponent.rectTransform.anchoredPosition + "  Text Bounds: " + codeInput.textComponent.bounds.ToString("f3"));
        }

        private void AssignPositioningByMainTextComponent(RectTransform target)
        {
            if (textComponent != null && target != null &&
                (target.localPosition != textComponent.rectTransform.localPosition ||
                 target.localRotation != textComponent.rectTransform.localRotation ||
                 target.localScale != textComponent.rectTransform.localScale ||
                 target.anchorMin != textComponent.rectTransform.anchorMin ||
                 target.anchorMax != textComponent.rectTransform.anchorMax ||
                 target.anchoredPosition != textComponent.rectTransform.anchoredPosition ||
                 target.sizeDelta != textComponent.rectTransform.sizeDelta ||
                 target.pivot != textComponent.rectTransform.pivot))
            {
                target.localPosition = textComponent.rectTransform.localPosition;
                target.localRotation = textComponent.rectTransform.localRotation;
                target.localScale = textComponent.rectTransform.localScale;
                target.anchorMin = textComponent.rectTransform.anchorMin;
                target.anchorMax = textComponent.rectTransform.anchorMax;
                target.anchoredPosition = textComponent.rectTransform.anchoredPosition;
                target.sizeDelta = textComponent.rectTransform.sizeDelta;
                target.pivot = textComponent.rectTransform.pivot;
            }
        }

        #region UNITY_EDITOR
        public void SetPointSize(float pointSize)
        {
            TMP_Text placeholderTextComponent = m_Placeholder as TMP_Text;

            if (placeholderTextComponent != null)
                placeholderTextComponent.fontSize = pointSize;

            if (highlightText != null)
                highlightText.fontSize = pointSize;

            if (lineText != null)
                lineText.fontSize = pointSize;

            textComponent.fontSize = pointSize;
        }

        public void SetFontAsset(TMP_FontAsset fontAsset)
        {
            TMP_Text placeholderTextComponent = m_Placeholder as TMP_Text;

            if (placeholderTextComponent != null)
                placeholderTextComponent.font = fontAsset;

            if (highlightText != null)
                highlightText.font = fontAsset;

            if (lineText != null)
                lineText.font = fontAsset;

            textComponent.font = fontAsset;
        }
        #endregion

    }
}