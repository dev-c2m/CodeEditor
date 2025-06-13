using TMPro;
using TMPro.EditorUtilities;
using UnityEditor;
using UnityEditor.UI;
using UnityEngine;
using UnityEngine.InputSystem;

namespace C2M.CodeEditor
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(CodeEditor), true)]
    public class Editor_CodeEditor : TMP_InputFieldEditor
    {
        SerializedProperty languageSetting;
        SerializedProperty horizontalScrollBar;
        SerializedProperty codeAutoComplete;
        SerializedProperty highlightText;
        SerializedProperty lineText;
        SerializedProperty globalPointSize;
        SerializedProperty globalFontAsset;
        float undoIdleThresholdSeconds;

        protected override void OnEnable()
        {
            base.OnEnable();
            languageSetting = serializedObject.FindProperty("languageSetting");
            horizontalScrollBar = serializedObject.FindProperty("horizontalScrollBar");
            codeAutoComplete = serializedObject.FindProperty("codeAutoComplete");
            highlightText = serializedObject.FindProperty("highlightText");
            lineText = serializedObject.FindProperty("lineText");
            globalPointSize = serializedObject.FindProperty("m_GlobalPointSize");
            globalFontAsset = serializedObject.FindProperty("m_GlobalFontAsset");
            undoIdleThresholdSeconds = serializedObject.FindProperty("undoIdleThresholdSeconds").floatValue;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            GUIStyle coloredLabel = new GUIStyle(EditorStyles.boldLabel);
            coloredLabel.normal.textColor = Color.yellow;
            EditorGUILayout.LabelField("CODE EDITOR SETTING", coloredLabel);
            EditorGUILayout.PropertyField(languageSetting);
            EditorGUILayout.PropertyField(horizontalScrollBar);
            EditorGUILayout.PropertyField(highlightText);
            EditorGUILayout.PropertyField(lineText);
            EditorGUILayout.PropertyField(codeAutoComplete);
            EditorGUILayout.FloatField("undoIdleThresholdSeconds", undoIdleThresholdSeconds);

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(globalFontAsset, new GUIContent("Font Asset", "Set the Font Asset for both Placeholder and Input Field text object."));
            if (EditorGUI.EndChangeCheck())
            {
                CodeEditor inputField = target as CodeEditor;
                inputField.SetFontAsset(globalFontAsset.objectReferenceValue as TMP_FontAsset);
            }

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(globalPointSize, new GUIContent("Point Size", "Set the point size of both Placeholder and Input Field text object."));
            if (EditorGUI.EndChangeCheck())
            {
                CodeEditor inputField = target as CodeEditor;
                inputField.SetPointSize(globalPointSize.floatValue);
            }

            serializedObject.ApplyModifiedProperties();

            EditorGUILayout.Space();
            DrawLine(1, new Color(0.5f, 0.5f, 0.5f, 1));
            EditorGUILayout.Space();

            EditorGUILayout.LabelField("DEFAULT SETTING", coloredLabel);
            base.OnInspectorGUI();
        }

        private void DrawLine(float height, Color color)
        {
            Rect rect = EditorGUILayout.GetControlRect(false, height);
            EditorGUI.DrawRect(rect, color);
        }
    }
}
