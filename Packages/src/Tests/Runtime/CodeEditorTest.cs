using C2M.CodeEditor;
using NUnit.Framework;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.InputSystem.Users;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public class CodeEditorTest
{
    private InputTestFixture inputTestFixture;
    private Keyboard keyboard;
    private CodeEditor codeEditor;
    private InputActionsManager inputActionsManager;
    private bool sceneLoaded = false;
    private PlayerInput playerInput;
    private InputUser user;

    private readonly string TEST_SCENE_NAME = "TestScene";
    private readonly string TEST_DEFAULT_TEXT = "class";


    [UnitySetUp]
    public IEnumerator UnitySetUp()
    {
        if (!sceneLoaded)
        {
            AsyncOperation ao = SceneManager.LoadSceneAsync(TEST_SCENE_NAME, LoadSceneMode.Single);

            while (!ao.isDone)
            {
                yield return null;
            }

            sceneLoaded = true;
            yield return null;
        }

        if (inputTestFixture == null)
        {
            inputTestFixture = new InputTestFixture();
            inputTestFixture.Setup();
        }

        keyboard = InputSystem.AddDevice<Keyboard>();
        codeEditor = GameObject.FindFirstObjectByType<CodeEditor>();
        yield return null;

        inputActionsManager = InputActionsManager.Instance;
        inputActionsManager.TryGetComponent(out playerInput);

        if (playerInput == null)
        {
            playerInput = codeEditor.gameObject.AddComponent<PlayerInput>();
        }

        if (!user.valid)
        {
            user = InputUser.PerformPairingWithDevice(keyboard, user: playerInput.user);
        }
        inputActionsManager.InputActions.devices = new InputDevice[] { keyboard };
        yield return null;

        codeEditor.text = "";
        EventSystem.current.SetSelectedGameObject(codeEditor.gameObject);
        yield return null;
    }

    [UnityTearDown]
    public IEnumerator UnityTearDown()
    {

        if (user.valid)
        {
            user.UnpairDevicesAndRemoveUser();
        }

        if (playerInput != null)
        {
            Object.DestroyImmediate(playerInput);
            playerInput = null;
        }

        inputTestFixture.TearDown();
        inputTestFixture = null;
        yield return null;
    }


    [UnityTest]
    public IEnumerator Test_HighlightKeywordText()
    {
        codeEditor.text = TEST_DEFAULT_TEXT;
        yield return null;
        Assert.IsTrue(codeEditor.HighlightText.Contains("<color="));
    }

    [UnityTest]
    public IEnumerator Test_Undo()
    {

        codeEditor.text = "un";
        yield return new WaitForSeconds(codeEditor.UndoIdleThresholdSeconds + 0.1f);
        codeEditor.text += "do";

        inputTestFixture.Press(keyboard.leftCtrlKey);
        inputTestFixture.PressAndRelease(keyboard.zKey);
        yield return null;

        inputTestFixture.Release(keyboard.leftCtrlKey);
        yield return null;

        Assert.AreEqual("un", codeEditor.text);
    }

    [UnityTest]
    public IEnumerator Test_Redo()
    {

        codeEditor.text = "un";
        yield return new WaitForSeconds(codeEditor.UndoIdleThresholdSeconds + 0.1f);
        codeEditor.text += "do";

        inputTestFixture.Press(keyboard.leftCtrlKey);
        inputTestFixture.PressAndRelease(keyboard.zKey);
        yield return null;

        inputTestFixture.Release(keyboard.leftCtrlKey);
        yield return null;

        Assert.AreEqual("un", codeEditor.text);

        inputTestFixture.Press(keyboard.leftCtrlKey);
        inputTestFixture.PressAndRelease(keyboard.yKey);
        yield return null;

        inputTestFixture.Release(keyboard.leftCtrlKey);
        yield return null;

        Assert.AreEqual("undo", codeEditor.text);
    }

    [UnityTest]
    public IEnumerator Test_Indent()
    {
        string text = TEST_DEFAULT_TEXT + "\n" + TEST_DEFAULT_TEXT;

        codeEditor.text = text;

        yield return null;

        codeEditor.selectionAnchorPosition = 2;
        codeEditor.selectionFocusPosition = text.Length - 3;
        codeEditor.stringPosition = codeEditor.selectionFocusPosition;
        yield return null;

        inputTestFixture.PressAndRelease(keyboard.yKey);
        inputTestFixture.PressAndRelease(keyboard.yKey);
        inputTestFixture.PressAndRelease(keyboard.tabKey);
        InputSystem.Update();
        //codeEditor.text = TEST_DEFAULT_TEXT;
        //codeEditor.onValueChanged.Invoke(text);


        yield return new WaitForEndOfFrame();

        Assert.AreEqual("\t" + TEST_DEFAULT_TEXT + "\n" + "\t" + TEST_DEFAULT_TEXT, codeEditor.text);
    }

    [UnityTest]
    public IEnumerator Test_UnIndent()
    {
        string text = "\t" + TEST_DEFAULT_TEXT + "\n" + "\t" + TEST_DEFAULT_TEXT;
        codeEditor.text = text;
        //codeEditor.stringPosition = TEST_DEFAULT_TEXT.Length;
        yield return null;

        codeEditor.selectionAnchorPosition = 2;
        codeEditor.selectionFocusPosition = text.Length - 5;

        yield return null;
        inputTestFixture.PressAndRelease(keyboard.tabKey);
        InputSystem.Update();
        yield return new WaitForEndOfFrame();

        inputTestFixture.Press(keyboard.leftShiftKey);
        inputTestFixture.PressAndRelease(keyboard.tabKey);
        InputSystem.Update();
        yield return new WaitForEndOfFrame();
        codeEditor.onValueChanged.Invoke(codeEditor.text);
        yield return new WaitForEndOfFrame();

        inputTestFixture.Release(keyboard.leftShiftKey);

        Assert.AreEqual(TEST_DEFAULT_TEXT + "\n" + TEST_DEFAULT_TEXT, codeEditor.text);
    }

    [UnityTest]
    public IEnumerator Test_AutoComplete()
    {
        string text = TEST_DEFAULT_TEXT.Substring(0, 3);

        codeEditor.text = text;
        codeEditor.stringPosition = codeEditor.text.Length;
        yield return null;
        codeEditor.onValueChanged.Invoke(text);
        yield return new WaitForEndOfFrame();

        inputTestFixture.PressAndRelease(keyboard.enterKey);
        yield return null;

        Assert.AreEqual(TEST_DEFAULT_TEXT, codeEditor.text);
    }

    //[UnityTest] 
    //public IEnumerator Test_()
    //{
    //    yield return null; 
    //}


}
