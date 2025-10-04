using System;
using System.Text;
using ImGuiColorTextEditNet.Operations;
using ImGuiNET;

namespace ImGuiColorTextEditNet.Editor;

/// <summary>Provides methods for modifying text, e.g. copy, cut, paste, delete, and character entry.</summary>
public static class TextEditorModify
{
    static readonly SimpleCache<char, string> CharLabelCache = new(
        "char strings",
        x => x.ToString()
    );

    /// <summary>Copies the currently selected text to the clipboard.</summary>
    public static void Copy(TextEditor e)
    {
        if (e.Selection.HasSelection)
        {
            ImGui.SetClipboardText(e.Selection.GetSelectedText());
            return;
        }

        if (e.Text.LineCount == 0)
            return;

        StringBuilder sb = new();

        var line = e.Text.GetLine(e.Selection.GetActualCursorCoordinates().Line);
        foreach (var g in line)
            sb.Append(g.Char);

        ImGui.SetClipboardText(sb.ToString());
    }

    /// <summary>Cuts the currently selected text, copying it to the clipboard and removing it from the editor.</summary>
    public static void Cut(TextEditor e)
    {
        if (e.Options.IsReadOnly)
        {
            Copy(e);
            return;
        }

        if (!e.Selection.HasSelection)
            return;

        Copy(e);
        UndoRecord undo = DeleteSelection(e);
        e.UndoStack.AddUndo(undo);
    }

    /// <summary>Pastes text from the clipboard into the editor at the current cursor position or replaces the selection if any exists.</summary>
    public static void Paste(TextEditor e)
    {
        unsafe
        {
            if (ImGuiNative.igGetClipboardText() == null)
                return;
        }

        var clipText = ImGui.GetClipboardText();
        if (string.IsNullOrEmpty(clipText))
            return;

        ReplaceSelection(e, clipText);
    }

    /// <summary>Replaces the currently selected text with the specified text, or inserts the text at the cursor position if no selection exists.</summary>
    public static void ReplaceSelection(TextEditor e, string text)
    {
        Util.Assert(!e.Options.IsReadOnly);

        UndoRecord u = e.Selection.HasSelection
            ? DeleteSelection(e)
            : new() { Before = e.Selection.State };

        var pos = e.Selection.GetActualCursorCoordinates();
        u.Added = text;
        u.AddedStart = pos;
        u.AddedEnd = pos;

        if (!string.IsNullOrEmpty(text))
        {
            var start = pos < e.Selection.Start ? pos : e.Selection.Start;
            int totalLines = pos.Line - start.Line;

            u.AddedEnd = e.Text.InsertTextAt(pos, text);

            e.Selection.Select(pos, pos);
            e.Selection.Cursor = pos;
            e.Color.InvalidateColor(start.Line - 1, totalLines + 2);
        }

        u.After = e.Selection.State;
        e.UndoStack.AddUndo(u);
    }

    /// <summary>Deletes the currently selected text or the character at the cursor position if no selection exists.</summary>
    public static void Delete(TextEditor e)
    {
        if (e.Options.IsReadOnly)
            return;

        if (e.Text.LineCount == 0)
            return;

        if (e.Selection.HasSelection)
        {
            UndoRecord u = DeleteSelection(e);
            e.UndoStack.AddUndo(u);
        }
        else
        {
            var pos = e.Selection.GetActualCursorCoordinates();
            e.Selection.Cursor = pos;

            UndoRecord u = new() { Before = e.Selection.State };

            if (pos.Column == e.Text.GetLineMaxColumn(pos.Line))
            {
                if (pos.Line == e.Text.LineCount - 1)
                    return;

                u.Removed = "\n";
                u.RemovedStart = u.RemovedEnd = e.Selection.GetActualCursorCoordinates();
                e.Text.Advance(u.RemovedEnd);
                e.Text.AppendToLine(pos.Line, e.Text.GetLineText(pos.Line + 1));
                e.Text.RemoveLine(pos.Line + 1);
            }
            else
            {
                var cindex = e.Text.GetCharacterIndex(pos);
                u.RemovedStart = u.RemovedEnd = e.Selection.GetActualCursorCoordinates();
                u.RemovedEnd.Column++;
                u.Removed = e.Text.GetText(u.RemovedStart, u.RemovedEnd);

                e.Text.RemoveInLine(pos.Line, cindex, cindex + 1);
            }

            u.After = e.Selection.State;
            e.Color.InvalidateColor(pos.Line, 1);
            e.UndoStack.AddUndo(u);
        }
    }

