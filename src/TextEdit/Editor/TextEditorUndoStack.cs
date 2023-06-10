using System;
using System.Collections.Generic;

namespace ImGuiColorTextEditNet.Editor;

internal class TextEditorUndoStack
{
    readonly TextEditorOptions _options;
    readonly TextEditorText _text;
    readonly TextEditorColor _color;

    readonly List<UndoRecord> _undoBuffer = new();
    int _undoIndex;

    internal TextEditorUndoStack(TextEditorText text, TextEditorColor color, TextEditorOptions options)
    {
        _text = text ?? throw new ArgumentNullException(nameof(text));
        _color = color ?? throw new ArgumentNullException(nameof(color));
        _options = options ?? throw new ArgumentNullException(nameof(options));

        _text.AllTextReplaced += () =>
        {
            _undoBuffer.Clear();
            _undoIndex = 0;
        };
    }

    internal int UndoCount => _undoBuffer.Count; // Only for unit testing
    internal int UndoIndex => _undoIndex; // Only for unit testing
    internal bool CanUndo() => !_options.IsReadOnly && _undoIndex > 0;
    internal bool CanRedo() => !_options.IsReadOnly && _undoIndex < _undoBuffer.Count;

    internal void AddUndo(UndoRecord value)
    {
        Util.Assert(!_options.IsReadOnly);
        /*
        Debug.WriteLine("AddUndo: (@{0}.{1}) +\'{2}' [{3}.{4} .. {5}.{6}], -\'{7}', [{8}.{9} .. {10}.{11}] (@{12}.{13})\n",
            value.Before.Cursor.Line, value.Before.Cursor.Column,
            value.Added, value.AddedStart.Line, value.AddedStart.Column, value.AddedEnd.Line, value.AddedEnd.Column,
            value.Removed, value.RemovedStart.Line, value.RemovedStart.Column, value.RemovedEnd.Line, value.RemovedEnd.Column,
            value.After.Cursor.Line, value.After.Cursor.Column);
        */

        _undoBuffer.Insert(_undoIndex, value);
        ++_undoIndex;
    }

    internal void Undo(int aSteps = 1)
    {
        while (CanUndo() && aSteps-- > 0)
            Undo(_undoBuffer[--_undoIndex]);
    }

    internal void Redo(int aSteps = 1)
    {
        while (CanRedo() && aSteps-- > 0)
            Redo(_undoBuffer[_undoIndex++]);
    }

    void Undo(UndoRecord record)
    {
        if (!string.IsNullOrEmpty(record.Added))
        {
            _text.DeleteRange(record.AddedStart, record.AddedEnd);
            _color.InvalidateColor(record.AddedStart.Line - 1, record.AddedEnd.Line - record.AddedStart.Line + 2);
        }

        if (!string.IsNullOrEmpty(record.Removed))
        {
            var start = record.RemovedStart;
            _text.InsertTextAt(start, record.Removed);
            _color.InvalidateColor(record.RemovedStart.Line - 1, record.RemovedEnd.Line - record.RemovedStart.Line + 2);
        }

        _text.ScrollToCursor = true;
    }

    void Redo(UndoRecord record)
    {
        if (!string.IsNullOrEmpty(record.Removed))
        {
            _text.DeleteRange(record.RemovedStart, record.RemovedEnd);
            _color.InvalidateColor(record.RemovedStart.Line - 1, record.RemovedEnd.Line - record.RemovedStart.Line + 1);
        }

        if (!string.IsNullOrEmpty(record.Added))
        {
            var start = record.AddedStart;
            _text.InsertTextAt(start, record.Added);
            _color.InvalidateColor(record.AddedStart.Line - 1, record.AddedEnd.Line - record.AddedStart.Line + 1);
        }

        _text.ScrollToCursor = true;
    }
}