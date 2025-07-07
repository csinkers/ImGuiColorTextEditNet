using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using ImGuiColorTextEditNet.Operations;

namespace ImGuiColorTextEditNet.Editor;

internal class TextEditorUndoStack
{
    readonly TextEditorOptions _options;
    readonly TextEditorText _text;

    readonly List<IEditorOperation> _undoBuffer = new();
    int _undoIndex;

    internal TextEditorUndoStack(TextEditorText text, TextEditorOptions options)
    {
        _text = text ?? throw new ArgumentNullException(nameof(text));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _text.AllTextReplaced += Clear;
    }

    internal void Clear()
    {
        _undoBuffer.Clear();
        _undoIndex = 0;
    }

    internal int UndoCount => _undoBuffer.Count; // Only for unit testing
    internal int UndoIndex => _undoIndex; // Only for unit testing

    internal bool CanUndo() => !_options.IsReadOnly && _undoIndex > 0;

    internal bool CanRedo() => !_options.IsReadOnly && _undoIndex < _undoBuffer.Count;

    internal void AddUndo(IEditorOperation operation)
    {
        Util.Assert(!_options.IsReadOnly);

        // If we are in the middle of the undo stack, remove all records after the current index
        if (_undoIndex < _undoBuffer.Count)
            _undoBuffer.RemoveRange(_undoIndex, _undoBuffer.Count - _undoIndex);

        _undoBuffer.Insert(_undoIndex, operation);
        ++_undoIndex;
    }

    internal void Undo(TextEditor editor, int aSteps = 1)
    {
        while (CanUndo() && aSteps-- > 0)
        {
            var operation = _undoBuffer[--_undoIndex];
            operation.Undo(editor);
        }
    }

    internal void Redo(TextEditor editor, int aSteps = 1)
    {
        while (CanRedo() && aSteps-- > 0)
        {
            var operation = _undoBuffer[_undoIndex++];
            operation.Apply(editor);
        }
    }

    public object SerializeState()
    {
        var state = new
        {
            UndoIndex = _undoIndex,
            UndoBuffer = _undoBuffer.Select(x => x.SerializeState()).ToList(),
        };

        return JsonSerializer.Serialize(state, new JsonSerializerOptions { WriteIndented = true });
    }

    public void Do(IEditorOperation operation, TextEditor e)
    {
        operation.Apply(e);
        AddUndo(operation);
    }
}