    /// <summary>Inserts a character at the current cursor position or replaces the selection if any exists.</summary>
    public static void EnterCharacter(TextEditor e, char c)
    {
        Util.Assert(!e.Options.IsReadOnly);
        UndoRecord u = e.Selection.HasSelection
            ? DeleteSelection(e)
            : new() { Before = e.Selection.State };

        var coord = e.Selection.GetActualCursorCoordinates();
        u.AddedStart = coord;

        Util.Assert(e.Text.LineCount != 0);

        if (c == '\n')
        {
            var line = e.Text.GetLine(coord.Line);
            var newLine = new Line();

            if (e.Color.SyntaxHighlighter.AutoIndentation)
            {
                for (
                    int it = 0;
                    it < line.Length
                        && char.IsAscii(line[it].Char)
                        && TextEditorText.IsBlank(line[it].Char);
                    ++it
                )
                {
                    newLine.Glyphs.Add(line[it]);
                }
            }

            int whitespaceSize = newLine.Glyphs.Count;
            var cindex = e.Text.GetCharacterIndex(coord);
            foreach (var glyph in line[cindex..])
                newLine.Glyphs.Add(glyph);

            e.Text.InsertLine(coord.Line + 1, newLine);
            e.Text.RemoveInLine(coord.Line, cindex, line.Length);
            e.Selection.Cursor = (
                coord.Line + 1,
                e.Text.GetCharacterColumn(coord.Line + 1, whitespaceSize)
            );

            u.Added = "\n";
        }
        else
        {
            var line = e.Text.GetLine(coord.Line);
            var cindex = e.Text.GetCharacterIndex(coord);

            if (e.Options.IsOverwrite && cindex < line.Length)
            {
                u.RemovedStart = e.Selection.Cursor;
                u.RemovedEnd = (coord.Line, e.Text.GetCharacterColumn(coord.Line, cindex + 1));

                u.Removed += line[cindex].Char;
                e.Text.RemoveInLine(coord.Line, cindex, cindex + 1);
            }

            e.Text.InsertCharAt(coord, c);
            u.Added = CharLabelCache.Get(c);

            e.Selection.Cursor = (coord.Line, e.Text.GetCharacterColumn(coord.Line, cindex + 1));
        }

        u.AddedEnd = e.Selection.GetActualCursorCoordinates();
        u.After = e.Selection.State;

        e.UndoStack.AddUndo(u);

        e.Color.InvalidateColor(coord.Line - 1, 3);
        e.Text.PendingScrollRequest = coord.Line;
    }

    static (Coordinates, Coordinates) GetIndentRange(TextEditor e)
    {
        var start = e.Selection.Start;
        var end = e.Selection.End;

        if (start > end)
            (start, end) = (end, start);

        start.Column = 0;
        // end._column = end._line < e.Text.LineCount ? _state._lines[end._line].Count : 0;
        if (end is { Column: 0, Line: > 0 })
            --end.Line;

        if (end.Line >= e.Text.LineCount)
            end.Line = e.Text.LineCount == 0 ? 0 : e.Text.LineCount - 1;

        end.Column = e.Text.GetLineMaxColumn(end.Line);

        //if (end._column >= GetLineMaxColumn(end._line))
        //    end._column = GetLineMaxColumn(end._line) - 1;

        return (start, end);
    }

