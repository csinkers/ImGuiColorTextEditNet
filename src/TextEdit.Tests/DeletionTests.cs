using ImGuiColorTextEditNet;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace TextEdit.Tests;

[TestClass]
public class DeletionTests
{
    [TestMethod]
    public void BackspaceTest()
    {
        var t = new TextEditor { AllText = "abc" };
        Assert.AreEqual((0, 0), t.CursorPosition);

        t.Modify.Backspace();
        Assert.AreEqual("abc", t.Text);
        Assert.AreEqual(0, t.UndoCount);
        Assert.AreEqual(0, t.UndoIndex);

        t.Selection.Select((0, 0), (0, 1));
        t.Modify.Backspace();
        Assert.AreEqual("bc", t.Text);
        Assert.AreEqual(1, t.UndoCount);
        Assert.AreEqual(1, t.UndoIndex);

        t.CursorPosition = (0, 1);
        t.Modify.Backspace();
        Assert.AreEqual("c", t.Text);
        Assert.AreEqual(2, t.UndoCount);
        Assert.AreEqual(2, t.UndoIndex);
    }

    [TestMethod]
    public void BackspaceTestMultiLine1()
    {
        var before = @"one
two
three";

        var after = @"onhree";

        var t = new TextEditor { AllText = before };
        t.Selection.Select((0,2), (2, 1));
        t.CursorPosition = (2, 1);

        t.Modify.Backspace();
        Assert.AreEqual(after, t.Text);
        Assert.AreEqual((0, 2), t.CursorPosition);
        Assert.AreEqual((0, 2), t.Selection.Start);
        Assert.AreEqual((0, 2), t.Selection.End);
        Assert.AreEqual(1, t.UndoCount);
        Assert.AreEqual(1, t.UndoIndex);

        t.Undo();
        Assert.AreEqual(before, t.Text);
        Assert.AreEqual((2, 1), t.CursorPosition);
        Assert.AreEqual((0, 2), t.Selection.Start);
        Assert.AreEqual((2, 1), t.Selection.End);
        Assert.AreEqual(1, t.UndoCount);
        Assert.AreEqual(0, t.UndoIndex);

        t.Redo();
        Assert.AreEqual(after, t.Text);
        Assert.AreEqual((0, 2), t.CursorPosition);
        Assert.AreEqual((0, 2), t.Selection.Start);
        Assert.AreEqual((0, 2), t.Selection.End);
        Assert.AreEqual(1, t.UndoCount);
        Assert.AreEqual(1, t.UndoIndex);
    }

    [TestMethod]
    public void BackspaceTestMultiLine2()
    {
        var before = @"one
two
three";

        var after = @"onhree";

        var t = new TextEditor { AllText = before };
        t.Selection.Select((0,2), (2, 1));
        t.CursorPosition = (0, 2);

        t.Modify.Backspace();
        Assert.AreEqual(after, t.Text);
        Assert.AreEqual((0, 2), t.CursorPosition);
        Assert.AreEqual((0, 2), t.Selection.Start);
        Assert.AreEqual((0, 2), t.Selection.End);
        Assert.AreEqual(1, t.UndoCount);
        Assert.AreEqual(1, t.UndoIndex);

        t.Undo();
        Assert.AreEqual(before, t.Text);
        Assert.AreEqual((0, 2), t.CursorPosition);
        Assert.AreEqual((0, 2), t.Selection.Start);
        Assert.AreEqual((2, 1), t.Selection.End);
        Assert.AreEqual(1, t.UndoCount);
        Assert.AreEqual(0, t.UndoIndex);

        t.Redo();
        Assert.AreEqual(after, t.Text);
        Assert.AreEqual((0, 2), t.CursorPosition);
        Assert.AreEqual((0, 2), t.Selection.Start);
        Assert.AreEqual((0, 2), t.Selection.End);
        Assert.AreEqual(1, t.UndoCount);
        Assert.AreEqual(1, t.UndoIndex);
    }

    [TestMethod]
    public void DeleteTest()
    {
        var t = new TextEditor { AllText = "abc" };
        Assert.AreEqual((0, 0), t.CursorPosition);

        t.Modify.Delete();
        Assert.AreEqual("bc", t.Text);
        Assert.AreEqual((0, 0), t.CursorPosition);
        Assert.AreEqual(1, t.UndoCount);
        Assert.AreEqual(1, t.UndoIndex);

        t.Undo();
        Assert.AreEqual("abc", t.Text);
        Assert.AreEqual((0, 0), t.CursorPosition);
        Assert.AreEqual(1, t.UndoCount);
        Assert.AreEqual(0, t.UndoIndex);

        t.Redo();
        Assert.AreEqual("bc", t.Text);
        Assert.AreEqual((0, 0), t.CursorPosition);
        Assert.AreEqual(1, t.UndoCount);
        Assert.AreEqual(1, t.UndoIndex);

        t.CursorPosition = (0, 2);
        t.Modify.Delete();
        Assert.AreEqual("bc", t.Text);
        Assert.AreEqual((0, 2), t.CursorPosition);
        Assert.AreEqual(1, t.UndoCount);
        Assert.AreEqual(1, t.UndoIndex);

        t.Selection.SelectAll();
        Assert.AreEqual((0,2), t.CursorPosition);
        Assert.AreEqual((0,0), t.Selection.Start);
        Assert.AreEqual((0,2), t.Selection.End);

        t.Modify.Delete();
        Assert.AreEqual("", t.Text);
        Assert.AreEqual((0,0), t.CursorPosition);
        Assert.AreEqual((0,0), t.Selection.Start);
        Assert.AreEqual((0,0), t.Selection.End);
        Assert.AreEqual(2, t.UndoCount);
        Assert.AreEqual(2, t.UndoIndex);

        t.Undo();
        Assert.AreEqual("bc", t.Text);
        Assert.AreEqual((0, 2), t.CursorPosition);
        Assert.AreEqual((0,0), t.Selection.Start);
        Assert.AreEqual((0,2), t.Selection.End);
        Assert.AreEqual(2, t.UndoCount);
        Assert.AreEqual(1, t.UndoIndex);

        t.Redo();
        Assert.AreEqual("", t.Text);
        Assert.AreEqual((0,0), t.CursorPosition);
        Assert.AreEqual((0,0), t.Selection.Start);
        Assert.AreEqual((0,0), t.Selection.End);
        Assert.AreEqual(2, t.UndoCount);
        Assert.AreEqual(2, t.UndoIndex);
    }
}