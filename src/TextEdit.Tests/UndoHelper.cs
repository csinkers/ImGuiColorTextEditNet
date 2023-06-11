using System;
using ImGuiColorTextEditNet;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace TextEdit.Tests;

internal static class UndoHelper
{
    public static void TestUndo(TextEditor editor, Action<TextEditor> func)
    {
        var initialState = editor.SerializeState();

        func(editor);
        var afterState = editor.SerializeState();

        editor.Undo();
        var undoState = editor.SerializeState();

        editor.Redo();
        var redoState = editor.SerializeState();

        Assert.AreEqual(initialState, undoState);
        Assert.AreEqual(afterState, redoState);
    }
}