using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace C2M.CodeEditor
{
    public class UI_CodeCompleteMember : MonoBehaviour
    {
        [SerializeField]
        private Image bgImage;
        [SerializeField]
        private TMP_Text keywordText;
        [SerializeField]
        private Button button;

        private bool isSelected = false;
        private RectTransform rectTransform;

        public bool IsSelected { get => isSelected; }
        public RectTransform RectTransform { get => rectTransform; }

        private void UpdateBGColor(Color color)
        {
            if (bgImage == null)
                return;

            bgImage.color = color;
        }

        public void Init(string keyword, Action<bool> selectEvent)
        {
            rectTransform = GetComponent<RectTransform>();
            keywordText.text = keyword;

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() =>
            {
                selectEvent?.Invoke(isSelected);
            });
        }

        public void Select(Color color)
        {
            isSelected = true;
            UpdateBGColor(color);
        }

        public void UnSelect(Color color)
        {
            isSelected = false;
            UpdateBGColor(color);
        }
    }
}
