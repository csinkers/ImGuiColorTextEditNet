using System;
using ImGuiColorTextEditNet;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace TextEdit.Tests;

[TestClass]
public class InsertionTests
{
    [TestMethod] public void InsertTest1() => InsertTest1Inner(false, false);
    [TestMethod] public void InsertTest1Bp() => InsertTest1Inner(true, false);
    [TestMethod] public void InsertTest1Err() => InsertTest1Inner(false, true);

    void InsertTest1Inner(bool breakpoints, bool errors)
    {
        var t = new TextEditor();
        if (breakpoints) t.Breakpoints.Add(0, 1);
        if (errors) t.ErrorMarkers.Add(0, 1);
        Assert.AreEqual("", t.AllText);

        UndoHelper.TestUndo(t, x => x.Modify.EnterCharacter('a'));
        Assert.AreEqual("a", t.AllText);
        Assert.AreEqual((0, 1), t.CursorPosition);
        Assert.AreEqual(1, t.UndoCount);
        Assert.AreEqual(1, t.UndoIndex);

        UndoHelper.TestUndo(t, x => x.Modify.EnterCharacter('b'));
        Assert.AreEqual("ab", t.AllText);
        Assert.AreEqual((0, 2), t.CursorPosition);
        Assert.AreEqual(2, t.UndoCount);
        Assert.AreEqual(2, t.UndoIndex);

        t.Selection.Select((0, 0), (0, 2));
        UndoHelper.TestUndo(t, x => x.Modify.EnterCharacter('c'));
        Assert.AreEqual("c", t.AllText);
        Assert.AreEqual((0, 1), t.CursorPosition);
        Assert.AreEqual(3, t.UndoCount);
        Assert.AreEqual(3, t.UndoIndex);
    }

    [TestMethod] public void InsertNewLine() => InsertNewLineInner(false, false);
    [TestMethod] public void InsertNewLineBp() => InsertNewLineInner(true, false);
    [TestMethod] public void InsertNewLineErr() => InsertNewLineInner(false, true);

    void InsertNewLineInner(bool breakpoints, bool errors)
    {
        var t = new TextEditor();
        if (breakpoints) t.Breakpoints.Add(0, 1);
        if (errors) t.ErrorMarkers.Add(0, 1);
        Assert.AreEqual("", t.AllText);

        UndoHelper.TestUndo(t, x => x.Modify.EnterCharacter('\n'));
        Assert.AreEqual(Environment.NewLine, t.AllText);
        Assert.AreEqual((1, 0), t.CursorPosition);
        Assert.AreEqual(1, t.UndoCount);
        Assert.AreEqual(1, t.UndoIndex);

        UndoHelper.TestUndo(t, x => x.Modify.EnterCharacter('a'));
        Assert.AreEqual(Environment.NewLine + "a", t.AllText);
        Assert.AreEqual((1, 1), t.CursorPosition);
        Assert.AreEqual(2, t.UndoCount);
        Assert.AreEqual(2, t.UndoIndex);

        t.Selection.SelectAll();
        UndoHelper.TestUndo(t, x => x.Modify.EnterCharacter('\n'));
        Assert.AreEqual(Environment.NewLine, t.AllText);
        Assert.AreEqual((1, 0), t.CursorPosition);
        Assert.AreEqual(3, t.UndoCount);
        Assert.AreEqual(3, t.UndoIndex);
    }

    [TestMethod] public void IndentBlockTest() => IndentBlockTestInner(false, false);
    [TestMethod] public void IndentBlockTestBp() => IndentBlockTestInner(true, false);
    [TestMethod] public void IndentBlockTestErr() => IndentBlockTestInner(false, true);

    void IndentBlockTestInner(bool breakpoints, bool errors)
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
        if (breakpoints) t.Breakpoints.Add(3, 1);
        if (errors) t.ErrorMarkers.Add(3, 1);

        t.Selection.Select((2, 0), (4, 1));
        UndoHelper.TestUndo(t, x => x.Modify.IndentSelection(false));
        Assert.AreEqual(after, t.AllText);
        Assert.AreEqual(1, t.UndoCount);
        Assert.AreEqual(1, t.UndoIndex);

        t.Selection.Select((2, 0), (4, 1));
        UndoHelper.TestUndo(t, x => x.Modify.IndentSelection(true));
        Assert.AreEqual(before, t.AllText);
        Assert.AreEqual(2, t.UndoCount);
        Assert.AreEqual(2, t.UndoIndex);
    }
}
