using System.Diagnostics;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using ImGuiNET;

// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedMember.Global
// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace ImGuiColorTextEditNet;

public class TextEditor
{
    const float _lineSpacing = 1.0f;
    const int _leftMargin = 10;

    static (List<Glyph> Glyphs, object? SyntaxState) BlankLine => (new List<Glyph>(), null);

    readonly List<UndoRecord> _undoBuffer = new();
    readonly List<(List<Glyph> Glyphs, object? SyntaxState)> _lines = new();
    readonly uint[] _palette = new uint[(int)PaletteIndex.Max];
    readonly SimpleCache<int, string> _lineNumberCache = new("line numbers", x => $"{x} ");
    readonly SimpleCache<char, string> _charLabelCache = new("chars", x => x.ToString());
    readonly SimpleCache<char, float> _charWidthCache = new("char width", x => ImGui.CalcTextSize(x.ToString()).X);
    readonly ISyntaxHighlighter _syntaxHighlighter;

    HashSet<int> _breakpoints = new();
    Dictionary<int, string> _errorMarkers = new();
    EditorState _state;
    int _undoIndex;
    int _tabSize = 4;
    bool _overwrite;
    bool _withinRender;
    bool _scrollToCursor;
    bool _scrollToTop;
    float _textStart = 20.0f; // position (in pixels) where a code line starts relative to the left of the TextEditor.
    int _foo;
    int _colorRangeMin
    {
        get => _foo;
        set
        {
            if (value < 0)
                value = 0;
            _foo = value;
        }
    }

    int _colorRangeMax;
    SelectionMode _selectionMode = SelectionMode.Normal;
    Vector2 _charAdvance;
    Coordinates _interactiveStart, _interactiveEnd;
    string _lineBuffer = "";
    DateTime _startTime = DateTime.UtcNow;
    float _lastClick = -1.0f;

    public TextEditor() : this(null) { }
    public TextEditor(ISyntaxHighlighter? syntaxHighlighter)
    {
        _syntaxHighlighter = syntaxHighlighter ?? NullSyntaxHighlighter.Instance;
        _lines.Add(BlankLine);
    }

    public uint[] Palette { get; set; } = Palettes.Dark;
    public void SetErrorMarkers(Dictionary<int, string> value) => _errorMarkers = value;
    public void SetBreakpoints(HashSet<int> value) => _breakpoints = value;

    public string Text
    {
        get => GetText(new Coordinates(0, 0), new Coordinates(_lines.Count, 0));
        set
        {
            _lines.Clear();
            _lines.Add(BlankLine);

            foreach (var chr in value)
            {
                if (chr == '\r')
                {
                    // ignore the carriage return character
                }
                else if (chr == '\n')
                    _lines.Add(BlankLine);
                else
                {
                    _lines[^1].Glyphs.Add(new Glyph(chr, PaletteIndex.Default));
                }
            }

            IsTextChanged = true;
            _scrollToTop = true;

            _undoBuffer.Clear();
            _undoIndex = 0;

            InvalidateColor();
        }
    }

    public List<string> TextLines
    {
        get
        {
            var result = new List<string>(_lines.Count);

            foreach (var (line, _) in _lines)
            {
                var sb = new StringBuilder(line.Count);

                for (int i = 0; i < line.Count; ++i)
                    sb.Append(line[i].Char);

                result.Add(sb.ToString());
            }

            return result;
        }
        set
        {
            _lines.Clear();

            if (value.Count == 0)
            {
                _lines.Add(BlankLine);
            }
            else
            {
                _lines.Capacity = value.Count;
                foreach (var aLine in value)
                {
                    var line = BlankLine;
                    line.Glyphs.Capacity = aLine.Length;
                    _lines.Add(line);

                    foreach (var c in aLine)
                        line.Glyphs.Add(new Glyph(c, PaletteIndex.Default));
                }
            }

            IsTextChanged = true;
            _scrollToTop = true;

            _undoBuffer.Clear();
            _undoIndex = 0;

            InvalidateColor();
        }
    }

    public string GetSelectedText() => GetText(_state.SelectionStart, _state.SelectionEnd);
    public string GetCurrentLineText()
    {
        var lineLength = GetLineMaxColumn(_state.CursorPosition.Line);
        return GetText(
                new Coordinates(_state.CursorPosition.Line, 0),
                new Coordinates(_state.CursorPosition.Line, lineLength));
    }

    public int TotalLines => _lines.Count;
    public bool IsOverwrite() => _overwrite;
    public bool IsReadOnly { get; set; }
    public bool IsTextChanged { get; private set; }
    public bool IsCursorPositionChanged { get; private set; }
    public bool IsColorizerEnabled { get; set; } = true;
    public Coordinates CursorPosition
    {
        get => GetActualCursorCoordinates();
        set
        {
            if (_state.CursorPosition != value)
            {
                _state.CursorPosition = value;
                IsCursorPositionChanged = true;
                EnsureCursorVisible();
            }
        }
    }

    public bool IsHandleMouseInputsEnabled { get; set; } = true;
    public bool IsHandleKeyboardInputsEnabled { get; set; } = true;
    public bool IsImGuiChildIgnored { get; set; }
    public bool IsShowingWhitespaces { get; set; } = true;
    public int TabSize
    {
        get => _tabSize;
        set => _tabSize = Math.Max(0, Math.Min(32, value));
    }

    public void Render(string title, Vector2 size = new(), bool showBorder = false)
    {
        _withinRender = true;
        IsTextChanged = false;
        IsCursorPositionChanged = false;

        ImGui.PushStyleColor(ImGuiCol.ChildBg, ImGui.ColorConvertU32ToFloat4(_palette[(int)PaletteIndex.Background]));
        ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(0.0f, 0.0f));

        if (!IsImGuiChildIgnored)
        {
            ImGui.BeginChild(title, size, showBorder,
                ImGuiWindowFlags.HorizontalScrollbar 
                | ImGuiWindowFlags.AlwaysHorizontalScrollbar 
                | ImGuiWindowFlags.NoMove);
        }

        if (IsHandleMouseInputsEnabled)
        {
            HandleKeyboardInputs();
            ImGui.PushAllowKeyboardFocus(true);
        }

        if (IsHandleMouseInputsEnabled)
            HandleMouseInputs();

        ColorizeIncremental();
        Render();

        if (IsHandleMouseInputsEnabled)
            ImGui.PopAllowKeyboardFocus();

        if (!IsImGuiChildIgnored)
            ImGui.EndChild();

        ImGui.PopStyleVar();
        ImGui.PopStyleColor();

