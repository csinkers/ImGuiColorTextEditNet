using System;
using System.Text;
using ImGuiNET;

namespace ImGuiColorTextEditNet.Editor;

/// <summary>Provides methods for modifying text, e.g. copy, cut, paste, delete, and character entry.</summary>
public class TextEditorModify
{
    readonly SimpleCache<char, string> _charLabelCache = new("char strings", x => x.ToString());
    readonly TextEditorSelection _selection;
    readonly TextEditorText _text;
    readonly TextEditorUndoStack _undo;
    readonly TextEditorOptions _options;
    readonly TextEditorColor _color;

    internal TextEditorModify(TextEditorSelection selection, TextEditorText text, TextEditorUndoStack undo, TextEditorOptions options, TextEditorColor color)
    {
        _selection = selection ?? throw new ArgumentNullException(nameof(selection));
        _text = text ?? throw new ArgumentNullException(nameof(text));
        _undo = undo ?? throw new ArgumentNullException(nameof(undo));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _color = color ?? throw new ArgumentNullException(nameof(color));
    }

    /// <summary>Copies the currently selected text to the clipboard.</summary>
    public void Copy()
    {
        if (_selection.HasSelection)
        {
            ImGui.SetClipboardText(_selection.GetSelectedText());
        }
        else
        {
            if (_text.LineCount != 0)
            {
                StringBuilder sb = new();

                var line = _text.GetLine(_selection.GetActualCursorCoordinates().Line);
                foreach (var g in line)
                    sb.Append(g.Char);

                ImGui.SetClipboardText(sb.ToString());
            }
        }
    }

    /// <summary>Cuts the currently selected text, copying it to the clipboard and removing it from the editor.</summary>
    public void Cut()
    {
        if (_options.IsReadOnly)
        {
            Copy();
            return;
        }

        if (!_selection.HasSelection)
            return;

        Copy();
        var undo = DeleteSelection();
        _undo.AddUndo(undo);
    }

    /// <summary>Pastes text from the clipboard into the editor at the current cursor position or replaces the selection if any exists.</summary>
    public void Paste()
    {
        Util.Assert(!_options.IsReadOnly);

        unsafe
        {
            if(ImGuiNative.igGetClipboardText() == null)
                return;
        }

        var clipText = ImGui.GetClipboardText();
        if (string.IsNullOrEmpty(clipText))
            return;

        UndoRecord u = _selection.HasSelection 
            ? DeleteSelection() 
            : new() { Before = _selection.State };

        u.Added = clipText;
        u.AddedStart = _selection.GetActualCursorCoordinates();

        InsertTextAtCursor(clipText);

        u.AddedEnd = _selection.GetActualCursorCoordinates();
        u.After = _selection.State;
        _undo.AddUndo(u);
    }

    /// <summary>Deletes the currently selected text or the character at the cursor position if no selection exists.</summary>
    public void Delete()
    {
        if (_options.IsReadOnly)
            return;

        if (_text.LineCount == 0)
            return;

        if (_selection.HasSelection)
        {
            var u = DeleteSelection();
            _undo.AddUndo(u);
        }
        else
        {
            var pos = _selection.GetActualCursorCoordinates();
            _selection.Cursor = pos;

            var u = new UndoRecord { Before = _selection.State };

            if (pos.Column == _text.GetLineMaxColumn(pos.Line))
            {
                if (pos.Line == _text.LineCount - 1)
                    return;

                u.Removed = "\n";
                u.RemovedStart = u.RemovedEnd = _selection.GetActualCursorCoordinates();
                _text.Advance(u.RemovedEnd);
                _text.AppendToLine(pos.Line, _text.GetLineText(pos.Line + 1));
                _text.RemoveLine(pos.Line + 1);
            }
            else
            {
                var cindex = _text.GetCharacterIndex(pos);
                u.RemovedStart = u.RemovedEnd = _selection.GetActualCursorCoordinates();
                u.RemovedEnd.Column++;
                u.Removed = _text.GetText(u.RemovedStart, u.RemovedEnd);

                _text.RemoveInLine(pos.Line, cindex, cindex + 1);
            }

            u.After = _selection.State;
            _color.InvalidateColor(pos.Line, 1);
            _undo.AddUndo(u);
        }
    }

