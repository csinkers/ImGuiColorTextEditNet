using ImGuiColorTextEditNet;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace TextEdit.Tests;

[TestClass]
public class DeletionTests
{
    [TestMethod] public void BackspaceTest() => BackspaceTestInner(false, false);
    [TestMethod] public void BackspaceTestBp() => BackspaceTestInner(true, false);
    [TestMethod] public void BackspaceTestErr() => BackspaceTestInner(false, true);

    void BackspaceTestInner(bool breakpoints, bool errors)
    {
        var t = new TextEditor { AllText = "abc" };
        if (breakpoints) t.Breakpoints.Add(0, 1);
        if (errors) t.ErrorMarkers.Add(0, 1);
        Assert.AreEqual((0, 0), t.CursorPosition);

        UndoHelper.TestUndo(t, x => x.Modify.Backspace());
        Assert.AreEqual("abc", t.AllText);
        Assert.AreEqual(0, t.UndoCount);
        Assert.AreEqual(0, t.UndoIndex);

        t.Selection.Select((0, 0), (0, 1));
        UndoHelper.TestUndo(t, x => x.Modify.Backspace());
        Assert.AreEqual("bc", t.AllText);
        Assert.AreEqual(1, t.UndoCount);
        Assert.AreEqual(1, t.UndoIndex);

        t.CursorPosition = (0, 1);
        UndoHelper.TestUndo(t, x => x.Modify.Backspace());
        Assert.AreEqual("c", t.AllText);
        Assert.AreEqual(2, t.UndoCount);
        Assert.AreEqual(2, t.UndoIndex);
    }

    [TestMethod] public void BackspaceTestMultiLine1() => BackspaceTestMultiLine1Inner(false, false);

    void BackspaceTestMultiLine1Inner(bool breakpoints, bool errors)
    {
        var before = @"one
two
three";

        var after = @"onhree";

        var t = new TextEditor { AllText = before };
        if (breakpoints) t.Breakpoints.Add(0, 1);
        if (errors) t.ErrorMarkers.Add(0, 1);
        t.Selection.Select((0,2), (2, 1));
        t.CursorPosition = (2, 1);

        UndoHelper.TestUndo(t, x => x.Modify.Backspace());
        Assert.AreEqual(after, t.AllText);
        Assert.AreEqual((0, 2), t.CursorPosition);
        Assert.AreEqual((0, 2), t.Selection.Start);
        Assert.AreEqual((0, 2), t.Selection.End);
        Assert.AreEqual(1, t.UndoCount);
        Assert.AreEqual(1, t.UndoIndex);
    }

    [TestMethod] public void BackspaceTestMultiLine2() => BackspaceTestMultiLine2Inner(false, false);

    public void BackspaceTestMultiLine2Inner(bool breakpoints, bool errors)
    {
        var before = @"one
two
three";

        var after = @"onhree";

        var t = new TextEditor { AllText = before };
        if (breakpoints) t.Breakpoints.Add(0, 1);
        if (errors) t.ErrorMarkers.Add(0, 1);

        t.Selection.Select((0,2), (2, 1));
        t.CursorPosition = (0, 2);

        UndoHelper.TestUndo(t, x => x.Modify.Backspace());
        Assert.AreEqual(after, t.AllText);
        Assert.AreEqual((0, 2), t.CursorPosition);
        Assert.AreEqual((0, 2), t.Selection.Start);
        Assert.AreEqual((0, 2), t.Selection.End);
        Assert.AreEqual(1, t.UndoCount);
        Assert.AreEqual(1, t.UndoIndex);
    }

    [TestMethod] public void DeleteTest() => DeleteTestInner(false, false);

    void DeleteTestInner(bool breakpoints, bool errors)
    {
        var t = new TextEditor { AllText = "abc" };
        if (breakpoints) t.Breakpoints.Add(0, 1);
        if (errors) t.ErrorMarkers.Add(0, 1);

        Assert.AreEqual((0, 0), t.CursorPosition);

        UndoHelper.TestUndo(t, x => x.Modify.Delete());
        Assert.AreEqual("bc", t.AllText);
        Assert.AreEqual((0, 0), t.CursorPosition);
        Assert.AreEqual(1, t.UndoCount);
        Assert.AreEqual(1, t.UndoIndex);

        t.CursorPosition = (0, 2);
        UndoHelper.TestUndo(t, x => x.Modify.Delete());
        Assert.AreEqual("bc", t.AllText);
        Assert.AreEqual((0, 2), t.CursorPosition);
        Assert.AreEqual(1, t.UndoCount);
        Assert.AreEqual(1, t.UndoIndex);

        t.Selection.SelectAll();
        Assert.AreEqual((0,2), t.CursorPosition);
        Assert.AreEqual((0,0), t.Selection.Start);
        Assert.AreEqual((0,2), t.Selection.End);

        UndoHelper.TestUndo(t, x => x.Modify.Delete());
        Assert.AreEqual("", t.AllText);
        Assert.AreEqual((0,0), t.CursorPosition);
        Assert.AreEqual((0,0), t.Selection.Start);
        Assert.AreEqual((0,0), t.Selection.End);
        Assert.AreEqual(2, t.UndoCount);
        Assert.AreEqual(2, t.UndoIndex);
    }
}
