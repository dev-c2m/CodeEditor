using UnityEngine;
using UnityEngine.InputSystem;

namespace C2M.CodeEditor
{
    public class InputActionsManager : Singleton<InputActionsManager>
    {
        private CodeEditorAction inputActions;
        public CodeEditorAction InputActions { get => inputActions; }


        private void Awake()
        {
            inputActions = new CodeEditorAction();
        }

        private void OnEnable()
        {
            inputActions.CodeEditor_ShortCut.Enable();
            inputActions.CodeEditor_Normal.Enable();
            inputActions.CodeEditor_AutoComplete.Enable();
        }

        private void OnDisable()
        {
            inputActions.CodeEditor_ShortCut.Disable();
            inputActions.CodeEditor_Normal.Disable();
            inputActions.CodeEditor_AutoComplete.Disable();
        }

        public bool IsInputUndo => inputActions.CodeEditor_ShortCut.Undo.WasPressedThisFrame();
        public bool IsInputRedo => inputActions.CodeEditor_ShortCut.Redo.WasPressedThisFrame();
        public bool IsInputRedo2 => inputActions.CodeEditor_ShortCut.Redo2.WasPressedThisFrame();
        public bool IsInputPaste => inputActions.CodeEditor_ShortCut.Paste.WasPressedThisFrame();
        public bool IsInputUnIndent => inputActions.CodeEditor_ShortCut.UnIndent.WasPressedThisFrame();

        public bool IsInputEnter => inputActions.CodeEditor_Normal.Enter.WasPressedThisFrame();
        public bool IsInputTab => inputActions.CodeEditor_Normal.Tab.WasPressedThisFrame(); 
        public bool IsInputEscape => inputActions.CodeEditor_Normal.Escape.WasPressedThisFrame();
        public bool IsInputBackspace => inputActions.CodeEditor_Normal.Backspace.WasPressedThisFrame();
        public bool IsPressedBackspace => inputActions.CodeEditor_Normal.Backspace.IsPressed();

        public bool IsInputArrowDown => inputActions.CodeEditor_AutoComplete.ArrowDown.WasPressedThisFrame();
        public bool IsInputArrowUp => inputActions.CodeEditor_AutoComplete.ArrowUp.WasPressedThisFrame();

        public bool IsInputAutoComplete => IsInputArrowDown || IsInputArrowUp || IsInputEnter || IsInputTab || IsInputEscape;

    }
}