    /// <summary>Inserts a character at the current cursor position or replaces the selection if any exists.</summary>
    public void EnterCharacter(char c)
    {
        Util.Assert(!_options.IsReadOnly);
        var u =
            _selection.HasSelection
            ? DeleteSelection()
            : new() { Before = _selection.State };

        var coord = _selection.GetActualCursorCoordinates();
        u.AddedStart = coord;

        Util.Assert(_text.LineCount != 0);

        if (c == '\n')
        {
            var line = _text.GetLine(coord.Line);
            var newLine = new Line();

            if (_color.SyntaxHighlighter.AutoIndentation)
                for (int it = 0; it < line.Length && char.IsAscii(line[it].Char) && TextEditorText.IsBlank(line[it].Char); ++it)
                    newLine.Glyphs.Add(line[it]);

            int whitespaceSize = newLine.Glyphs.Count;
            var cindex = _text.GetCharacterIndex(coord);
            foreach (var glyph in line[cindex..])
                newLine.Glyphs.Add(glyph);

            _text.InsertLine(coord.Line + 1, newLine);
            _text.RemoveInLine(coord.Line, cindex, line.Length);
            _selection.Cursor = (coord.Line + 1, _text.GetCharacterColumn(coord.Line + 1, whitespaceSize));
            u.Added = "\n";
        }
        else
        {
            var line = _text.GetLine(coord.Line);
            var cindex = _text.GetCharacterIndex(coord);

            if (_options.IsOverwrite && cindex < line.Length)
            {
                u.RemovedStart = _selection.Cursor;
                u.RemovedEnd = (coord.Line, _text.GetCharacterColumn(coord.Line, cindex + 1));

                u.Removed += line[cindex].Char;
                _text.RemoveInLine(coord.Line, cindex, cindex + 1);
            }

            _text.InsertCharAt(coord, c);
            u.Added = _charLabelCache.Get(c);

            _selection.Cursor = (coord.Line, _text.GetCharacterColumn(coord.Line, cindex + 1));
        }

        u.AddedEnd = _selection.GetActualCursorCoordinates();
        u.After = _selection.State;

        _undo.AddUndo(u);

        _color.InvalidateColor(coord.Line - 1, 3);
        _text.PendingScrollRequest = coord.Line;
    }

