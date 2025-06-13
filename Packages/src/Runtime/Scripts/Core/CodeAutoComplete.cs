using System.Collections.Generic;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;

namespace C2M.CodeEditor
{
    public class CodeAutoComplete : MonoBehaviour
    {
        private struct KeywordScore
        {
            public string Keyword { get; }
            public int Score { get; }

            public KeywordScore(string keyword, int score)
            {
                Keyword = keyword;
                Score = score;
            }
        }

        [SerializeField]
        private LanguageSetting languageSetting;
        [SerializeField]
        private int maxSuggestions = -1;
        [SerializeField]
        private TMP_InputField inputField;
        [SerializeField]
        private ScrollRect codeCompleteRoot;
        [SerializeField]
        private Transform codeCompleteMemberRoot;
        [SerializeField]
        private GameObject codeCompleteMember;
        [SerializeField]
        private Color codeCompleteDefaultColor;
        [SerializeField]
        private Color codeCompleteSelectColor;

        private List<string> globalKeywords;
        private Dictionary<string, List<string>> staticMembers = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        private Stack<UI_CodeCompleteMember> codeCompleteMembersPool = new Stack<UI_CodeCompleteMember>();
        private List<UI_CodeCompleteMember> activeCodeCompleteMembers = new List<UI_CodeCompleteMember>();
        private List<string> suggestionList = new List<string>();
        private bool isShow = false;
        private int selectedIndex = 0;
        private InputActionsManager inputActionsManager;

        private readonly char[] TOKEN_SEPARATORS = { ' ', '.', '\n' };
        private readonly char DOT = '.';
        private readonly int DEFAULT_SCORE = 10;
        private readonly int PREFIX_MATCH_SCORE = 1000;
        private readonly int CASE_MATCH_SCORE = 50;
        private readonly int POSITION_SCORE = 5;
        private readonly int LENGTH_SCORE = 30;

        public bool IsShow { get => isShow; }

        private void Start()
        {
            codeCompleteRoot.gameObject.SetActive(false);
            inputActionsManager = InputActionsManager.Instance;

            SetGlobalKeywords();
            SetStaticMemberKeywords();

            globalKeywords.Sort();
        }

        private void Update()
        {
            if (!IsShow)
                return;

            if (inputActionsManager.IsInputArrowDown)
            {
                if (selectedIndex + 1 >= activeCodeCompleteMembers.Count)
                    return;
                
                SelectCodeCompleteMember(selectedIndex + 1);
            }
            else if (inputActionsManager.IsInputArrowUp)
            {
                if (selectedIndex <= 0)
                    return;

                SelectCodeCompleteMember(selectedIndex - 1);
            }
            else if (inputActionsManager.IsInputEnter || inputActionsManager.IsInputTab)
            {
                if (selectedIndex < 0)
                    return;

                OnSuggestionSelected(suggestionList[selectedIndex]);
            }
            else if (inputActionsManager.IsInputEscape)
            {
                HideSuggestions();
            }
        }

        private void SetGlobalKeywords()
        {
            globalKeywords = languageSetting.GetKeywords()
                                            .Split(languageSetting.GetSplitChars(), StringSplitOptions.RemoveEmptyEntries)
                                            .Distinct()
                                            .ToList();
        }

        private void SetStaticMemberKeywords()
        {
            List<StaticMemberData> staticMembers = languageSetting.GetStaticMembers();
            List<string> subKeywords = new List<string>();

            foreach (StaticMemberData member in staticMembers)
            {
                if (!globalKeywords.Contains(member.GetMainKeyword()))
                {
                    globalKeywords.Add(member.GetMainKeyword());
                }

                subKeywords = member.GetSubKeyword()
                                    .Split(languageSetting.GetSplitChars(), StringSplitOptions.RemoveEmptyEntries)
                                    .Distinct()
                                    .OrderBy(k => k, StringComparer.OrdinalIgnoreCase)
                                    .ToList();

                this.staticMembers[member.GetMainKeyword()] = subKeywords;
            }
        }

        private bool IsLastInputDot(int keywordStartIndex, int caretPosition, string currentInput)
        {
            return keywordStartIndex > 0 && caretPosition > 1 && currentInput[keywordStartIndex - 1] == DOT;
        }

        private List<string> GetKeywordList(int keywordStartIndex, string currentInput, bool useStaticmember)
        {
            if (useStaticmember)
            {
                int typeTokenStartIndex = currentInput.LastIndexOfAny(TOKEN_SEPARATORS, keywordStartIndex - 2);
                typeTokenStartIndex = (typeTokenStartIndex == -1) ? 0 : typeTokenStartIndex + 1;

                if (typeTokenStartIndex < keywordStartIndex - 1)
                {
                    string staticMainKeyword = currentInput.Substring(typeTokenStartIndex, (keywordStartIndex - 1) - typeTokenStartIndex).Trim();

                    if (staticMembers.TryGetValue(staticMainKeyword, out List<string> members))
                        return members;
                }

                return null;
            }

            return globalKeywords;
        }

