using ImGuiColorTextEditNet;
using ImGuiColorTextEditNet.Editor;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace TextEdit.Tests;

[TestClass]
public class DeletionTests
{
    [TestMethod]
    public void BackspaceTest() => BackspaceTestInner(false, false);

    [TestMethod]
    public void BackspaceTestBp() => BackspaceTestInner(true, false);

    [TestMethod]
    public void BackspaceTestErr() => BackspaceTestInner(false, true);

    static void BackspaceTestInner(bool breakpoints, bool errors)
    {
        var t = new TextEditor { AllText = "abc" };
        if (breakpoints)
            t.Breakpoints.Add(0, 1);
        if (errors)
            t.ErrorMarkers.Add(0, 1);
        Assert.AreEqual((0, 0), t.CursorPosition);

        UndoHelper.TestNopUndo(t, TextEditorModify.Backspace); // Backspace at start of a line should do nothing
        Assert.AreEqual("abc", t.AllText);
        Assert.AreEqual(0, t.UndoCount);
        Assert.AreEqual(0, t.UndoIndex);

        t.Selection.Select((0, 0), (0, 1));
        UndoHelper.TestUndo(t, TextEditorModify.Backspace);
        Assert.AreEqual("bc", t.AllText);
        Assert.AreEqual(1, t.UndoCount);
        Assert.AreEqual(1, t.UndoIndex);

        t.CursorPosition = (0, 1);
        UndoHelper.TestUndo(t, TextEditorModify.Backspace);
        Assert.AreEqual("c", t.AllText);
        Assert.AreEqual(2, t.UndoCount);
        Assert.AreEqual(2, t.UndoIndex);
    }

    [TestMethod]
    public void BackspaceTestEol()
    {
        var t = new TextEditor { AllText = "abc" };
        t.CursorPosition = (0, 3);

        UndoHelper.TestUndo(t, TextEditorModify.Backspace);
        Assert.AreEqual("ab", t.AllText);
        Assert.AreEqual(1, t.UndoCount);
        Assert.AreEqual(1, t.UndoIndex);
    }

    [TestMethod]
    public void BackspaceTestMultiLine1() => BackspaceTestMultiLine1Inner(false, false);

    static void BackspaceTestMultiLine1Inner(bool breakpoints, bool errors)
    {
        var before =
            @"one
two
three";

        var after = @"onhree";

        var t = new TextEditor { AllText = before };
        if (breakpoints)
            t.Breakpoints.Add(0, 1);

        if (errors)
            t.ErrorMarkers.Add(0, 1);

        t.CursorPosition = (2, 1);
        t.Selection.Select((0, 2), (2, 1));

        UndoHelper.TestUndo(t, TextEditorModify.Backspace);
        Assert.AreEqual(after, t.AllText);
        Assert.AreEqual((0, 2), t.CursorPosition);
        Assert.AreEqual((0, 2), t.Selection.Start);
        Assert.AreEqual((0, 2), t.Selection.End);
        Assert.AreEqual(1, t.UndoCount);
        Assert.AreEqual(1, t.UndoIndex);
    }

    [TestMethod]
    public void BackspaceTestMultiLine2() => BackspaceTestMultiLine2Inner(false, false);

    static void BackspaceTestMultiLine2Inner(bool breakpoints, bool errors)
    {
        var before =
            @"one
two
three";

        var after = @"onhree";

        var t = new TextEditor { AllText = before };
        if (breakpoints)
            t.Breakpoints.Add(0, 1);
        if (errors)
            t.ErrorMarkers.Add(0, 1);

        t.CursorPosition = (0, 2);
        t.Selection.Select((0, 2), (2, 1));

        UndoHelper.TestUndo(t, TextEditorModify.Backspace);
        Assert.AreEqual(after, t.AllText);
        Assert.AreEqual((0, 2), t.CursorPosition);
        Assert.AreEqual((0, 2), t.Selection.Start);
        Assert.AreEqual((0, 2), t.Selection.End);
        Assert.AreEqual(1, t.UndoCount);
        Assert.AreEqual(1, t.UndoIndex);
    }

    [TestMethod]
    public void BackspaceTestMultiLine3() => BackspaceTestMultiLine3Inner(false, false);

    static void BackspaceTestMultiLine3Inner(bool breakpoints, bool errors)
    {
        var before =
            @"first
second
third";

        var after =
            @"firstsecond
third";

        var t = new TextEditor { AllText = before };
        if (breakpoints)
            t.Breakpoints.Add(0, 1);
        if (errors)
            t.ErrorMarkers.Add(0, 1);

        t.CursorPosition = (1, 0);

        UndoHelper.TestUndo(t, TextEditorModify.Backspace);
        Assert.AreEqual(after, t.AllText);
        Assert.AreEqual((0, 5), t.CursorPosition);
        Assert.AreEqual((0, 5), t.Selection.Start);
        Assert.AreEqual((0, 5), t.Selection.End);
        Assert.AreEqual(1, t.UndoCount);
        Assert.AreEqual(1, t.UndoIndex);
    }

    [TestMethod]
    public void DeleteTest() => DeleteTestInner(false, false);

    static void DeleteTestInner(bool breakpoints, bool errors)
    {
        var t = new TextEditor { AllText = "abc" };
        if (breakpoints)
            t.Breakpoints.Add(0, 1);
        if (errors)
            t.ErrorMarkers.Add(0, 1);

        Assert.AreEqual((0, 0), t.CursorPosition);

        UndoHelper.TestUndo(t, TextEditorModify.Delete);
        Assert.AreEqual("bc", t.AllText);
        Assert.AreEqual((0, 0), t.CursorPosition);
        Assert.AreEqual(1, t.UndoCount);
        Assert.AreEqual(1, t.UndoIndex);

        t.CursorPosition = (0, 2);
        UndoHelper.TestNopUndo(t, TextEditorModify.Delete);
        Assert.AreEqual("bc", t.AllText);
        Assert.AreEqual((0, 2), t.CursorPosition);
        Assert.AreEqual(1, t.UndoCount);
        Assert.AreEqual(1, t.UndoIndex);

        t.Selection.SelectAll();
        Assert.AreEqual((0, 2), t.CursorPosition);
        Assert.AreEqual((0, 0), t.Selection.Start);
        Assert.AreEqual((0, 2), t.Selection.End);

        UndoHelper.TestUndo(t, TextEditorModify.Delete);
        Assert.AreEqual("", t.AllText);
        Assert.AreEqual((0, 0), t.CursorPosition);
        Assert.AreEqual((0, 0), t.Selection.Start);
        Assert.AreEqual((0, 0), t.Selection.End);
        Assert.AreEqual(2, t.UndoCount);
        Assert.AreEqual(2, t.UndoIndex);
    }
}
