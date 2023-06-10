using System;
using ImGuiColorTextEditNet;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace TextEdit.Tests;

[TestClass]
public class InsertionTests
{
    [TestMethod]
    public void InsertTest1()
    {
        var t = new TextEditor();
        Assert.AreEqual("", t.Text);

        t.Modify.EnterCharacter('a');
        Assert.AreEqual("a", t.Text);
        Assert.AreEqual((0, 1), t.CursorPosition);
        Assert.AreEqual(1, t.UndoCount);
        Assert.AreEqual(1, t.UndoIndex);

        t.Modify.EnterCharacter('b');
        Assert.AreEqual("ab", t.Text);
        Assert.AreEqual((0, 2), t.CursorPosition);
        Assert.AreEqual(2, t.UndoCount);
        Assert.AreEqual(2, t.UndoIndex);

        t.Undo();
        Assert.AreEqual("a", t.Text);
        Assert.AreEqual((0, 1), t.CursorPosition);
        Assert.AreEqual(2, t.UndoCount);
        Assert.AreEqual(1, t.UndoIndex);

        t.Redo();
        Assert.AreEqual("ab", t.Text);
        Assert.AreEqual((0, 2), t.CursorPosition);
        Assert.AreEqual(2, t.UndoCount);
        Assert.AreEqual(2, t.UndoIndex);

        t.Selection.Select((0, 0), (0, 2));
        t.Modify.EnterCharacter('c');
        Assert.AreEqual("c", t.Text);
        Assert.AreEqual((0, 1), t.CursorPosition);
        Assert.AreEqual(3, t.UndoCount);
        Assert.AreEqual(3, t.UndoIndex);

        t.Undo();
        Assert.AreEqual("ab", t.Text);
        Assert.AreEqual((0, 2), t.CursorPosition);
        Assert.AreEqual(3, t.UndoCount);
        Assert.AreEqual(2, t.UndoIndex);

        t.Redo();
        Assert.AreEqual("c", t.Text);
        Assert.AreEqual((0, 1), t.CursorPosition);
        Assert.AreEqual(3, t.UndoCount);
        Assert.AreEqual(3, t.UndoIndex);
    }

    [TestMethod]
    public void InsertNewLine()
    {
        var t = new TextEditor();
        Assert.AreEqual("", t.Text);

        t.Modify.EnterCharacter('\n');
        Assert.AreEqual(Environment.NewLine, t.Text);
        Assert.AreEqual((1, 0), t.CursorPosition);
        Assert.AreEqual(1, t.UndoCount);
        Assert.AreEqual(1, t.UndoIndex);

        t.Undo();
        Assert.AreEqual("", t.Text);
        Assert.AreEqual((0, 0), t.CursorPosition);
        Assert.AreEqual(1, t.UndoCount);
        Assert.AreEqual(0, t.UndoIndex);

        t.Redo();
        Assert.AreEqual(Environment.NewLine, t.Text);
        Assert.AreEqual((1, 0), t.CursorPosition);
        Assert.AreEqual(1, t.UndoCount);
        Assert.AreEqual(1, t.UndoIndex);

        t.Modify.EnterCharacter('a');
        Assert.AreEqual(Environment.NewLine + "a", t.Text);
        Assert.AreEqual((1, 1), t.CursorPosition);
        Assert.AreEqual(2, t.UndoCount);
        Assert.AreEqual(2, t.UndoIndex);

        t.Selection.SelectAll();
        t.Modify.EnterCharacter('\n');
        Assert.AreEqual(Environment.NewLine, t.Text);
        Assert.AreEqual((1, 0), t.CursorPosition);
        Assert.AreEqual(3, t.UndoCount);
        Assert.AreEqual(3, t.UndoIndex);

        t.Undo();
        Assert.AreEqual(Environment.NewLine + "a", t.Text);
        Assert.AreEqual((1, 1), t.CursorPosition);
        Assert.AreEqual(3, t.UndoCount);
        Assert.AreEqual(2, t.UndoIndex);

        t.Redo();
        Assert.AreEqual(Environment.NewLine, t.Text);
        Assert.AreEqual((1, 0), t.CursorPosition);
        Assert.AreEqual(3, t.UndoCount);
        Assert.AreEqual(3, t.UndoIndex);
    }

    [TestMethod]
    public void IndentBlockTest()
    {
        var tab = "\t";
        var before = $@"void main() // 0
{{ // 1
int a; // 2
for (a = 0; a < 10; a++) // 3
{tab}printf(""%d\n"", a); // 4
}} // 5
";

        var after = $@"void main() // 0
{{ // 1
{tab}int a; // 2
{tab}for (a = 0; a < 10; a++) // 3
{tab}{tab}printf(""%d\n"", a); // 4
}} // 5
";
        var t = new TextEditor { AllText = before };

        t.Selection.Select((2, 0), (4, 1));
        t.Modify.IndentSelection(false);
        Assert.AreEqual(after, t.Text);
        Assert.AreEqual(1, t.UndoCount);
        Assert.AreEqual(1, t.UndoIndex);

        t.Undo();
        Assert.AreEqual(before, t.Text);
        Assert.AreEqual(1, t.UndoCount);
        Assert.AreEqual(0, t.UndoIndex);

        t.Redo();
        Assert.AreEqual(after, t.Text);
        Assert.AreEqual(1, t.UndoCount);
        Assert.AreEqual(1, t.UndoIndex);

        t.Selection.Select((2, 0), (4, 1));
        t.Modify.IndentSelection(true);
        Assert.AreEqual(before, t.Text);
        Assert.AreEqual(2, t.UndoCount);
        Assert.AreEqual(2, t.UndoIndex);
    }
}