        _withinRender = false;
    }

    public void InsertText(string aValue)
    {
        if (string.IsNullOrEmpty(aValue))
            return;

        var pos = GetActualCursorCoordinates();
        var start = pos < _state.SelectionStart ? pos : _state.SelectionStart;
        int totalLines = pos.Line - start.Line;

        totalLines += InsertTextAt(pos, aValue);

        SetSelection(pos, pos);
        CursorPosition = pos;
        InvalidateColor(start.Line - 1, totalLines + 2);
    }

    public void SetSelectionStart(Coordinates aPosition)
    {
        _state.SelectionStart = SanitizeCoordinates(aPosition);
        if (_state.SelectionStart > _state.SelectionEnd)
            (_state.SelectionStart, _state.SelectionEnd) = (_state.SelectionEnd, _state.SelectionStart);
    }

    public void SetSelectionEnd(Coordinates aPosition)
    {
        _state.SelectionEnd = SanitizeCoordinates(aPosition);
        if (_state.SelectionStart > _state.SelectionEnd)
            (_state.SelectionStart, _state.SelectionEnd) = (_state.SelectionEnd, _state.SelectionStart);
    }

    public void SetSelection(Coordinates aStart, Coordinates aEnd, SelectionMode aMode = SelectionMode.Normal)
    {
        var oldSelStart = _state.SelectionStart;
        var oldSelEnd = _state.SelectionEnd;

        _state.SelectionStart = SanitizeCoordinates(aStart);
        _state.SelectionEnd = SanitizeCoordinates(aEnd);
        if (_state.SelectionStart > _state.SelectionEnd)
            (_state.SelectionStart, _state.SelectionEnd) = (_state.SelectionEnd, _state.SelectionStart);

        switch (aMode)
        {
            case SelectionMode.Normal:
                break;
            case SelectionMode.Word:
                {
                    _state.SelectionStart = FindWordStart(_state.SelectionStart);
                    if (!IsOnWordBoundary(_state.SelectionEnd))
                        _state.SelectionEnd = FindWordEnd(FindWordStart(_state.SelectionEnd));
                    break;
                }
            case SelectionMode.Line:
                {
                    var lineNo = _state.SelectionEnd.Line;
                    _state.SelectionStart = new Coordinates(_state.SelectionStart.Line, 0);
                    _state.SelectionEnd = new Coordinates(lineNo, GetLineMaxColumn(lineNo));
                    break;
                }
        }

        if (_state.SelectionStart != oldSelStart ||
            _state.SelectionEnd != oldSelEnd)
            IsCursorPositionChanged = true;
    }

    public void SelectWordUnderCursor()
    {
        var c = CursorPosition;
        SetSelection(FindWordStart(c), FindWordEnd(c));
    }

    public void SelectAll() => SetSelection(new Coordinates(0, 0), new Coordinates(_lines.Count, 0));
    public bool HasSelection() => _state.SelectionEnd > _state.SelectionStart;

    public void MoveUp(int aAmount = 1, bool aSelect = false)
    {
        var oldPos = _state.CursorPosition;
        _state.CursorPosition.Line = Math.Max(0, _state.CursorPosition.Line - aAmount);
        if (oldPos != _state.CursorPosition)
        {
            if (aSelect)
            {
                if (oldPos == _interactiveStart)
                    _interactiveStart = _state.CursorPosition;
                else if (oldPos == _interactiveEnd)
                    _interactiveEnd = _state.CursorPosition;
                else
                {
                    _interactiveStart = _state.CursorPosition;
                    _interactiveEnd = oldPos;
                }
            }
            else
                _interactiveStart = _interactiveEnd = _state.CursorPosition;
            SetSelection(_interactiveStart, _interactiveEnd);

            EnsureCursorVisible();
        }
    }

    public void MoveDown(int aAmount = 1, bool aSelect = false)
    {
        Util.Assert(_state.CursorPosition.Column >= 0);
        var oldPos = _state.CursorPosition;
        _state.CursorPosition.Line = Math.Max(0, Math.Min(_lines.Count - 1, _state.CursorPosition.Line + aAmount));

        if (_state.CursorPosition != oldPos)
        {
            if (aSelect)
            {
                if (oldPos == _interactiveEnd)
                    _interactiveEnd = _state.CursorPosition;
                else if (oldPos == _interactiveStart)
                    _interactiveStart = _state.CursorPosition;
                else
                {
                    _interactiveStart = oldPos;
                    _interactiveEnd = _state.CursorPosition;
                }
            }
            else
                _interactiveStart = _interactiveEnd = _state.CursorPosition;
            SetSelection(_interactiveStart, _interactiveEnd);

            EnsureCursorVisible();
        }
    }

    static bool IsUTFSequence(char c) => (c & 0xC0) == 0x80;

    public void MoveLeft(int aAmount = 1, bool aSelect = false, bool aWordMode = false)
    {
        if (_lines.Count == 0)
            return;

        var oldPos = _state.CursorPosition;
        _state.CursorPosition = GetActualCursorCoordinates();
        var line = _state.CursorPosition.Line;
        var cindex = GetCharacterIndex(_state.CursorPosition);

        while (aAmount-- > 0)
        {
            if (cindex == 0)
            {
                if (line > 0)
                {
                    --line;
                    cindex = _lines.Count > line ? _lines[line].Glyphs.Count : 0;
                }
            }
            else
            {
                --cindex;
                if (cindex > 0)
                {
                    if (_lines.Count > line)
                    {
                        while (cindex > 0 && IsUTFSequence(_lines[line].Glyphs[cindex].Char))
                            --cindex;
                    }
                }
            }

            _state.CursorPosition = new Coordinates(line, GetCharacterColumn(line, cindex));
            if (aWordMode)
            {
                _state.CursorPosition = FindWordStart(_state.CursorPosition);
                cindex = GetCharacterIndex(_state.CursorPosition);
            }
        }

        _state.CursorPosition = new Coordinates(line, GetCharacterColumn(line, cindex));

        Util.Assert(_state.CursorPosition.Column >= 0);
        if (aSelect)
        {
            if (oldPos == _interactiveStart)
                _interactiveStart = _state.CursorPosition;
            else if (oldPos == _interactiveEnd)
                _interactiveEnd = _state.CursorPosition;
            else
            {
                _interactiveStart = _state.CursorPosition;
                _interactiveEnd = oldPos;
            }
        }
        else
            _interactiveStart = _interactiveEnd = _state.CursorPosition;
        SetSelection(_interactiveStart, _interactiveEnd, aSelect && aWordMode ? SelectionMode.Word : SelectionMode.Normal);

        EnsureCursorVisible();
    }

    public void MoveRight(int aAmount = 1, bool aSelect = false, bool aWordMode = false)
    {
        var oldPos = _state.CursorPosition;

        if (_lines.Count == 0 || oldPos.Line >= _lines.Count)
            return;

        var cindex = GetCharacterIndex(_state.CursorPosition);
        while (aAmount-- > 0)
        {
            var lindex = _state.CursorPosition.Line;
            var line = _lines[lindex];

            if (cindex >= line.Glyphs.Count)
            {
                if (_state.CursorPosition.Line < _lines.Count - 1)
                {
                    _state.CursorPosition.Line = Math.Max(0, Math.Min(_lines.Count - 1, _state.CursorPosition.Line + 1));
                    _state.CursorPosition.Column = 0;
                }
                else
                    return;
            }
            else
            {
                cindex++;
                _state.CursorPosition = new Coordinates(lindex, GetCharacterColumn(lindex, cindex));
                if (aWordMode)
                    _state.CursorPosition = FindNextWord(_state.CursorPosition);
            }
        }

        if (aSelect)
        {
            if (oldPos == _interactiveEnd)
                _interactiveEnd = SanitizeCoordinates(_state.CursorPosition);
            else if (oldPos == _interactiveStart)
                _interactiveStart = _state.CursorPosition;
            else
            {
                _interactiveStart = oldPos;
                _interactiveEnd = _state.CursorPosition;
            }
        }
        else
            _interactiveStart = _interactiveEnd = _state.CursorPosition;
        SetSelection(_interactiveStart, _interactiveEnd, aSelect && aWordMode ? SelectionMode.Word : SelectionMode.Normal);

        EnsureCursorVisible();
    }

    public void MoveTop(bool aSelect = false)
    {
        var oldPos = _state.CursorPosition;
        CursorPosition = new Coordinates(0, 0);

        if (_state.CursorPosition != oldPos)
        {
            if (aSelect)
            {
                _interactiveEnd = oldPos;
                _interactiveStart = _state.CursorPosition;
            }
            else
                _interactiveStart = _interactiveEnd = _state.CursorPosition;
            SetSelection(_interactiveStart, _interactiveEnd);
        }
    }

    public void MoveBottom(bool aSelect = false)
    {
        var oldPos = CursorPosition;
        var newPos = new Coordinates(_lines.Count - 1, 0);
        CursorPosition = newPos;

        if (aSelect)
        {
            _interactiveStart = oldPos;
            _interactiveEnd = newPos;
        }
        else
            _interactiveStart = _interactiveEnd = newPos;

        SetSelection(_interactiveStart, _interactiveEnd);
    }

    public void MoveHome(bool aSelect = false)
    {
        var oldPos = _state.CursorPosition;
        CursorPosition = new Coordinates(_state.CursorPosition.Line, 0);

        if (_state.CursorPosition != oldPos)
        {
            if (aSelect)
            {
                if (oldPos == _interactiveStart)
                    _interactiveStart = _state.CursorPosition;
                else if (oldPos == _interactiveEnd)
                    _interactiveEnd = _state.CursorPosition;
                else
                {
                    _interactiveStart = _state.CursorPosition;
                    _interactiveEnd = oldPos;
                }
            }
            else
                _interactiveStart = _interactiveEnd = _state.CursorPosition;
            SetSelection(_interactiveStart, _interactiveEnd);
        }
    }

    public void MoveEnd(bool aSelect = false)
    {
        var oldPos = _state.CursorPosition;
        CursorPosition = new Coordinates(_state.CursorPosition.Line, GetLineMaxColumn(oldPos.Line));

        if (_state.CursorPosition != oldPos)
        {
            if (aSelect)
            {
                if (oldPos == _interactiveEnd)
                    _interactiveEnd = _state.CursorPosition;
                else if (oldPos == _interactiveStart)
                    _interactiveStart = _state.CursorPosition;
                else
                {
                    _interactiveStart = oldPos;
                    _interactiveEnd = _state.CursorPosition;
                }
            }
            else
                _interactiveStart = _interactiveEnd = _state.CursorPosition;
            SetSelection(_interactiveStart, _interactiveEnd);
        }
    }

    public void Copy()
    {
        if (HasSelection())
        {
            ImGui.SetClipboardText(GetSelectedText());
        }
        else
        {
            if (_lines.Count != 0)
            {
                StringBuilder sb = new();
                var line = _lines[GetActualCursorCoordinates().Line];

                foreach (var g in line.Glyphs)
                    sb.Append(g.Char);

                ImGui.SetClipboardText(sb.ToString());
            }
        }
    }

    public void Cut()
    {
        if (IsReadOnly)
        {
            Copy();
            return;
        }

        if (!HasSelection())
            return;

        UndoRecord u = new()
        {
            Before = _state,
            Removed = GetSelectedText(),
            RemovedStart = _state.SelectionStart,
            RemovedEnd = _state.SelectionEnd
        };

        Copy();
        DeleteSelection();

        u.After = _state;
        AddUndo(u);
    }

    public void Paste()
    {
        if (IsReadOnly)
            return;

        var clipText = ImGui.GetClipboardText();
        if (string.IsNullOrEmpty(clipText))
            return;

        UndoRecord u = new()
        {
            Before = _state,
            Added = clipText,
            AddedStart = GetActualCursorCoordinates()
        };

        if (HasSelection())
        {
            u.Removed = GetSelectedText();
            u.RemovedStart = _state.SelectionStart;
            u.RemovedEnd = _state.SelectionEnd;
            DeleteSelection();
        }

        InsertText(clipText);

        u.AddedEnd = GetActualCursorCoordinates();
        u.After = _state;
        AddUndo(u);
    }

    public void Delete()
    {
        Util.Assert(!IsReadOnly);

        if (_lines.Count == 0)
            return;

        UndoRecord u = new() { Before = _state };

        if (HasSelection())
        {
            u.Removed = GetSelectedText();
            u.RemovedStart = _state.SelectionStart;
            u.RemovedEnd = _state.SelectionEnd;

            DeleteSelection();
        }
        else
        {
            var pos = GetActualCursorCoordinates();
            CursorPosition = pos;
            var line = _lines[pos.Line];

            if (pos.Column == GetLineMaxColumn(pos.Line))
            {
                if (pos.Line == _lines.Count - 1)
                    return;

                u.Removed = "\n";
                u.RemovedStart = u.RemovedEnd = GetActualCursorCoordinates();
                Advance(u.RemovedEnd);

                var nextLine = _lines[pos.Line + 1].Glyphs;
                line.Glyphs.AddRange(nextLine);
                RemoveLine(pos.Line + 1);
            }
            else
            {
                var cindex = GetCharacterIndex(pos);
                u.RemovedStart = u.RemovedEnd = GetActualCursorCoordinates();
                u.RemovedEnd.Column++;
                u.Removed = GetText(u.RemovedStart, u.RemovedEnd);
                line.Glyphs.RemoveAt(cindex);
            }

            IsTextChanged = true;

            InvalidateColor(pos.Line, 1);
        }

        u.After = _state;
        AddUndo(u);
    }

    public bool CanUndo() => !IsReadOnly && _undoIndex > 0;
    public bool CanRedo() => !IsReadOnly && _undoIndex < _undoBuffer.Count;

    public void Undo(int aSteps = 1)
    {
        while (CanUndo() && aSteps-- > 0)
            Undo(_undoBuffer[--_undoIndex]);
    }

    void Undo(UndoRecord record)
    {
        if (!string.IsNullOrEmpty(record.Added))
        {
            DeleteRange(record.AddedStart, record.AddedEnd);
            InvalidateColor(record.AddedStart.Line - 1, record.AddedEnd.Line - record.AddedStart.Line + 2);
        }

        if (!string.IsNullOrEmpty(record.Removed))
        {
            var start = record.RemovedStart;
            InsertTextAt(start, record.Removed);
            InvalidateColor(record.RemovedStart.Line - 1, record.RemovedEnd.Line - record.RemovedStart.Line + 2);
        }

        _state = record.Before;
        EnsureCursorVisible();
    }

    public void Redo(int aSteps = 1)
    {
        while (CanRedo() && aSteps-- > 0)
            Redo(_undoBuffer[_undoIndex++]);
    }

    void Redo(UndoRecord record)
    {
        if (!string.IsNullOrEmpty(record.Removed))
        {
            DeleteRange(record.RemovedStart, record.RemovedEnd);
            InvalidateColor(record.RemovedStart.Line - 1, record.RemovedEnd.Line - record.RemovedStart.Line + 1);
        }

        if (!string.IsNullOrEmpty(record.Added))
        {
            var start = record.AddedStart;
            InsertTextAt(start, record.Added);
            InvalidateColor(record.AddedStart.Line - 1, record.AddedEnd.Line - record.AddedStart.Line + 1);
        }

        _state = record.After;
        EnsureCursorVisible();
    }

    void InvalidateColor(int fromLine = 0, int aLines = -1)
    {
        fromLine = Math.Min(_colorRangeMin, fromLine);
        fromLine = Math.Max(0, fromLine);

        int toLine = aLines == -1 ? _lines.Count : Math.Min(_lines.Count, fromLine + aLines);
        toLine = Math.Max(_colorRangeMax, toLine);
        toLine = Math.Max(fromLine, toLine);

        _colorRangeMin = fromLine;
        _colorRangeMax = toLine;
    }

    void ColorizeIncremental()
    {
        if (_lines.Count == 0 || !IsColorizerEnabled || _colorRangeMin >= _colorRangeMax)
            return;

        int increment = _syntaxHighlighter.MaxLinesPerFrame;
        int to = Math.Min(_colorRangeMin + increment, _colorRangeMax);

        for (int lineIndex = _colorRangeMin; lineIndex < to; lineIndex++)
        {
            var glyphs = _lines[lineIndex].Glyphs;
            var state = lineIndex > 0 ? _lines[lineIndex - 1].SyntaxState : null;
            state = _syntaxHighlighter.Colorize(CollectionsMarshal.AsSpan(glyphs), state);
            _lines[lineIndex] = (glyphs, state);
        }

        _colorRangeMin = to;

        if (_colorRangeMax == _colorRangeMin) // Done?
        {
            _colorRangeMin = int.MaxValue;
            _colorRangeMax = 0;
        }
    }

    float TextDistanceToLineStart(Coordinates aFrom)
    {
        var line = _lines[aFrom.Line];
        float distance = 0.0f;
        float spaceSize = _charWidthCache.Get(' '); // remaining

        int colIndex = GetCharacterIndex(aFrom);
        for (int i = 0; i < line.Glyphs.Count && i < colIndex;)
        {
            var c = line.Glyphs[i].Char;
            distance =
                c == '\t'
                ? (1.0f + MathF.Floor((1.0f + distance) / (_tabSize * spaceSize))) * (_tabSize * spaceSize)
                : distance + _charWidthCache.Get(c);

            i++;
        }

        return distance;
    }

    void EnsureCursorVisible()
    {
        if (!_withinRender)
        {
            _scrollToCursor = true;
            return;
        }

        float scrollX = ImGui.GetScrollX();
        float scrollY = ImGui.GetScrollY();

        var height = ImGui.GetWindowHeight();
        var width = ImGui.GetWindowWidth();

        var top = 1 + (int)MathF.Ceiling(scrollY / _charAdvance.Y);
        var bottom = (int)MathF.Ceiling((scrollY + height) / _charAdvance.Y);

        var left = (int)MathF.Ceiling(scrollX / _charAdvance.X);
        var right = (int)MathF.Ceiling((scrollX + width) / _charAdvance.X);

        var pos = GetActualCursorCoordinates();
        var len = TextDistanceToLineStart(pos);

        if (pos.Line < top)
            ImGui.SetScrollY(Math.Max(0.0f, (pos.Line - 1) * _charAdvance.Y));
        if (pos.Line > bottom - 4)
            ImGui.SetScrollY(Math.Max(0.0f, (pos.Line + 4) * _charAdvance.Y - height));
        if (len + _textStart < left + 4)
            ImGui.SetScrollX(Math.Max(0.0f, len + _textStart - 4));
        if (len + _textStart > right - 4)
            ImGui.SetScrollX(Math.Max(0.0f, len + _textStart + 4 - width));
    }

    int GetPageSize()
    {
        var height = ImGui.GetWindowHeight() - 20.0f;
        return (int)MathF.Floor(height / _charAdvance.Y);
    }

    string GetText(Coordinates aStart, Coordinates aEnd)
    {
        var lstart = aStart.Line;
        var lend = aEnd.Line;
        var istart = GetCharacterIndex(aStart);
        var iend = GetCharacterIndex(aEnd);
        int s = 0;

        for (int i = lstart; i < lend; i++)
            s += _lines[i].Glyphs.Count;

        var result = new StringBuilder(s + s / 8);
        while (istart < iend || lstart < lend)
        {
            if (lstart >= _lines.Count)
                break;

            var line = _lines[lstart].Glyphs;
            if (istart < line.Count)
            {
                result.Append(line[istart].Char);
                istart++;
            }
            else
            {
                istart = 0;
                ++lstart;
                result.Append('\n');
            }
        }

        return result.ToString();
    }

    Coordinates GetActualCursorCoordinates() => SanitizeCoordinates(_state.CursorPosition);
    Coordinates SanitizeCoordinates(Coordinates aValue)
    {
        var line = aValue.Line;
        var column = aValue.Column;
        if (line >= _lines.Count)
        {
            if (_lines.Count == 0)
            {
                line = 0;
                column = 0;
            }
            else
            {
                line = _lines.Count - 1;
                column = GetLineMaxColumn(line);
            }
            return new Coordinates(line, column);
        }
        else
        {
            column = _lines.Count == 0 ? 0 : Math.Min(column, GetLineMaxColumn(line));
            return new Coordinates(line, column);
        }
    }

    void Advance(Coordinates aCoordinates)
    {
        if (aCoordinates.Line < _lines.Count)
        {
            var line = _lines[aCoordinates.Line].Glyphs;
            var cindex = GetCharacterIndex(aCoordinates);

            if (cindex + 1 < line.Count)
            {
                cindex = Math.Min(cindex + 1, line.Count - 1);
            }
            else
            {
                ++aCoordinates.Line;
                cindex = 0;
            }
            aCoordinates.Column = GetCharacterColumn(aCoordinates.Line, cindex);
        }
    }

    void DeleteRange(Coordinates aStart, Coordinates aEnd)
    {
        Util.Assert(aEnd >= aStart);
        Util.Assert(!IsReadOnly);

        // Console.WriteLine($"D({aStart.Line}.{aStart.Column})-({aEnd.Line}.{aEnd.Column})\n");

        if (aEnd == aStart)
            return;

        var start = GetCharacterIndex(aStart);
        var end = GetCharacterIndex(aEnd);

        if (aStart.Line == aEnd.Line)
        {
            var line = _lines[aStart.Line].Glyphs;
            var n = GetLineMaxColumn(aStart.Line);
            if (aEnd.Column >= n)
                line.RemoveRange(start, line.Count - start);
            else
                line.RemoveRange(start, end - start);
        }
        else
        {
            var firstLine = _lines[aStart.Line].Glyphs;
            var lastLine = _lines[aEnd.Line].Glyphs;

            firstLine.RemoveRange(start, firstLine.Count - start);
            lastLine.RemoveRange(0, end);

            if (aStart.Line < aEnd.Line)
                firstLine.AddRange(lastLine);

            if (aStart.Line < aEnd.Line)
                RemoveLine(aStart.Line + 1, aEnd.Line + 1);
        }

        IsTextChanged = true;
    }

    int InsertTextAt(Coordinates pos, string value)
    {
        Util.Assert(!IsReadOnly);

        int cindex = GetCharacterIndex(pos);
        int totalLines = 0;
        foreach (var c in value)
        {
            Util.Assert(_lines.Count != 0);

            if (c == '\r')
                continue;

            if (c == '\n')
            {
                if (cindex < _lines[pos.Line].Glyphs.Count)
                {
                    var newLine = InsertLine(pos.Line + 1);
                    var line = _lines[pos.Line].Glyphs;
                    newLine.InsertRange(0, line.Skip(cindex));
                    line.RemoveRange(cindex, line.Count - cindex);
                }
                else
                {
                    InsertLine(pos.Line + 1);
                }

                pos.Line++;
                pos.Column = 0;
                cindex = 0;
                totalLines++;
            }
            else
            {
                var line = _lines[pos.Line].Glyphs;
                var glyph = new Glyph(c, PaletteIndex.Default);
                line.Insert(cindex, glyph);

                cindex++;
                pos.Column++;
            }

            IsTextChanged = true;
        }

        return totalLines;
    }

    void AddUndo(UndoRecord aValue)
    {
        Util.Assert(!IsReadOnly);
        Debug.WriteLine("AddUndo: (@{0}.{1}) +\'{2}' [{3}.{4} .. {5}.{6}], -\'{7}', [{8}.{9} .. {10}.{11}] (@{12}.{13})\n",
            aValue.Before.CursorPosition.Line, aValue.Before.CursorPosition.Column,
            aValue.Added, aValue.AddedStart.Line, aValue.AddedStart.Column, aValue.AddedEnd.Line, aValue.AddedEnd.Column,
            aValue.Removed, aValue.RemovedStart.Line, aValue.RemovedStart.Column, aValue.RemovedEnd.Line, aValue.RemovedEnd.Column,
            aValue.After.CursorPosition.Line, aValue.After.CursorPosition.Column);

        _undoBuffer.Add(aValue);
        ++_undoIndex;
    }

    Coordinates ScreenPosToCoordinates(Vector2 aPosition)
    {
        Vector2 origin = ImGui.GetCursorScreenPos();
        Vector2 local = new(aPosition.X - origin.X, aPosition.Y - origin.Y);

        int lineNo = Math.Max(0, (int)MathF.Floor(local.Y / _charAdvance.Y));
        int columnCoord = 0;

        if (lineNo < _lines.Count)
        {
            var line = _lines[lineNo].Glyphs;

            int columnIndex = 0;
            float columnX = 0.0f;

            while (columnIndex < line.Count)
            {
                float columnWidth;

                if (line[columnIndex].Char == '\t')
                {
                    float spaceSize = _charWidthCache.Get(' ');
                    float oldX = columnX;
                    float newColumnX = (1.0f + MathF.Floor((1.0f + columnX) / (_tabSize * spaceSize))) * (_tabSize * spaceSize);
                    columnWidth = newColumnX - oldX;
                    if (_textStart + columnX + columnWidth * 0.5f > local.X)
                        break;

                    columnX = newColumnX;
                    columnCoord = (columnCoord / _tabSize) * _tabSize + _tabSize;
                    columnIndex++;
                }
                else
                {
                    columnWidth = _charWidthCache.Get(line[columnIndex++].Char);
                    if (_textStart + columnX + columnWidth * 0.5f > local.X)
                        break;

                    columnX += columnWidth;
                    columnCoord++;
                }
            }
        }

        return SanitizeCoordinates(new Coordinates(lineNo, columnCoord));
    }

    Coordinates FindWordStart(Coordinates aFrom)
    {
        if (aFrom.Line >= _lines.Count)
            return aFrom;

        var line = _lines[aFrom.Line].Glyphs;
        var cindex = GetCharacterIndex(aFrom);

        if (cindex >= line.Count)
            return aFrom;

        while (cindex > 0 && char.IsWhiteSpace(line[cindex].Char))
            --cindex;

        var cstart = line[cindex].ColorIndex;
        while (cindex > 0)
        {
            var c = line[cindex].Char;
            if ((c & 0xC0) != 0x80) // not UTF code sequence 10xxxxxx
            {
                if (c <= 32 && char.IsWhiteSpace(c))
                {
                    cindex++;
                    break;
                }
                if (cstart != line[cindex - 1].ColorIndex)
                    break;
            }
            --cindex;
        }

        return new Coordinates(aFrom.Line, GetCharacterColumn(aFrom.Line, cindex));
    }

    Coordinates FindWordEnd(Coordinates aFrom)
    {
        Coordinates at = aFrom;
        if (at.Line >= _lines.Count)
            return at;

        var line = _lines[at.Line].Glyphs;
        var cindex = GetCharacterIndex(at);

        if (cindex >= line.Count)
            return at;

        bool prevspace = char.IsWhiteSpace(line[cindex].Char);
        var cstart = line[cindex].ColorIndex;
        while (cindex < line.Count)
        {
            var c = line[cindex].Char;
            if (cstart != line[cindex].ColorIndex)
                break;

            if (prevspace != char.IsWhiteSpace(c))
            {
                if (char.IsWhiteSpace(c))
                    while (cindex < line.Count && char.IsWhiteSpace(line[cindex].Char))
                        ++cindex;
                break;
            }
            cindex++;
        }
        return new Coordinates(aFrom.Line, GetCharacterColumn(aFrom.Line, cindex));
    }

    Coordinates FindNextWord(Coordinates aFrom)
    {
        Coordinates at = aFrom;
        if (at.Line >= _lines.Count)
            return at;

        // skip to the next non-word character
        var cindex = GetCharacterIndex(aFrom);
        bool isword = false;
        bool skip = false;
        if (cindex < _lines[at.Line].Glyphs.Count)
        {
            var line = _lines[at.Line].Glyphs;
            isword = char.IsLetterOrDigit(line[cindex].Char);
            skip = isword;
        }

        while (!isword || skip)
        {
            if (at.Line >= _lines.Count)
            {
                var l = Math.Max(0, _lines.Count - 1);
                return new Coordinates(l, GetLineMaxColumn(l));
            }

            var line = _lines[at.Line].Glyphs;
            if (cindex < line.Count)
            {
                isword = char.IsLetterOrDigit(line[cindex].Char);

                if (isword && !skip)
                    return new Coordinates(at.Line, GetCharacterColumn(at.Line, cindex));

                if (!isword)
                    skip = false;

                cindex++;
            }
            else
            {
                cindex = 0;
                ++at.Line;
                skip = false;
                isword = false;
            }
        }

        return at;
    }

    int GetCharacterIndex(Coordinates aCoordinates)
    {
        if (aCoordinates.Line >= _lines.Count)
            return -1;

        var line = _lines[aCoordinates.Line].Glyphs;
        int c = 0;
        int i = 0;

        for (; i < line.Count && c < aCoordinates.Column;)
        {
            if (line[i].Char == '\t')
                c = (c / _tabSize) * _tabSize + _tabSize;
            else
                ++c;
            i++;
        }

        return i;
    }

    int GetCharacterColumn(int aLine, int aIndex)
    {
        if (aLine >= _lines.Count)
            return 0;

        var line = _lines[aLine].Glyphs;
        int col = 0;
        int i = 0;

        while (i < aIndex && i < line.Count)
        {
            var c = line[i].Char;
            i++;
            if (c == '\t')
                col = (col / _tabSize) * _tabSize + _tabSize;
            else
                col++;
        }

        return col;
    }

    int GetLineMaxColumn(int aLine)
    {
        if (aLine >= _lines.Count)
            return 0;

        var line = _lines[aLine].Glyphs;
        int col = 0;

        for (int i = 0; i < line.Count;)
        {
            var c = line[i].Char;
            if (c == '\t')
                col = (col / _tabSize) * _tabSize + _tabSize;
            else
                col++;
            i++;
        }

        return col;
    }

    bool IsOnWordBoundary(Coordinates aAt)
    {
        if (aAt.Line >= _lines.Count || aAt.Column == 0)
            return true;

        var line = _lines[aAt.Line].Glyphs;
        var cindex = GetCharacterIndex(aAt);
        if (cindex >= line.Count)
            return true;

        if (IsColorizerEnabled)
            return line[cindex].ColorIndex != line[cindex - 1].ColorIndex;

        return char.IsWhiteSpace(line[cindex].Char) != char.IsWhiteSpace(line[cindex - 1].Char);
    }

    void RemoveLine(int aStart, int aEnd)
    {
        Util.Assert(!IsReadOnly);
        Util.Assert(aEnd >= aStart);
        Util.Assert(_lines.Count > aEnd - aStart);

        Dictionary<int, string> etmp = new Dictionary<int, string>();
        foreach (var kvp in _errorMarkers)
        {
            int key = kvp.Key >= aStart ? kvp.Key - 1 : kvp.Key;

            if (key >= aStart && key <= aEnd)
                continue;

            etmp[key] = kvp.Value;
        }
        _errorMarkers = etmp;

        HashSet<int> btmp = new HashSet<int>();
        foreach (var i in _breakpoints)
        {
            if (i >= aStart && i <= aEnd)
                continue;
            btmp.Add(i >= aStart ? i - 1 : i);
        }
        _breakpoints = btmp;

        _lines.RemoveRange(aStart, aEnd - aStart);
        Util.Assert(_lines.Count != 0);

        IsTextChanged = true;
    }

    void RemoveLine(int aIndex)
    {
        Util.Assert(!IsReadOnly);
        Util.Assert(_lines.Count > 1);

        Dictionary<int, string> etmp = new Dictionary<int, string>();
        foreach (var i in _errorMarkers)
        {
            var key = i.Key > aIndex ? i.Key - 1 : i.Key;
            if (key - 1 == aIndex)
                continue;
            etmp[key] = i.Value;
        }

        _errorMarkers = etmp;

        HashSet<int> btmp = new();
        foreach (var i in _breakpoints)
        {
            if (i == aIndex)
                continue;
            btmp.Add(i >= aIndex ? i - 1 : i);
        }
        _breakpoints = btmp;

        _lines.RemoveRange(aIndex, _lines.Count - aIndex);
        Util.Assert(_lines.Count != 0);

        IsTextChanged = true;
    }

    List<Glyph> InsertLine(int aIndex)
    {
        Util.Assert(!IsReadOnly);

        var result = BlankLine;
        _lines.Insert(aIndex, result);

        Dictionary<int, string> etmp = new();
        foreach (var i in _errorMarkers)
            etmp[i.Key >= aIndex ? i.Key + 1 : i.Key] = i.Value;
        _errorMarkers = etmp;

        HashSet<int> btmp = new();
        foreach (var i in _breakpoints)
            btmp.Add(i >= aIndex ? i + 1 : i);
        _breakpoints = btmp;

        return result.Glyphs;
    }

    void EnterCharacter(char aChar, bool aShift)
    {
        Util.Assert(!IsReadOnly);

        UndoRecord u = new() { Before = _state };

        if (HasSelection())
        {
            if (aChar == '\t' && _state.SelectionStart.Line != _state.SelectionEnd.Line)
            {

                var start = _state.SelectionStart;
                var end = _state.SelectionEnd;
                var originalEnd = end;

                if (start > end)
                    (start, end) = (end, start);

                start.Column = 0;
                // end._column = end._line < _lines.Count ? _lines[end._line].Count : 0;
                if (end.Column == 0 && end.Line > 0)
                    --end.Line;
                if (end.Line >= _lines.Count)
                    end.Line = _lines.Count == 0 ? 0 : _lines.Count - 1;
                end.Column = GetLineMaxColumn(end.Line);

                //if (end._column >= GetLineMaxColumn(end._line))
                //    end._column = GetLineMaxColumn(end._line) - 1;

                u.RemovedStart = start;
                u.RemovedEnd = end;
                u.Removed = GetText(start, end);

                bool modified = false;

                for (int i = start.Line; i <= end.Line; i++)
                {
                    var line = _lines[i].Glyphs;
                    if (aShift)
                    {
                        if (line.Count != 0)
                        {
                            if (line[0].Char == '\t')
                            {
                                line.RemoveAt(0);
                                modified = true;
                            }
                            else
                            {
                                for (int j = 0; j < _tabSize && line.Count != 0 && line[0].Char == ' '; j++)
                                {
                                    line.RemoveAt(0);
                                    modified = true;
                                }
                            }
                        }
                    }
                    else
                    {
                        line.Insert(0, new Glyph('\t', PaletteIndex.Background));
                        modified = true;
                    }
                }

                if (modified)
                {
                    start = new Coordinates(start.Line, GetCharacterColumn(start.Line, 0));
                    Coordinates rangeEnd;
                    if (originalEnd.Column != 0)
                    {
                        end = new Coordinates(end.Line, GetLineMaxColumn(end.Line));
                        rangeEnd = end;
                        u.Added = GetText(start, end);
                    }
                    else
                    {
                        end = new Coordinates(originalEnd.Line, 0);
                        rangeEnd = new Coordinates(end.Line - 1, GetLineMaxColumn(end.Line - 1));
                        u.Added = GetText(start, rangeEnd);
                    }

                    u.AddedStart = start;
                    u.AddedEnd = rangeEnd;
                    u.After = _state;

                    _state.SelectionStart = start;
                    _state.SelectionEnd = end;
                    AddUndo(u);

                    IsTextChanged = true;

                    EnsureCursorVisible();
                }

                return;
            } // c == '\t'
            else
            {
                u.Removed = GetSelectedText();
                u.RemovedStart = _state.SelectionStart;
                u.RemovedEnd = _state.SelectionEnd;
                DeleteSelection();
            }
        } // HasSelection

        var coord = GetActualCursorCoordinates();
        u.AddedStart = coord;

        Util.Assert(_lines.Count != 0);

        if (aChar == '\n')
        {
            InsertLine(coord.Line + 1);
            var line = _lines[coord.Line].Glyphs;
            var newLine = _lines[coord.Line + 1].Glyphs;

            if (_syntaxHighlighter.AutoIndentation)
                for (int it = 0; it < line.Count && char.IsAscii(line[it].Char) && IsBlank(line[it].Char); ++it)
                    newLine.Add(line[it]);

            int whitespaceSize = newLine.Count;
            var cindex = GetCharacterIndex(coord);
            newLine.AddRange(line.Skip(cindex));
            line.RemoveRange(cindex, line.Count - cindex);
            CursorPosition = new Coordinates(coord.Line + 1, GetCharacterColumn(coord.Line + 1, whitespaceSize));
            u.Added = "\n";
        }
        else
        {
            var line = _lines[coord.Line].Glyphs;
            var cindex = GetCharacterIndex(coord);

            if (_overwrite && cindex < line.Count)
            {
                u.RemovedStart = _state.CursorPosition;
                u.RemovedEnd = new Coordinates(coord.Line, GetCharacterColumn(coord.Line, cindex + 1));

                u.Removed += line[cindex].Char;
                line.RemoveAt(cindex);
            }

            line.Insert(cindex, new Glyph(aChar, PaletteIndex.Default));
            u.Added = _charLabelCache.Get(aChar);

            CursorPosition = new Coordinates(coord.Line, GetCharacterColumn(coord.Line, cindex + 1));
        }

        IsTextChanged = true;

        u.AddedEnd = GetActualCursorCoordinates();
        u.After = _state;

        AddUndo(u);

        InvalidateColor(coord.Line - 1, 3);
        EnsureCursorVisible();
    }

    static bool IsBlank(char c) => c is ' ' or '\t';

    void Backspace()
    {
        Util.Assert(!IsReadOnly);

        if (_lines.Count == 0)
            return;

        UndoRecord u = new() { Before = _state };

        if (HasSelection())
        {
            u.Removed = GetSelectedText();
            u.RemovedStart = _state.SelectionStart;
            u.RemovedEnd = _state.SelectionEnd;

            DeleteSelection();
        }
        else
        {
            var pos = GetActualCursorCoordinates();
            CursorPosition = pos;

            if (_state.CursorPosition.Column == 0)
            {
                if (_state.CursorPosition.Line == 0)
                    return;

                u.Removed = "\n";
                u.RemovedStart = u.RemovedEnd = new Coordinates(pos.Line - 1, GetLineMaxColumn(pos.Line - 1));
                Advance(u.RemovedEnd);

                var line = _lines[_state.CursorPosition.Line].Glyphs;
                var prevLine = _lines[_state.CursorPosition.Line - 1].Glyphs;
                var prevSize = GetLineMaxColumn(_state.CursorPosition.Line - 1);
                prevLine.AddRange(line);

                Dictionary<int, string> etmp = new Dictionary<int, string>();
                foreach (var kvp in _errorMarkers)
                    etmp[kvp.Key - 1 == _state.CursorPosition.Line ? kvp.Key - 1 : kvp.Key] = kvp.Value;
                _errorMarkers = etmp;

                RemoveLine(_state.CursorPosition.Line);
                --_state.CursorPosition.Line;
                _state.CursorPosition.Column = prevSize;
            }
            else
            {
                var line = _lines[_state.CursorPosition.Line].Glyphs;
                var cindex = GetCharacterIndex(pos) - 1;
                var cend = cindex + 1;
                while (cindex > 0 && IsUTFSequence(line[cindex].Char))
                    --cindex;

                u.RemovedStart = u.RemovedEnd = GetActualCursorCoordinates();
                --u.RemovedStart.Column;
                --_state.CursorPosition.Column;

                while (cindex < line.Count && cend-- > cindex)
                {
                    u.Removed += line[cindex].Char;
                    line.RemoveAt(cindex);
                }
            }

            IsTextChanged = true;

            EnsureCursorVisible();
            InvalidateColor(_state.CursorPosition.Line, 1);
        }

        u.After = _state;
        AddUndo(u);
    }

    void DeleteSelection()
    {
        Util.Assert(_state.SelectionEnd >= _state.SelectionStart);

        if (_state.SelectionEnd == _state.SelectionStart)
            return;

        DeleteRange(_state.SelectionStart, _state.SelectionEnd);

        SetSelection(_state.SelectionStart, _state.SelectionStart);
        CursorPosition = _state.SelectionStart;
        InvalidateColor(_state.SelectionStart.Line, 1);
    }

    string GetWordAt(Coordinates aCoords)
    {
        var start = FindWordStart(aCoords);
        var end = FindWordEnd(aCoords);

        var sb = new StringBuilder();

        var istart = GetCharacterIndex(start);
        var iend = GetCharacterIndex(end);

        for (var it = istart; it < iend; ++it)
            sb.Append(_lines[aCoords.Line].Glyphs[it].Char);

        return sb.ToString();
    }

    uint GetGlyphColor(Glyph aGlyph) => _palette[(int)aGlyph.ColorIndex];

    void HandleKeyboardInputs()
    {
        var io = ImGui.GetIO();
        var shift = io.KeyShift;
        var ctrl = io.ConfigMacOSXBehaviors ? io.KeySuper : io.KeyCtrl;
        var alt = io.ConfigMacOSXBehaviors ? io.KeyCtrl : io.KeyAlt;

        if (ImGui.IsWindowFocused())
        {
            if (ImGui.IsWindowHovered())
                ImGui.SetMouseCursor(ImGuiMouseCursor.TextInput);
            //ImGui.CaptureKeyboardFromApp(true);

            io.WantCaptureKeyboard = true;
            io.WantTextInput = true;

            if (!IsReadOnly && ctrl && !shift && !alt && ImGui.IsKeyPressed(ImGui.GetKeyIndex(ImGuiKey.Z)))
                Undo();
            else if (!IsReadOnly && !ctrl && !shift && alt && ImGui.IsKeyPressed(ImGui.GetKeyIndex(ImGuiKey.Backspace)))
                Undo();
            else if (!IsReadOnly && ctrl && !shift && !alt && ImGui.IsKeyPressed(ImGui.GetKeyIndex(ImGuiKey.Y)))
                Redo();
            else if (!ctrl && !alt && ImGui.IsKeyPressed(ImGui.GetKeyIndex(ImGuiKey.UpArrow)))
                MoveUp(1, shift);
            else if (!ctrl && !alt && ImGui.IsKeyPressed(ImGui.GetKeyIndex(ImGuiKey.DownArrow)))
                MoveDown(1, shift);
            else if (!alt && ImGui.IsKeyPressed(ImGui.GetKeyIndex(ImGuiKey.LeftArrow)))
                MoveLeft(1, shift, ctrl);
            else if (!alt && ImGui.IsKeyPressed(ImGui.GetKeyIndex(ImGuiKey.RightArrow)))
                MoveRight(1, shift, ctrl);
            else if (!alt && ImGui.IsKeyPressed(ImGui.GetKeyIndex(ImGuiKey.PageUp)))
                MoveUp(GetPageSize() - 4, shift);
            else if (!alt && ImGui.IsKeyPressed(ImGui.GetKeyIndex(ImGuiKey.PageDown)))
                MoveDown(GetPageSize() - 4, shift);
            else if (!alt && ctrl && ImGui.IsKeyPressed(ImGui.GetKeyIndex(ImGuiKey.Home)))
                MoveTop(shift);
            else if (ctrl && !alt && ImGui.IsKeyPressed(ImGui.GetKeyIndex(ImGuiKey.End)))
                MoveBottom(shift);
            else if (!ctrl && !alt && ImGui.IsKeyPressed(ImGui.GetKeyIndex(ImGuiKey.Home)))
                MoveHome(shift);
            else if (!ctrl && !alt && ImGui.IsKeyPressed(ImGui.GetKeyIndex(ImGuiKey.End)))
                MoveEnd(shift);
            else if (!IsReadOnly && !ctrl && !shift && !alt && ImGui.IsKeyPressed(ImGui.GetKeyIndex(ImGuiKey.Delete)))
                Delete();
            else if (!IsReadOnly && !ctrl && !shift && !alt && ImGui.IsKeyPressed(ImGui.GetKeyIndex(ImGuiKey.Backspace)))
                Backspace();
            else if (!ctrl && !shift && !alt && ImGui.IsKeyPressed(ImGui.GetKeyIndex(ImGuiKey.Insert)))
                _overwrite ^= true;
            else if (ctrl && !shift && !alt && ImGui.IsKeyPressed(ImGui.GetKeyIndex(ImGuiKey.Insert)))
                Copy();
            else if (ctrl && !shift && !alt && ImGui.IsKeyPressed(ImGui.GetKeyIndex(ImGuiKey.C)))
                Copy();
            else if (!IsReadOnly && !ctrl && shift && !alt && ImGui.IsKeyPressed(ImGui.GetKeyIndex(ImGuiKey.Insert)))
                Paste();
            else if (!IsReadOnly && ctrl && !shift && !alt && ImGui.IsKeyPressed(ImGui.GetKeyIndex(ImGuiKey.V)))
                Paste();
            else if (ctrl && !shift && !alt && ImGui.IsKeyPressed(ImGui.GetKeyIndex(ImGuiKey.X)))
                Cut();
            else if (!ctrl && shift && !alt && ImGui.IsKeyPressed(ImGui.GetKeyIndex(ImGuiKey.Delete)))
                Cut();
            else if (ctrl && !shift && !alt && ImGui.IsKeyPressed(ImGui.GetKeyIndex(ImGuiKey.A)))
                SelectAll();
            else if (!IsReadOnly && !ctrl && !shift && !alt && ImGui.IsKeyPressed(ImGui.GetKeyIndex(ImGuiKey.Enter)))
                EnterCharacter('\n', false);
            else if (!IsReadOnly && !ctrl && !alt && ImGui.IsKeyPressed(ImGui.GetKeyIndex(ImGuiKey.Tab)))
                EnterCharacter('\t', shift);

            if (!IsReadOnly && io.InputQueueCharacters.Size != 0)
            {
                for (int i = 0; i < io.InputQueueCharacters.Size; i++)
                {
                    var c = io.InputQueueCharacters[i];
                    if (c != 0 && c is '\n' or >= 32)
                        EnterCharacter((char)c, shift);
                }

                // io.InputQueueCharacters.resize(0); // TODO: Revisit
            }
        }
    }

    void HandleMouseInputs()
    {
        var io = ImGui.GetIO();
        var shift = io.KeyShift;
        var ctrl = io.ConfigMacOSXBehaviors ? io.KeySuper : io.KeyCtrl;
        var alt = io.ConfigMacOSXBehaviors ? io.KeyCtrl : io.KeyAlt;

        if (!ImGui.IsWindowHovered())
            return;

        if (shift || alt)
            return;

        var click = ImGui.IsMouseClicked(0);
        var doubleClick = ImGui.IsMouseDoubleClicked(0);
        var t = ImGui.GetTime();
        var tripleClick = 
            click && 
            !doubleClick && 
            _lastClick != -1.0f 
            && t - _lastClick < io.MouseDoubleClickTime;

        /* Left mouse button triple click */
        if (tripleClick)
        {
            if (!ctrl)
            {
                _state.CursorPosition = _interactiveStart = _interactiveEnd = ScreenPosToCoordinates(ImGui.GetMousePos());
                _selectionMode = SelectionMode.Line;
                SetSelection(_interactiveStart, _interactiveEnd, _selectionMode);
            }

            _lastClick = -1.0f;
        }

        /* Left mouse button double click */
        else if (doubleClick)
        {
            if (!ctrl)
            {
                _state.CursorPosition = _interactiveStart = _interactiveEnd = ScreenPosToCoordinates(ImGui.GetMousePos());
                _selectionMode = _selectionMode == SelectionMode.Line ? SelectionMode.Normal : SelectionMode.Word;
                SetSelection(_interactiveStart, _interactiveEnd, _selectionMode);
            }

            _lastClick = (float)ImGui.GetTime();
        }

        /* Left mouse button click */
        else if (click)
        {
            _state.CursorPosition = _interactiveStart = _interactiveEnd = ScreenPosToCoordinates(ImGui.GetMousePos());
            _selectionMode = ctrl ? SelectionMode.Word : SelectionMode.Normal;
            SetSelection(_interactiveStart, _interactiveEnd, _selectionMode);

            _lastClick = (float)ImGui.GetTime();
        }
        // Mouse left button dragging (=> update selection)
        else if (ImGui.IsMouseDragging(0) && ImGui.IsMouseDown(0))
        {
            io.WantCaptureMouse = true;
            _state.CursorPosition = _interactiveEnd = ScreenPosToCoordinates(ImGui.GetMousePos());
            SetSelection(_interactiveStart, _interactiveEnd, _selectionMode);
        }
    }

    void Render()
    {
        /* Compute _charAdvance regarding to scaled font size (Ctrl + mouse wheel)*/
        float fontSize = _charWidthCache.Get('#');
        _charAdvance = new Vector2(fontSize, ImGui.GetTextLineHeightWithSpacing() * _lineSpacing);

        /* Update palette with the current alpha from style */
        for (int i = 0; i < (int)PaletteIndex.Max; ++i)
        {
            var color = ImGui.ColorConvertU32ToFloat4(Palette[i]);
            color.W *= ImGui.GetStyle().Alpha;
            _palette[i] = ImGui.ColorConvertFloat4ToU32(color);
        }

        Util.Assert(_lineBuffer.Length == 0);

        var contentSize = ImGui.GetWindowContentRegionMax();
        var drawList = ImGui.GetWindowDrawList();
        float longest = _textStart;

        if (_scrollToTop)
        {
            _scrollToTop = false;
            ImGui.SetScrollY(0f);
        }

        Vector2 cursorScreenPos = ImGui.GetCursorScreenPos();
        var scrollX = ImGui.GetScrollX();
        var scrollY = ImGui.GetScrollY();

        var lineNo = (int)MathF.Floor(scrollY / _charAdvance.Y);
        var globalLineMax = _lines.Count;
        var lineMax = Math.Max(0, Math.Min(_lines.Count - 1, lineNo + (int)MathF.Floor((scrollY + contentSize.Y) / _charAdvance.Y)));

        // Deduce _textStart by evaluating _lines size (global lineMax) plus two spaces as text width
        float spaceSize = _charWidthCache.Get(' ');
        var buf = _lineNumberCache.Get(globalLineMax);
        _textStart = ImGui.CalcTextSize(buf).X + _leftMargin + spaceSize;

        if (_lines.Count != 0)
        {
            while (lineNo <= lineMax)
            {
                Vector2 lineStartScreenPos = cursorScreenPos with { Y = cursorScreenPos.Y + lineNo * _charAdvance.Y };
                Vector2 textScreenPos = lineStartScreenPos with { X = lineStartScreenPos.X + _textStart };

                var line = _lines[lineNo].Glyphs;
                longest = Math.Max(
                    _textStart + TextDistanceToLineStart(new Coordinates(lineNo, GetLineMaxColumn(lineNo))),
                    longest);

                Coordinates lineStartCoord = new(lineNo, 0);
                Coordinates lineEndCoord = new(lineNo, GetLineMaxColumn(lineNo));

                // Draw selection for the current line
                float sstart = -1.0f;
                float ssend = -1.0f;

                Util.Assert(_state.SelectionStart <= _state.SelectionEnd);
                if (_state.SelectionStart <= lineEndCoord)
                    sstart = _state.SelectionStart > lineStartCoord ? TextDistanceToLineStart(_state.SelectionStart) : 0.0f;
                if (_state.SelectionEnd > lineStartCoord)
                    ssend = TextDistanceToLineStart(_state.SelectionEnd < lineEndCoord ? _state.SelectionEnd : lineEndCoord);

                if (_state.SelectionEnd.Line > lineNo)
                    ssend += _charAdvance.X;

                if (sstart != -1 && ssend != -1 && sstart < ssend)
                {
                    Vector2 vstart = lineStartScreenPos with { X = lineStartScreenPos.X + _textStart + sstart };
                    Vector2 vend = new(lineStartScreenPos.X + _textStart + ssend, lineStartScreenPos.Y + _charAdvance.Y);
                    drawList.AddRectFilled(vstart, vend, _palette[(int)PaletteIndex.Selection]);
                }

                // Draw breakpoints
                var start = lineStartScreenPos with { X = lineStartScreenPos.X + scrollX };

                if (_breakpoints.Contains(lineNo + 1))
                {
                    var end = new Vector2(
                        lineStartScreenPos.X + contentSize.X + 2.0f * scrollX,
                        lineStartScreenPos.Y + _charAdvance.Y);

                    drawList.AddRectFilled(start, end, _palette[(int)PaletteIndex.Breakpoint]);
                }

                // Draw error markers
                if (_errorMarkers.TryGetValue(lineNo + 1, out var error))
                {
                    var end = new Vector2(
                        lineStartScreenPos.X + contentSize.X + 2.0f * scrollX,
                        lineStartScreenPos.Y + _charAdvance.Y);

                    drawList.AddRectFilled(start, end, _palette[(int)PaletteIndex.ErrorMarker]);

                    if (ImGui.IsMouseHoveringRect(lineStartScreenPos, end))
                    {
                        ImGui.BeginTooltip();
                        ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(1.0f, 0.2f, 0.2f, 1.0f));
                        ImGui.Text($"Error at line {lineNo + 1}:");
                        ImGui.PopStyleColor();
                        ImGui.Separator();
                        ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(1.0f, 1.0f, 0.2f, 1.0f));
                        ImGui.Text(error);
                        ImGui.PopStyleColor();
                        ImGui.EndTooltip();
                    }
                }

                // Draw line number (right aligned)
                buf = _lineNumberCache.Get(lineNo + 1);

                var lineNoWidth = ImGui.CalcTextSize(buf).X;
                drawList.AddText(
                    lineStartScreenPos with { X = lineStartScreenPos.X + _textStart - lineNoWidth },
                    _palette[(int)PaletteIndex.LineNumber],
                    buf);

                if (_state.CursorPosition.Line == lineNo)
                {
                    var focused = ImGui.IsWindowFocused();

                    // Highlight the current line (where the cursor is)
                    if (!HasSelection())
                    {
                        var end = new Vector2(start.X + contentSize.X + scrollX, start.Y + _charAdvance.Y);
                        drawList.AddRectFilled(
                            start,
                            end,
                            _palette[(int)(focused ? PaletteIndex.CurrentLineFill : PaletteIndex.CurrentLineFillInactive)]);

                        drawList.AddRect(start, end, _palette[(int)PaletteIndex.CurrentLineEdge], 1.0f);
                    }

                    // Render the cursor
                    if (focused)
                    {
                        var timeEnd = DateTime.UtcNow;
                        var elapsed = timeEnd - _startTime;
                        if (elapsed.Milliseconds > 400)
                        {
                            float width = 1.0f;
                            var cindex = GetCharacterIndex(_state.CursorPosition);
                            float cx = TextDistanceToLineStart(_state.CursorPosition);

                            if (_overwrite && cindex < line.Count)
                            {
                                var c = line[cindex].Char;
                                if (c == '\t')
                                {
                                    var x = (1.0f + MathF.Floor((1.0f + cx) / (_tabSize * spaceSize))) * (_tabSize * spaceSize);
                                    width = x - cx;
                                }
                                else
                                {
                                    width = _charWidthCache.Get(line[cindex].Char);
                                }
                            }
                            Vector2 cstart = lineStartScreenPos with { X = textScreenPos.X + cx };
                            Vector2 cend = new(textScreenPos.X + cx + width, lineStartScreenPos.Y + _charAdvance.Y);
                            drawList.AddRectFilled(cstart, cend, _palette[(int)PaletteIndex.Cursor]);
                            if (elapsed.Milliseconds > 800)
                                _startTime = timeEnd;
                        }
                    }
                }

                // Render colorized text
                var prevColor = line.Count == 0 ? _palette[(int)PaletteIndex.Default] : GetGlyphColor(line[0]);
                var bufferOffset = new Vector2();

                for (int i = 0; i < line.Count;)
                {
                    var glyph = line[i];
                    var color = GetGlyphColor(glyph);

                    if ((color != prevColor || glyph.Char == '\t' || glyph.Char == ' ') && _lineBuffer.Length != 0)
                    {
                        Vector2 newOffset = new(textScreenPos.X + bufferOffset.X, textScreenPos.Y + bufferOffset.Y);
                        drawList.AddText(newOffset, prevColor, _lineBuffer);
                        var textSize = ImGui.CalcTextSize(_lineBuffer);
                        bufferOffset.X += textSize.X;
                        _lineBuffer = "";
                    }
                    prevColor = color;

                    if (glyph.Char == '\t')
                    {
                        var oldX = bufferOffset.X;
                        bufferOffset.X = (1.0f + MathF.Floor((1.0f + bufferOffset.X) / (_tabSize * spaceSize))) * (_tabSize * spaceSize);
                        ++i;

                        if (IsShowingWhitespaces)
                        {
                            var s = ImGui.GetFontSize();
                            var x1 = textScreenPos.X + oldX + 1.0f;
                            var x2 = textScreenPos.X + bufferOffset.X - 1.0f;
                            var y = textScreenPos.Y + bufferOffset.Y + s * 0.5f;
                            Vector2 p1 = new(x1, y);
                            Vector2 p2 = new(x2, y);
                            Vector2 p3 = new(x2 - s * 0.2f, y - s * 0.2f);
                            Vector2 p4 = new(x2 - s * 0.2f, y + s * 0.2f);
                            drawList.AddLine(p1, p2, 0x90909090);
                            drawList.AddLine(p2, p3, 0x90909090);
                            drawList.AddLine(p2, p4, 0x90909090);
                        }
                    }
                    else if (glyph.Char == ' ')
                    {
                        if (IsShowingWhitespaces)
                        {
                            var s = ImGui.GetFontSize();
                            var x = textScreenPos.X + bufferOffset.X + spaceSize * 0.5f;
                            var y = textScreenPos.Y + bufferOffset.Y + s * 0.5f;
                            drawList.AddCircleFilled(new Vector2(x, y), 1.5f, 0x80808080, 4);
                        }
                        bufferOffset.X += spaceSize;
                        i++;
                    }
                    else
                    {
                        _lineBuffer += line[i++].Char;
                    }
                }

                if (_lineBuffer.Length != 0)
                {
                    Vector2 newOffset = new(textScreenPos.X + bufferOffset.X, textScreenPos.Y + bufferOffset.Y);
                    drawList.AddText(newOffset, prevColor, _lineBuffer);
                    _lineBuffer = "";
                }

                ++lineNo;
            }

            if (ImGui.IsMousePosValid())
            {
                var id = GetWordAt(ScreenPosToCoordinates(ImGui.GetMousePos()));
                if (id.Length != 0)
                {
                    var tooltip = _syntaxHighlighter.GetTooltip(id);
                    if (tooltip != null)
                    {
                        ImGui.BeginTooltip();
                        ImGui.TextUnformatted(tooltip);
                        ImGui.EndTooltip();
                    }
                }
            }
        }

        ImGui.Dummy(new Vector2(longest + 2, _lines.Count * _charAdvance.Y));

        if (_scrollToCursor)
        {
            EnsureCursorVisible();
            ImGui.SetWindowFocus();
            _scrollToCursor = false;
        }
    }
}