        private List<string> GetSuggestionList(List<string> keywordList, string searchTarget, bool useStaticMember)
        {
            List<string> filteredSuggestions;
            IEnumerable<string> linq;

            if (!useStaticMember)
            {
                List<KeywordScore> scoredKeywords = new List<KeywordScore>();
                foreach (string keyword in keywordList)
                {
                    int score = CalculateScore(keyword, searchTarget);
                    if (score > 0 && !keyword.Equals(searchTarget, StringComparison.OrdinalIgnoreCase))
                    {
                        scoredKeywords.Add(new KeywordScore(keyword, score));
                    }
                }
                scoredKeywords.Sort((a, b) => b.Score.CompareTo(a.Score));
                linq = scoredKeywords.Select(ks => ks.Keyword);
            }
            else
            {
                linq = keywordList
                            .Where(member => member.StartsWith(searchTarget, StringComparison.OrdinalIgnoreCase))
                            .Where(member => string.IsNullOrWhiteSpace(searchTarget) || !member.Equals(searchTarget, StringComparison.OrdinalIgnoreCase));

            }

            if (maxSuggestions > 0)
            {
                filteredSuggestions = linq.Take(maxSuggestions).ToList();
            }
            else
            {
                filteredSuggestions = linq.ToList();
            }

            return filteredSuggestions;
        }


        private int CalculateScore(string keyword, string searchTarget)
        {
            if (string.IsNullOrEmpty(searchTarget))
                return 0;

            int score = 0;
            string lowerKeyword = keyword.ToLowerInvariant();
            string lowerSearchTarget = searchTarget.ToLowerInvariant();
            int matchIndex = lowerKeyword.IndexOf(lowerSearchTarget);

            if (matchIndex == -1)
                return 0;

            score += DEFAULT_SCORE;

            if (matchIndex == 0)
            {
                score += PREFIX_MATCH_SCORE;

                if (keyword.StartsWith(searchTarget, StringComparison.Ordinal))
                {
                    score += CASE_MATCH_SCORE;
                }
            }

            score += (keyword.Length - matchIndex) * POSITION_SCORE;

            int lengthDifference = Math.Abs(keyword.Length - searchTarget.Length);
            score += Math.Max(0, LENGTH_SCORE - lengthDifference);

            return score;
        }

        private void ShowSuggestions()
        {
            ClearSuggestions();
            codeCompleteRoot.gameObject.SetActive(true);

            for (int i = 0; i < suggestionList.Count; i++)
            {
                UI_CodeCompleteMember codeCompleteMember = PopCodeCompleteMember();
                activeCodeCompleteMembers.Add(codeCompleteMember);

                int index = i;
                codeCompleteMember.Init(suggestionList[i], (isSelected) =>
                {
                    if (isSelected)
                    {
                        OnSuggestionSelected(suggestionList[index]);
                        return;
                    }

                    SelectCodeCompleteMember(index);
                });
                codeCompleteMember.UnSelect(codeCompleteDefaultColor);
                codeCompleteMember.transform.SetSiblingIndex(i);
                codeCompleteMember.gameObject.SetActive(true);
            }
            
            SelectCodeCompleteMember(0);
            isShow = true;
        }

        private void SelectCodeCompleteMember(int index)
        {
            if (selectedIndex >= 0)
            {
                activeCodeCompleteMembers[selectedIndex].UnSelect(codeCompleteDefaultColor);
            }

            activeCodeCompleteMembers[index].Select(codeCompleteSelectColor);
            selectedIndex = index;

            UpdateScrollValue();
        }

        private void UpdateScrollValue()
        {
            if (selectedIndex == 0)
            {
                codeCompleteRoot.verticalNormalizedPosition = 1f;
                return;
            }

            RectTransform selectedItemRect = activeCodeCompleteMembers[selectedIndex].RectTransform;
            RectTransform contentRect = codeCompleteRoot.content;
            RectTransform viewportRect = codeCompleteRoot.viewport;

            float viewportHeight = viewportRect.rect.height;

            Vector3[] itemWorldCorners = new Vector3[4];
            selectedItemRect.GetWorldCorners(itemWorldCorners); 
            Vector3[] viewportWorldCorners = new Vector3[4];
            viewportRect.GetWorldCorners(viewportWorldCorners);

            float itemTopY_World = itemWorldCorners[1].y;
            float itemBottomY_World = itemWorldCorners[0].y;
            float viewportTopY_World = viewportWorldCorners[1].y;
            float viewportBottomY_World = viewportWorldCorners[0].y;
            float currentScrollPos = codeCompleteRoot.verticalNormalizedPosition;
            float contentHeight = contentRect.rect.height;
            float targetScrollPos = currentScrollPos;

            if (itemTopY_World > viewportTopY_World)
            {
                float diff = itemTopY_World - viewportTopY_World;
                if (contentHeight > viewportHeight)
                {
                    targetScrollPos = currentScrollPos + (diff / (contentHeight - viewportHeight));
                }
            }
            else if (itemBottomY_World < viewportBottomY_World)
            {
                float diff = viewportBottomY_World - itemBottomY_World;
                if (contentHeight > viewportHeight)
                {
                    targetScrollPos = currentScrollPos - (diff / (contentHeight - viewportHeight));
                }
            }

            targetScrollPos = Mathf.Clamp01(targetScrollPos);

            if (Mathf.Abs(currentScrollPos - targetScrollPos) > 0.001f)
            {
                codeCompleteRoot.verticalNormalizedPosition = targetScrollPos;                
            }
        }

