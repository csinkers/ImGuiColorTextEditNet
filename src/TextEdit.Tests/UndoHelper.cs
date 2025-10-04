using System;
using ImGuiColorTextEditNet;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace TextEdit.Tests;

internal static class UndoHelper
{
    /// <summary>
    /// Tests undo handling for an operation that can be undone.
    /// </summary>
    public static void TestUndo(TextEditor editor, Action<TextEditor> func)
    {
        var initialState = editor.SerializeState();
        var initialUndo = editor.UndoStack.SerializeState();
        long v = editor.Version;

        func(editor);
        var afterState = editor.SerializeState();
        var afterUndo = editor.UndoStack.SerializeState();

        Assert.AreNotEqual(initialUndo, afterUndo); // Verify that an undo record is created.
        Assert.AreNotEqual(v, editor.Version);
        v = editor.Version;

        editor.Undo();
        var undoState = editor.SerializeState();
        Assert.AreNotEqual(v, editor.Version);
        v = editor.Version;

        editor.Redo();
        var redoState = editor.SerializeState();
        Assert.AreNotEqual(v, editor.Version);

        Assert.AreEqual(initialState, undoState);
        Assert.AreEqual(afterState, redoState);
    }

    /// <summary>
    /// Tests undo handling for an operation that should not result in an undo record.
    /// </summary>
    public static void TestNopUndo(TextEditor editor, Action<TextEditor> func)
    {
        var initialUndo = editor.UndoStack.SerializeState();
        func(editor);
        var afterUndo = editor.UndoStack.SerializeState();

        Assert.AreEqual(initialUndo, afterUndo);
    }
}