    /// <summary>Indents or unindents the selected lines based on the current tab size.</summary>
    public void IndentSelection(bool shift)
    {
        Util.Assert(!_options.IsReadOnly);

        UndoRecord u = new() { Before = _selection.State };

        var start = _selection.Start;
        var end = _selection.End;
        var originalEnd = end;

        if (start > end)
            (start, end) = (end, start);

        start.Column = 0;
        // end._column = end._line < _text.LineCount ? _state._lines[end._line].Count : 0;
        if (end is { Column: 0, Line: > 0 })
            --end.Line;

        if (end.Line >= _text.LineCount)
            end.Line = _text.LineCount == 0 ? 0 : _text.LineCount - 1;

        end.Column = _text.GetLineMaxColumn(end.Line);

        //if (end._column >= GetLineMaxColumn(end._line))
        //    end._column = GetLineMaxColumn(end._line) - 1;

        u.RemovedStart = start;
        u.RemovedEnd = end;
        u.Removed = _text.GetText(start, end);

        bool modified = false;

        for (int i = start.Line; i <= end.Line; i++)
        {
            var line = _text.GetLine(i);
            if (shift)
            {
                if (line.Length != 0)
                {
                    if (line[0].Char == '\t')
                    {
                        _text.RemoveInLine(i, 0, 1);
                        modified = true;
                    }
                    else
                    {
                        for (int j = 0; j < _text.TabSize && line.Length != 0 && line[0].Char == ' '; j++)
                        {
                            _text.RemoveInLine(i, 0, 1);
                            modified = true;
                        }
                    }
                }
            }
            else
            {
                _text.InsertTextAt((i, 0), "\t");
                modified = true;
            }
        }

        if (modified)
        {
            start = (start.Line, _text.GetCharacterColumn(start.Line, 0));
            Coordinates rangeEnd;
            if (originalEnd.Column != 0)
            {
                end = (end.Line, _text.GetLineMaxColumn(end.Line));
                rangeEnd = end;
                u.Added = _text.GetText(start, end);
            }
            else
            {
                end = (originalEnd.Line, 0);
                rangeEnd = (end.Line - 1, _text.GetLineMaxColumn(end.Line - 1));
                u.Added = _text.GetText(start, rangeEnd);
            }

            u.AddedStart = start;
            u.AddedEnd = rangeEnd;
            u.After = _selection.State;

            _selection.Start = start;
            _selection.End = end;
            _undo.AddUndo(u);

            _text.PendingScrollRequest = end.Line;
        }
    }
    /// <summary>Deletes the character before the cursor position or the selected text if any exists.</summary>
    public void Backspace()
    {
        Util.Assert(!_options.IsReadOnly);

        if (_text.LineCount == 0)
            return;

        if (_selection.HasSelection)
        {
            var undo = DeleteSelection();
            _undo.AddUndo(undo);
            return;
        }

        UndoRecord u = new() { Before = _selection.State };
        var pos = _selection.GetActualCursorCoordinates();
        _selection.Cursor = pos;

        if (_selection.Cursor.Column == 0)
        {
            if (_selection.Cursor.Line == 0)
                return;

            u.Removed = "\n";
            u.RemovedStart = u.RemovedEnd = (pos.Line - 1, _text.GetLineMaxColumn(pos.Line - 1));
            _text.Advance(u.RemovedEnd);

            int lineNum = _selection.Cursor.Line;
            var lineText = _text.GetLineText(lineNum);
            var prevSize = _text.GetLineMaxColumn(lineNum - 1);
            _text.InsertTextAt((lineNum, prevSize), lineText);
            _text.RemoveLine(_selection.Cursor.Line);
            _selection.Cursor = (_selection.Cursor.Line - 1, prevSize);
        }
        else
        {
            var cindex = _text.GetCharacterIndex(pos) - 1;
            u.RemovedStart = u.RemovedEnd = _selection.GetActualCursorCoordinates();
            --u.RemovedStart.Column;

            _selection.Cursor = (_selection.Cursor.Line, _selection.Cursor.Column - 1);
            u.Removed = _text.RemoveInLine(_selection.Cursor.Line, cindex, cindex + 1);
        }

        _text.PendingScrollRequest = _selection.Cursor.Line;
        _color.InvalidateColor(_selection.Cursor.Line, 1);

        u.After = _selection.State;
        _undo.AddUndo(u);
    }

    void InsertTextAtCursor(string value)
    {
        if (string.IsNullOrEmpty(value))
            return;

        var pos = _selection.GetActualCursorCoordinates();
        var start = pos < _selection.Start ? pos : _selection.Start;
        int totalLines = pos.Line - start.Line;

        totalLines += _text.InsertTextAt(pos, value);

        _selection.Select(pos, pos);
        _selection.Cursor = pos;
        _color.InvalidateColor(start.Line - 1, totalLines + 2);
    }

    UndoRecord DeleteSelection()
    {
        Util.Assert(_selection.End >= _selection.Start);

        UndoRecord undo = new()
        {
            Before = _selection.State,
            Removed = _selection.GetSelectedText(),
            RemovedStart = _selection.Start,
            RemovedEnd = _selection.End
        };

        if (_selection.End != _selection.Start)
        {
            _text.DeleteRange(_selection.Start, _selection.End);

            _selection.Select(_selection.Start, _selection.Start);
            _selection.Cursor = _selection.Start;
            _color.InvalidateColor(_selection.Start.Line, 1);
        }

        undo.After = _selection.State;
        return undo;
    }
}
