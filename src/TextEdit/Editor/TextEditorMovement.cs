using System;

namespace ImGuiColorTextEditNet.Editor;

public class TextEditorMovement
{
    readonly TextEditorSelection _selection;
    readonly TextEditorText _text;

    internal TextEditorMovement(TextEditorSelection selection, TextEditorText text)
    {
        _selection = selection ?? throw new ArgumentNullException(nameof(selection));
        _text = text ?? throw new ArgumentNullException(nameof(text));
    }

    public void MoveUp(int amount = 1, bool isSelecting = false)
    {
        var oldPos = _selection.Cursor;
        var newPos = _selection.Cursor;
        newPos.Line = Math.Max(0, _selection.Cursor.Line - amount);
        if (oldPos != newPos)
        {
            _selection.Cursor = newPos;
            if (isSelecting)
            {
                if (oldPos == _selection.InteractiveStart)
                    _selection.InteractiveStart = _selection.Cursor;
                else if (oldPos == _selection.InteractiveEnd)
                    _selection.InteractiveEnd = _selection.Cursor;
                else
                {
                    _selection.InteractiveStart = _selection.Cursor;
                    _selection.InteractiveEnd = oldPos;
                }
            }
            else
                _selection.InteractiveStart = _selection.InteractiveEnd = _selection.Cursor;

            _selection.Select(_selection.InteractiveStart, _selection.InteractiveEnd);
            _text.ScrollToCursor = true;
        }
    }

    public void MoveDown(int amount = 1, bool isSelecting = false)
    {
        Util.Assert(_selection.Cursor.Column >= 0);
        var oldPos = _selection.Cursor;
        var newPos = _selection.Cursor;
        newPos.Line = Math.Max(0, Math.Min(_text.LineCount - 1, _selection.Cursor.Line + amount));

        if (newPos != oldPos)
        {
            _selection.Cursor = newPos;

            if (isSelecting)
            {
                if (oldPos == _selection.InteractiveEnd)
                    _selection.InteractiveEnd = _selection.Cursor;
                else if (oldPos == _selection.InteractiveStart)
                    _selection.InteractiveStart = _selection.Cursor;
                else
                {
                    _selection.InteractiveStart = oldPos;
                    _selection.InteractiveEnd = _selection.Cursor;
                }
            }
            else
                _selection.InteractiveStart = _selection.InteractiveEnd = _selection.Cursor;

            _selection.Select(_selection.InteractiveStart, _selection.InteractiveEnd);
            _text.ScrollToCursor = true;
        }
    }

    public void MoveLeft(int amount = 1, bool isSelecting = false, bool isWordMode = false)
    {
        if (_text.LineCount == 0)
            return;

        var oldPos = _selection.Cursor;
        _selection.Cursor = _selection.GetActualCursorCoordinates();
        var line = _selection.Cursor.Line;
        var cindex = _text.GetCharacterIndex(_selection.Cursor);

        while (amount-- > 0)
        {
            if (cindex == 0)
            {
                if (line > 0)
                {
                    --line;
                    cindex = _text.LineCount > line ? _text.GetLine(line).Length : 0;
                }
            }
            else
                --cindex;

            _selection.Cursor = (line, _text.GetCharacterColumn(line, cindex));
            if (isWordMode)
            {
                _selection.Cursor = _text.FindWordStart(_selection.Cursor);
                cindex = _text.GetCharacterIndex(_selection.Cursor);
            }
        }

        _selection.Cursor = (line, _text.GetCharacterColumn(line, cindex));

        Util.Assert(_selection.Cursor.Column >= 0);
        if (isSelecting)
        {
            if (oldPos == _selection.InteractiveStart)
                _selection.InteractiveStart = _selection.Cursor;
            else if (oldPos == _selection.InteractiveEnd)
                _selection.InteractiveEnd = _selection.Cursor;
            else
            {
                _selection.InteractiveStart = _selection.Cursor;
                _selection.InteractiveEnd = oldPos;
            }
        }
        else
            _selection.InteractiveStart = _selection.InteractiveEnd = _selection.Cursor;
        _selection.Select(_selection.InteractiveStart, _selection.InteractiveEnd, isSelecting && isWordMode ? SelectionMode.Word : SelectionMode.Normal);

        _text.ScrollToCursor = true;
    }

