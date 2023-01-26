using ImGuiColorTextEditNet;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace TextEdit.Tests;

[TestClass]
public class DeletionTests
{
    [TestMethod]
    public void BackspaceTest()
    {
        var t = new TextEditor { Text = "abc" };
        Assert.AreEqual((0, 0), t.CursorPosition);

        t.Backspace();
        Assert.AreEqual("abc", t.Text);
        Assert.AreEqual(0, t.UndoCount);
        Assert.AreEqual(0, t._undoIndex);

        t.SetSelection((0, 0), (0, 1));
        t.Backspace();
        Assert.AreEqual("bc", t.Text);
        Assert.AreEqual(1, t.UndoCount);
        Assert.AreEqual(1, t._undoIndex);

        t.CursorPosition = (0, 1);
        t.Backspace();
        Assert.AreEqual("c", t.Text);
        Assert.AreEqual(2, t.UndoCount);
        Assert.AreEqual(2, t._undoIndex);
    }

    [TestMethod]
    public void BackspaceTestMultiLine1()
    {
        var before = @"one
two
three";

        var after = @"onhree";

        var t = new TextEditor { Text = before };
        t.SetSelection((0,2), (2, 1));
        t.CursorPosition = (2, 1);

        t.Backspace();
        Assert.AreEqual(after, t.Text);
        Assert.AreEqual((0, 2), t.CursorPosition);
        Assert.AreEqual((0, 2), t.SelectionStart);
        Assert.AreEqual((0, 2), t.SelectionEnd);
        Assert.AreEqual(1, t.UndoCount);
        Assert.AreEqual(1, t._undoIndex);

        t.Undo();
        Assert.AreEqual(before, t.Text);
        Assert.AreEqual((2, 1), t.CursorPosition);
        Assert.AreEqual((0, 2), t.SelectionStart);
        Assert.AreEqual((2, 1), t.SelectionEnd);
        Assert.AreEqual(1, t.UndoCount);
        Assert.AreEqual(0, t._undoIndex);

        t.Redo();
        Assert.AreEqual(after, t.Text);
        Assert.AreEqual((0, 2), t.CursorPosition);
        Assert.AreEqual((0, 2), t.SelectionStart);
        Assert.AreEqual((0, 2), t.SelectionEnd);
        Assert.AreEqual(1, t.UndoCount);
        Assert.AreEqual(1, t._undoIndex);
    }

    [TestMethod]
    public void BackspaceTestMultiLine2()
    {
        var before = @"one
two
three";

        var after = @"onhree";

        var t = new TextEditor { Text = before };
        t.SetSelection((0,2), (2, 1));
        t.CursorPosition = (0, 2);

        t.Backspace();
        Assert.AreEqual(after, t.Text);
        Assert.AreEqual((0, 2), t.CursorPosition);
        Assert.AreEqual((0, 2), t.SelectionStart);
        Assert.AreEqual((0, 2), t.SelectionEnd);
        Assert.AreEqual(1, t.UndoCount);
        Assert.AreEqual(1, t._undoIndex);

        t.Undo();
        Assert.AreEqual(before, t.Text);
        Assert.AreEqual((0, 2), t.CursorPosition);
        Assert.AreEqual((0, 2), t.SelectionStart);
        Assert.AreEqual((2, 1), t.SelectionEnd);
        Assert.AreEqual(1, t.UndoCount);
        Assert.AreEqual(0, t._undoIndex);

        t.Redo();
        Assert.AreEqual(after, t.Text);
        Assert.AreEqual((0, 2), t.CursorPosition);
        Assert.AreEqual((0, 2), t.SelectionStart);
        Assert.AreEqual((0, 2), t.SelectionEnd);
        Assert.AreEqual(1, t.UndoCount);
        Assert.AreEqual(1, t._undoIndex);
    }

    [TestMethod]
    public void DeleteTest()
    {
        var t = new TextEditor { Text = "abc" };
        Assert.AreEqual((0, 0), t.CursorPosition);

        t.Delete();
        Assert.AreEqual("bc", t.Text);
        Assert.AreEqual((0, 0), t.CursorPosition);
        Assert.AreEqual(1, t.UndoCount);
        Assert.AreEqual(1, t._undoIndex);

        t.Undo();
        Assert.AreEqual("abc", t.Text);
        Assert.AreEqual((0, 0), t.CursorPosition);
        Assert.AreEqual(1, t.UndoCount);
        Assert.AreEqual(0, t._undoIndex);

        t.Redo();
        Assert.AreEqual("bc", t.Text);
        Assert.AreEqual((0, 0), t.CursorPosition);
        Assert.AreEqual(1, t.UndoCount);
        Assert.AreEqual(1, t._undoIndex);

        t.CursorPosition = (0, 2);
        t.Delete();
        Assert.AreEqual("bc", t.Text);
        Assert.AreEqual((0, 2), t.CursorPosition);
        Assert.AreEqual(1, t.UndoCount);
        Assert.AreEqual(1, t._undoIndex);

        t.SelectAll();
        Assert.AreEqual((0,2), t.CursorPosition);
        Assert.AreEqual((0,0), t.SelectionStart);
        Assert.AreEqual((0,2), t.SelectionEnd);

        t.Delete();
        Assert.AreEqual("", t.Text);
        Assert.AreEqual((0,0), t.CursorPosition);
        Assert.AreEqual((0,0), t.SelectionStart);
        Assert.AreEqual((0,0), t.SelectionEnd);
        Assert.AreEqual(2, t.UndoCount);
        Assert.AreEqual(2, t._undoIndex);

        t.Undo();
        Assert.AreEqual("bc", t.Text);
        Assert.AreEqual((0, 2), t.CursorPosition);
        Assert.AreEqual((0,0), t.SelectionStart);
        Assert.AreEqual((0,2), t.SelectionEnd);
        Assert.AreEqual(2, t.UndoCount);
        Assert.AreEqual(1, t._undoIndex);

        t.Redo();
        Assert.AreEqual("", t.Text);
        Assert.AreEqual((0,0), t.CursorPosition);
        Assert.AreEqual((0,0), t.SelectionStart);
        Assert.AreEqual((0,0), t.SelectionEnd);
        Assert.AreEqual(2, t.UndoCount);
        Assert.AreEqual(2, t._undoIndex);
    }
}