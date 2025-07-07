using System;
using System.Collections.Generic;
using System.Linq;

namespace ImGuiColorTextEditNet.Operations;

internal class MetaOperation : IEditorOperation
{
    readonly List<IEditorOperation> _operations = [];

    public SelectionState Before;
    public SelectionState After;
    public int Count => _operations.Count;

    public void Add(IEditorOperation operation)
    {
        ArgumentNullException.ThrowIfNull(operation);
        _operations.Add(operation);
    }

    public void Apply(TextEditor editor)
    {
        foreach (var op in _operations)
            op.Apply(editor);

        editor.Selection.Select(After.Start, After.End);
        editor.Selection.Cursor = After.Cursor;
        editor.Text.PendingScrollRequest = editor.Selection.Cursor.Line;
    }

    public void Undo(TextEditor editor)
    {
        foreach (var op in _operations.AsEnumerable().Reverse())
            op.Undo(editor);

        editor.Selection.Select(Before.Start, Before.End);
        editor.Selection.Cursor = Before.Cursor;
        editor.Text.PendingScrollRequest = editor.Selection.Cursor.Line;
    }

    public object SerializeState() => _operations.Select(op => op.SerializeState()).ToList();
}