    public void MoveRight(int amount = 1, bool isSelecting = false, bool isWordMode = false)
    {
        var oldPos = _selection.Cursor;

        if (_text.LineCount == 0 || oldPos.Line >= _text.LineCount)
            return;

        var cindex = _text.GetCharacterIndex(_selection.Cursor);
        while (amount-- > 0)
        {
            var lindex = _selection.Cursor.Line;
            var line = _text.GetLine(lindex);
            if (cindex >= line.Length)
            {
                if (_selection.Cursor.Line < _text.LineCount - 1)
                    _selection.Cursor = (Math.Max(0, Math.Min(_text.LineCount - 1, _selection.Cursor.Line + 1)), 0);
                else
                    return;
            }
            else
            {
                cindex++;
                _selection.Cursor = (lindex, _text.GetCharacterColumn(lindex, cindex));
                if (isWordMode)
                    _selection.Cursor = _text.FindNextWord(_selection.Cursor);
            }
        }

        if (isSelecting)
        {
            if (oldPos == _selection.InteractiveEnd)
                _selection.InteractiveEnd = _text.SanitizeCoordinates(_selection.Cursor);
            else if (oldPos == _selection.InteractiveStart)
                _selection.InteractiveStart = _selection.Cursor;
            else
            {
                _selection.InteractiveStart = oldPos;
                _selection.InteractiveEnd = _selection.Cursor;
            }
        }
        else
            _selection.InteractiveStart = _selection.InteractiveEnd = _selection.Cursor;
        _selection.Select(_selection.InteractiveStart, _selection.InteractiveEnd, isSelecting && isWordMode ? SelectionMode.Word : SelectionMode.Normal);

        _text.ScrollToCursor = true;
    }

    public void MoveToStartOfFile(bool isSelecting = false)
    {
        var oldPos = _selection.Cursor;
        _selection.Cursor = (0, 0);

        if (_selection.Cursor != oldPos)
        {
            if (isSelecting)
            {
                _selection.InteractiveEnd = oldPos;
                _selection.InteractiveStart = _selection.Cursor;
            }
            else
                _selection.InteractiveStart = _selection.InteractiveEnd = _selection.Cursor;
            _selection.Select(_selection.InteractiveStart, _selection.InteractiveEnd);
        }
    }

    public void MoveToEndOfFile(bool isSelecting = false)
    {
        var oldPos = _selection.Cursor;
        var newPos = (_text.LineCount - 1, 0);
        _selection.Cursor = newPos;

        if (isSelecting)
        {
            _selection.InteractiveStart = oldPos;
            _selection.InteractiveEnd = newPos;
        }
        else
            _selection.InteractiveStart = _selection.InteractiveEnd = newPos;

        _selection.Select(_selection.InteractiveStart, _selection.InteractiveEnd);
    }

    public void MoveToStartOfLine(bool isSelecting = false)
    {
        var oldPos = _selection.Cursor;
        _selection.Cursor = (_selection.Cursor.Line, 0);

        if (_selection.Cursor != oldPos)
        {
            if (isSelecting)
            {
                if (oldPos == _selection.InteractiveStart)
                    _selection.InteractiveStart = _selection.Cursor;
                else if (oldPos == _selection.InteractiveEnd)
                    _selection.InteractiveEnd = _selection.Cursor;
                else
                {
                    _selection.InteractiveStart = _selection.Cursor;
                    _selection.InteractiveEnd = oldPos;
                }
            }
            else
                _selection.InteractiveStart = _selection.InteractiveEnd = _selection.Cursor;
            _selection.Select(_selection.InteractiveStart, _selection.InteractiveEnd);
        }
    }

    public void MoveToEndOfLine(bool isSelecting = false)
    {
        var oldPos = _selection.Cursor;
        _selection.Cursor = (_selection.Cursor.Line, _text.GetLineMaxColumn(oldPos.Line));

        if (_selection.Cursor == oldPos)
            return;

        if (isSelecting)
        {
            if (oldPos == _selection.InteractiveEnd)
                _selection.InteractiveEnd = _selection.Cursor;
            else if (oldPos == _selection.InteractiveStart)
                _selection.InteractiveStart = _selection.Cursor;
            else
            {
                _selection.InteractiveStart = oldPos;
                _selection.InteractiveEnd = _selection.Cursor;
            }
        }
        else
            _selection.InteractiveStart = _selection.InteractiveEnd = _selection.Cursor;

        _selection.Select(_selection.InteractiveStart, _selection.InteractiveEnd);
    }
}