        private UI_CodeCompleteMember PopCodeCompleteMember()
        {
            if (codeCompleteMembersPool.Count == 0)
            {
                return Instantiate(codeCompleteMember, codeCompleteMemberRoot).GetComponent<UI_CodeCompleteMember>();
            }

            return codeCompleteMembersPool.Pop();
        }

        private void HideSuggestions()
        {
            isShow = false;
            ClearSuggestions();
            codeCompleteRoot.gameObject.SetActive(false);
            inputField.ActivateInputField();
        }

        private void ClearSuggestions()
        {
            foreach (UI_CodeCompleteMember ccm in activeCodeCompleteMembers)
            {
                ccm.gameObject.SetActive(false);
                codeCompleteMembersPool.Push(ccm);
            }

            activeCodeCompleteMembers.Clear();
        }

        private void OnSuggestionSelected(string suggestion)
        {
            string currentText = inputField.text;
            int caretPosition = inputField.caretPosition;

            int tokenStartIndex;
            if (caretPosition == 0)
            {
                tokenStartIndex = 0;
            }
            else
            {
                tokenStartIndex = currentText.LastIndexOfAny(TOKEN_SEPARATORS, caretPosition - 1);
                tokenStartIndex = (tokenStartIndex == -1) ? 0 : tokenStartIndex + 1;

                if (tokenStartIndex > caretPosition)
                {
                    tokenStartIndex = caretPosition;
                }
            }

            string textBeforeReplacement = currentText.Substring(0, tokenStartIndex);

            string textAfterReplacement = "";
            if (caretPosition < currentText.Length)
            {
                textAfterReplacement = currentText.Substring(caretPosition);
            }

            string newText = textBeforeReplacement + suggestion + textAfterReplacement;

            inputField.text = newText;

            int newCaretPosition = tokenStartIndex + suggestion.Length;
            inputField.caretPosition = newCaretPosition;
            inputField.ActivateInputField();
            HideSuggestions();
        }

        public void UpdateCompletePosition(Vector3 pos)
        {
            if (isShow)
                return;

            transform.position = pos;
        }

        public void UpdateSuggestions(string currentInput, int caretPosition)
        {
            if (string.IsNullOrEmpty(currentInput) || caretPosition == 0)
            { 
                HideSuggestions();
                return;
            }

            caretPosition = Mathf.Clamp(caretPosition, 0, currentInput.Length);

            int keywordStartIndex = currentInput.LastIndexOfAny(TOKEN_SEPARATORS, caretPosition - 1);
            keywordStartIndex = (keywordStartIndex == -1) ? 0 : keywordStartIndex + 1;

            string searchTarget = currentInput.Substring(keywordStartIndex, caretPosition - keywordStartIndex);
            bool isLastInputDot = IsLastInputDot(keywordStartIndex, caretPosition, currentInput);
            List<string> keywordList = GetKeywordList(keywordStartIndex, currentInput, isLastInputDot);

            if (keywordList == null)
            {
                HideSuggestions();
                return;
            }

            suggestionList = GetSuggestionList(keywordList, searchTarget, isLastInputDot);

            if (suggestionList.Count > 0)
            {
                ShowSuggestions();
            }
            else
            {
                HideSuggestions();
            }
        }


        public void AddGlobalKeyword(string newKeyword)
        {
            if (string.IsNullOrWhiteSpace(newKeyword))
                return;

            string trimmedKeyword = newKeyword.Trim();

            for (int i = 0; i < globalKeywords.Count; i++)
            {
                if (globalKeywords[i].Equals(trimmedKeyword, StringComparison.OrdinalIgnoreCase))
                    return;
            }

            globalKeywords.Add(trimmedKeyword);
        }

        public void AddStaticMemberKeyword(string mainKeyword, string subKeyword)
        {
            if (string.IsNullOrWhiteSpace(mainKeyword) || string.IsNullOrWhiteSpace(subKeyword))
                return;

            string trimmedMainKeyword = mainKeyword.Trim();
            string trimmedSubKeyword = subKeyword.Trim();

            if (!staticMembers.ContainsKey(trimmedMainKeyword))
            {
                staticMembers[trimmedMainKeyword] = new List<string>();
            }

            if (!staticMembers[trimmedMainKeyword].Contains(trimmedSubKeyword))
            {
                staticMembers[trimmedMainKeyword].Add(trimmedSubKeyword);
            }
        }

        public void Hide()
        {
            HideSuggestions();
        }
    }
}