    /// <summary>Indents or unindents the selected lines based on the current tab size.</summary>
    public static void IndentSelection(TextEditor e, bool shift)
    {
        Util.Assert(!e.Options.IsReadOnly);

        var u = new MetaOperation { Before = e.Selection.State };
        var originalEnd = e.Selection.End;
        var (start, end) = GetIndentRange(e);

        for (int i = start.Line; i <= end.Line; i++)
        {
            if (shift)
            {
                UnindentLine(e, i, u);
            }
            else
            {
                var indentString = e.Options.IndentWithSpaces
                    ? new string(' ', e.Options.TabSize)
                    : "\t";

                u.Add(
                    new ModifyLineOperation
                    {
                        Line = i,
                        AddedColumn = 0,
                        Added = indentString,
                    }
                );
            }
        }

        if (u.Count == 0)
            return;

        e.Selection.Start = (start.Line, e.Text.GetCharacterColumn(start.Line, 0));
        e.Selection.End =
            originalEnd.Column != 0
                ? (end.Line, e.Text.GetLineMaxColumn(end.Line))
                : (originalEnd.Line, 0);

        e.Text.PendingScrollRequest = e.Selection.End.Line;

        u.After = e.Selection.State;
        e.UndoStack.Do(u, e);
    }

    static void UnindentLine(TextEditor e, int i, MetaOperation u)
    {
        ReadOnlySpan<Glyph> line = e.Text.GetLine(i);
        if (line.Length == 0)
            return;

        string removed;
        if (line[0].Char == '\t')
        {
            removed = e.Text.GetText((i, 0), (i, 1));
        }
        else
        {
            int j = 0;
            for (; j < e.Options.TabSize && line.Length != 0 && line[0].Char == ' '; j++) { }

            if (j == 0)
                return;

            removed = new string(' ', j);
        }

        u.Add(
            new ModifyLineOperation
            {
                Line = i,
                RemovedColumn = 0,
                Removed = removed,
            }
        );
    }

    /// <summary>Deletes the character before the cursor position or the selected text if any exists.</summary>
    public static void Backspace(TextEditor e)
    {
        Util.Assert(!e.Options.IsReadOnly);

        if (e.Text.LineCount == 0)
            return;

        if (e.Selection.HasSelection)
        {
            UndoRecord undo = DeleteSelection(e);
            e.UndoStack.AddUndo(undo);
            return;
        }

        MetaOperation u = new() { Before = e.Selection.State };
        var pos = e.Selection.GetActualCursorCoordinates();
        e.Selection.Cursor = pos;

        if (e.Selection.Cursor.Column == 0)
        {
            if (e.Selection.Cursor.Line == 0)
                return;

            int lineNum = e.Selection.Cursor.Line;
            var lineText = e.Text.GetLineText(lineNum);
            var prevSize = e.Text.GetLineMaxColumn(lineNum - 1);

            u.Add(
                new ModifyLineOperation
                {
                    Line = lineNum - 1,
                    Added = lineText,
                    AddedColumn = prevSize,
                }
            );

            u.Add(new RemoveLineOperation { Line = lineNum, Removed = lineText });
            u.After.Cursor = (e.Selection.Cursor.Line - 1, prevSize);
        }
        else
        {
            var cindex = e.Text.GetCharacterIndex(pos) - 1;
            var removed = e.Text.GetText(pos - (0, 1), pos);

            u.Add(
                new ModifyLineOperation
                {
                    Line = e.Selection.Cursor.Line,
                    RemovedColumn = cindex,
                    Removed = removed,
                }
            );

            u.After.Cursor = (e.Selection.Cursor.Line, e.Selection.Cursor.Column - 1);
        }

        u.After.Start = u.After.End = u.After.Cursor;
        u.Apply(e);

        e.Text.PendingScrollRequest = e.Selection.Cursor.Line;
        e.UndoStack.AddUndo(u);
    }

    static UndoRecord DeleteSelection(TextEditor e)
    {
        Util.Assert(e.Selection.End >= e.Selection.Start);

        UndoRecord undo = new()
        {
            Before = e.Selection.State,
            Removed = e.Selection.GetSelectedText(),
            RemovedStart = e.Selection.Start,
            RemovedEnd = e.Selection.End,
        };

        if (e.Selection.End != e.Selection.Start)
        {
            e.Text.DeleteRange(e.Selection.Start, e.Selection.End);

            e.Selection.Select(e.Selection.Start, e.Selection.Start);
            e.Selection.Cursor = e.Selection.Start;
            e.Color.InvalidateColor(e.Selection.Start.Line, 1);
        }

        undo.After = e.Selection.State;
        return undo;
    }
}
