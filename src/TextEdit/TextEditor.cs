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

    readonly List<UndoRecord> _undoBuffer = new();
    readonly List<Line> _lines = new();
    readonly uint[] _palette = new uint[(int)PaletteIndex.Max];
    public ISyntaxHighlighter SyntaxHighlighter { get; init; } = NullSyntaxHighlighter.Instance;
    public ITextEditorKeyboardInput KeyboardInput { get; init; } = StandardKeyboardInput.Instance;

    // Note: if fonts / sizes can ever be changed the char width cache will need to be invalidated.
    readonly SimpleCache<char, float> _charWidthCache = new("char widths", x => ImGui.CalcTextSize(x.ToString()).X);
    readonly SimpleCache<int, string> _lineNumberCache = new("line numbers", x => $"{x} ");
    readonly SimpleCache<char, string> _charLabelCache = new("char strings", x => x.ToString());

    HashSet<int> _breakpoints = new();
    Dictionary<int, string> _errorMarkers = new();
    EditorState _state;
    internal int UndoCount => _undoBuffer.Count; // Only for unit testing
    internal int _undoIndex { get; private set; } // Only 'internal' for unit testing purposes
    int _tabSize = 4;
    bool _withinRender;
    bool _scrollToCursor;
    bool _scrollToTop;
    float _textStart = 20.0f; // position (in pixels) where a code line starts relative to the left of the TextEditor.
    int _colorRangeMin;
    int _colorRangeMax;

    SelectionMode _selectionMode = SelectionMode.Normal;
    Vector2 _charAdvance;
    Coordinates _interactiveStart, _interactiveEnd;
    string _lineBuffer = "";
    DateTime _startTime = DateTime.UtcNow;
    float _lastClick = -1.0f;

    public TextEditor() => _lines.Add(new Line());

    public uint[] Palette { get; set; } = Palettes.Dark;
    public void SetErrorMarkers(Dictionary<int, string> value) => _errorMarkers = value;
    public void SetBreakpoints(HashSet<int> value) => _breakpoints = value;

    public string Text
    {
        get => GetText((0, 0), (_lines.Count, 0));
        set
        {
            _lines.Clear();
            _lines.Add(new Line());

            foreach (var chr in value)
            {
                if (chr == '\r')
                {
                    // ignore the carriage return character
                }
                else if (chr == '\n')
                {
                    _lines.Add(new Line());
                }
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

    public IList<string> TextLines
    {
        get
        {
            var result = new string[_lines.Count];

            var sb = new StringBuilder();
            for (int i = 0; i < _lines.Count; i++)
            {
                sb.Clear();

                var line = _lines[i].Glyphs;
                for (int j = 0; j < line.Count; ++j)
                    sb.Append(line[j].Char);

                result[i] = sb.ToString();
            }

            return result;
        }
        set
        {
            _lines.Clear();

            if (value.Count == 0)
            {
                _lines.Add(new Line());
            }
            else
            {
                _lines.Capacity = value.Count;
                foreach (var stringLine in value)
                {
                    var internalLine = new Line(new List<Glyph>(stringLine.Length), null);
                    foreach (var c in stringLine)
                        internalLine.Glyphs.Add(new Glyph(c, PaletteIndex.Default));

                    _lines.Add(internalLine);
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
                (_state.CursorPosition.Line, 0),
                (_state.CursorPosition.Line, lineLength));
    }

    public int TotalLines => _lines.Count;
    public bool IsOverwrite { get; set; }
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
    public bool IsShowingWhitespace { get; set; } = true;
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

        KeyboardInput.HandleKeyboardInputs(this);
        if (IsHandleMouseInputsEnabled)
            HandleMouseInputs();

        ColorizeIncremental();
        Render();

        if (IsHandleKeyboardInputsEnabled)
            ImGui.PopAllowKeyboardFocus();

        if (!IsImGuiChildIgnored)
            ImGui.EndChild();

        ImGui.PopStyleVar();
        ImGui.PopStyleColor();

        _withinRender = false;
    }

    public void InsertText(string value)
    {
        if (string.IsNullOrEmpty(value))
            return;

        var pos = GetActualCursorCoordinates();
        var start = pos < _state.SelectionStart ? pos : _state.SelectionStart;
        int totalLines = pos.Line - start.Line;

        totalLines += InsertTextAt(pos, value);

        SetSelection(pos, pos);
        CursorPosition = pos;
        InvalidateColor(start.Line - 1, totalLines + 2);
    }

    public Coordinates SelectionStart
    {
        get => _state.SelectionStart;
        set {
            _state.SelectionStart = SanitizeCoordinates(value);
            if (_state.SelectionStart > _state.SelectionEnd)
                (_state.SelectionStart, _state.SelectionEnd) = (_state.SelectionEnd, _state.SelectionStart);
        }
    }

    public Coordinates SelectionEnd
    {
        get => _state.SelectionEnd;
        set
        {
            _state.SelectionEnd = SanitizeCoordinates(value);
            if (_state.SelectionStart > _state.SelectionEnd)
                (_state.SelectionStart, _state.SelectionEnd) = (_state.SelectionEnd, _state.SelectionStart);
        }
    }

    public void SetSelection(Coordinates start, Coordinates end, SelectionMode mode = SelectionMode.Normal)
    {
        var oldSelStart = _state.SelectionStart;
        var oldSelEnd = _state.SelectionEnd;

        _state.SelectionStart = SanitizeCoordinates(start);
        _state.SelectionEnd = SanitizeCoordinates(end);
        if (_state.SelectionStart > _state.SelectionEnd)
            (_state.SelectionStart, _state.SelectionEnd) = (_state.SelectionEnd, _state.SelectionStart);

        switch (mode)
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
                    _state.SelectionStart = (_state.SelectionStart.Line, 0);
                    _state.SelectionEnd = (lineNo, GetLineMaxColumn(lineNo));
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

    public void SelectAll() => SetSelection((0, 0), (_lines.Count, 0));
    public bool HasSelection => _state.SelectionEnd > _state.SelectionStart;

    public void MoveUp(int amount = 1, bool isSelecting = false)
    {
        var oldPos = _state.CursorPosition;
        _state.CursorPosition.Line = Math.Max(0, _state.CursorPosition.Line - amount);
        if (oldPos != _state.CursorPosition)
        {
            if (isSelecting)
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

    public void MoveDown(int amount = 1, bool isSelecting = false)
    {
        Util.Assert(_state.CursorPosition.Column >= 0);
        var oldPos = _state.CursorPosition;
        _state.CursorPosition.Line = Math.Max(0, Math.Min(_lines.Count - 1, _state.CursorPosition.Line + amount));

        if (_state.CursorPosition != oldPos)
        {
            if (isSelecting)
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

    public void MoveLeft(int amount = 1, bool isSelecting = false, bool isWordMode = false)
    {
        if (_lines.Count == 0)
            return;

        var oldPos = _state.CursorPosition;
        _state.CursorPosition = GetActualCursorCoordinates();
        var line = _state.CursorPosition.Line;
        var cindex = GetCharacterIndex(_state.CursorPosition);

        while (amount-- > 0)
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

            _state.CursorPosition = (line, GetCharacterColumn(line, cindex));
            if (isWordMode)
            {
                _state.CursorPosition = FindWordStart(_state.CursorPosition);
                cindex = GetCharacterIndex(_state.CursorPosition);
            }
        }

        _state.CursorPosition = (line, GetCharacterColumn(line, cindex));

        Util.Assert(_state.CursorPosition.Column >= 0);
        if (isSelecting)
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
        SetSelection(_interactiveStart, _interactiveEnd, isSelecting && isWordMode ? SelectionMode.Word : SelectionMode.Normal);

        EnsureCursorVisible();
    }

    public void MoveRight(int amount = 1, bool isSelecting = false, bool isWordMode = false)
    {
        var oldPos = _state.CursorPosition;

        if (_lines.Count == 0 || oldPos.Line >= _lines.Count)
            return;

        var cindex = GetCharacterIndex(_state.CursorPosition);
        while (amount-- > 0)
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
                _state.CursorPosition = (lindex, GetCharacterColumn(lindex, cindex));
                if (isWordMode)
                    _state.CursorPosition = FindNextWord(_state.CursorPosition);
            }
        }

        if (isSelecting)
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
        SetSelection(_interactiveStart, _interactiveEnd, isSelecting && isWordMode ? SelectionMode.Word : SelectionMode.Normal);

        EnsureCursorVisible();
    }

    public void MoveTop(bool isSelecting = false)
    {
        var oldPos = _state.CursorPosition;
        CursorPosition = (0, 0);

        if (_state.CursorPosition != oldPos)
        {
            if (isSelecting)
            {
                _interactiveEnd = oldPos;
                _interactiveStart = _state.CursorPosition;
            }
            else
                _interactiveStart = _interactiveEnd = _state.CursorPosition;
            SetSelection(_interactiveStart, _interactiveEnd);
        }
    }

    public void MoveBottom(bool isSelecting = false)
    {
        var oldPos = CursorPosition;
        var newPos = (_lines.Count - 1, 0);
        CursorPosition = newPos;

        if (isSelecting)
        {
            _interactiveStart = oldPos;
            _interactiveEnd = newPos;
        }
        else
            _interactiveStart = _interactiveEnd = newPos;

        SetSelection(_interactiveStart, _interactiveEnd);
    }

    public void MoveHome(bool isSelecting = false)
    {
        var oldPos = _state.CursorPosition;
        CursorPosition = (_state.CursorPosition.Line, 0);

        if (_state.CursorPosition != oldPos)
        {
            if (isSelecting)
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

    public void MoveEnd(bool isSelecting = false)
    {
        var oldPos = _state.CursorPosition;
        CursorPosition = (_state.CursorPosition.Line, GetLineMaxColumn(oldPos.Line));

        if (_state.CursorPosition == oldPos) 
            return;

        if (isSelecting)
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

    public void Copy()
    {
        if (HasSelection)
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

        if (!HasSelection)
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
        Util.Assert(!IsReadOnly);

        var clipText = ImGui.GetClipboardText();
        if (string.IsNullOrEmpty(clipText))
            return;

        UndoRecord u = new()
        {
            Before = _state,
            Added = clipText,
            AddedStart = GetActualCursorCoordinates()
        };

        if (HasSelection)
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
        if (IsReadOnly)
            return;

        if (_lines.Count == 0)
            return;

        UndoRecord u = new() { Before = _state };

        if (HasSelection)
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

        int increment = SyntaxHighlighter.MaxLinesPerFrame;
        int to = Math.Min(_colorRangeMin + increment, _colorRangeMax);

        for (int lineIndex = _colorRangeMin; lineIndex < to; lineIndex++)
        {
            var glyphs = _lines[lineIndex].Glyphs;
            var state = lineIndex > 0 ? _lines[lineIndex - 1].SyntaxState : null;
            state = SyntaxHighlighter.Colorize(CollectionsMarshal.AsSpan(glyphs), state);
            _lines[lineIndex] = new Line(glyphs, state);
        }

        _colorRangeMin = Math.Max(0, to);

        if (_colorRangeMax == _colorRangeMin) // Done?
        {
            _colorRangeMin = int.MaxValue;
            _colorRangeMax = 0;
        }
    }

    float TextDistanceToLineStart(Coordinates position)
    {
        var line = _lines[position.Line];
        float distance = 0.0f;
        float spaceSize = _charWidthCache.Get(' '); // remaining

        int colIndex = GetCharacterIndex(position);
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

    public int PageSize
    {
        get
        {
            var height = ImGui.GetWindowHeight() - 20.0f;
            return (int)MathF.Floor(height / _charAdvance.Y);
        }
    }

    string GetText(Coordinates startPos, Coordinates endPos)
    {
        var lstart = startPos.Line;
        var lend = endPos.Line;
        var istart = GetCharacterIndex(startPos);
        var iend = GetCharacterIndex(endPos);
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
                if (lstart < _lines.Count)
                    result.Append(Environment.NewLine);
            }
        }

        return result.ToString();
    }

    Coordinates GetActualCursorCoordinates() => SanitizeCoordinates(_state.CursorPosition);
    Coordinates SanitizeCoordinates(Coordinates value)
    {
        var line = value.Line;
        var column = value.Column;
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
            return (line, column);
        }
        else
        {
            column = _lines.Count == 0 ? 0 : Math.Min(column, GetLineMaxColumn(line));
            return (line, column);
        }
    }

    void Advance(Coordinates position)
    {
        if (position.Line < _lines.Count)
        {
            var line = _lines[position.Line].Glyphs;
            var cindex = GetCharacterIndex(position);

            if (cindex + 1 < line.Count)
            {
                cindex = Math.Min(cindex + 1, line.Count - 1);
            }
            else
            {
                ++position.Line;
                cindex = 0;
            }
            position.Column = GetCharacterColumn(position.Line, cindex);
        }
    }

    void DeleteRange(Coordinates startPos, Coordinates endPos)
    {
        Util.Assert(endPos >= startPos);
        Util.Assert(!IsReadOnly);

        // Console.WriteLine($"D({startPos.Line}.{startPos.Column})-({endPos.Line}.{endPos.Column})\n");

        if (endPos == startPos)
            return;

        var start = GetCharacterIndex(startPos);
        var end = GetCharacterIndex(endPos);

        if (startPos.Line == endPos.Line)
        {
            var line = _lines[startPos.Line].Glyphs;
            var n = GetLineMaxColumn(startPos.Line);
            if (endPos.Column >= n)
                line.RemoveRange(start, line.Count - start);
            else
                line.RemoveRange(start, end - start);
        }
        else
        {
            var firstLine = _lines[startPos.Line].Glyphs;
            var lastLine = _lines[endPos.Line].Glyphs;

            firstLine.RemoveRange(start, firstLine.Count - start);
            lastLine.RemoveRange(0, end);

            if (startPos.Line < endPos.Line)
                firstLine.AddRange(lastLine);

            if (startPos.Line < endPos.Line)
                RemoveLine(startPos.Line + 1, endPos.Line + 1);
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

    void AddUndo(UndoRecord value)
    {
        Util.Assert(!IsReadOnly);
        Debug.WriteLine("AddUndo: (@{0}.{1}) +\'{2}' [{3}.{4} .. {5}.{6}], -\'{7}', [{8}.{9} .. {10}.{11}] (@{12}.{13})\n",
            value.Before.CursorPosition.Line, value.Before.CursorPosition.Column,
            value.Added, value.AddedStart.Line, value.AddedStart.Column, value.AddedEnd.Line, value.AddedEnd.Column,
            value.Removed, value.RemovedStart.Line, value.RemovedStart.Column, value.RemovedEnd.Line, value.RemovedEnd.Column,
            value.After.CursorPosition.Line, value.After.CursorPosition.Column);

        _undoBuffer.Insert(_undoIndex, value);
        ++_undoIndex;
    }

    Coordinates ScreenPosToCoordinates(Vector2 position)
    {
        Vector2 origin = ImGui.GetCursorScreenPos();
        Vector2 local = new(position.X - origin.X, position.Y - origin.Y);

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
                    columnCoord = columnCoord / _tabSize * _tabSize + _tabSize;
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

        return SanitizeCoordinates((lineNo, columnCoord));
    }

    Coordinates FindWordStart(Coordinates position)
    {
        if (position.Line >= _lines.Count)
            return position;

        var line = _lines[position.Line].Glyphs;
        var cindex = GetCharacterIndex(position);

        if (cindex >= line.Count)
            return position;

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

        return (position.Line, GetCharacterColumn(position.Line, cindex));
    }

    Coordinates FindWordEnd(Coordinates position)
    {
        if (position.Line >= _lines.Count)
            return position;

        var line = _lines[position.Line].Glyphs;
        var cindex = GetCharacterIndex(position);

        if (cindex >= line.Count)
            return position;

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
        return (position.Line, GetCharacterColumn(position.Line, cindex));
    }

    Coordinates FindNextWord(Coordinates from)
    {
        Coordinates at = from;
        if (at.Line >= _lines.Count)
            return at;

        // skip to the next non-word character
        var cindex = GetCharacterIndex(from);
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
                return (l, GetLineMaxColumn(l));
            }

            var line = _lines[at.Line].Glyphs;
            if (cindex < line.Count)
            {
                isword = char.IsLetterOrDigit(line[cindex].Char);

                if (isword && !skip)
                    return (at.Line, GetCharacterColumn(at.Line, cindex));

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

    int GetCharacterIndex(Coordinates position)
    {
        if (position.Line >= _lines.Count)
            return -1;

        var line = _lines[position.Line].Glyphs;
        int c = 0;
        int i = 0;

        for (; i < line.Count && c < position.Column;)
        {
            if (line[i].Char == '\t')
                c = c / _tabSize * _tabSize + _tabSize;
            else
                ++c;
            i++;
        }

        return i;
    }

    int GetCharacterColumn(int lineNumber, int columnNumber)
    {
        if (lineNumber >= _lines.Count)
            return 0;

        var line = _lines[lineNumber].Glyphs;
        int col = 0;
        int i = 0;

        while (i < columnNumber && i < line.Count)
        {
            var c = line[i].Char;
            i++;
            if (c == '\t')
                col = col / _tabSize * _tabSize + _tabSize;
            else
                col++;
        }

        return col;
    }

    int GetLineMaxColumn(int lineNumber)
    {
        if (lineNumber >= _lines.Count)
            return 0;

        var line = _lines[lineNumber].Glyphs;
        int col = 0;

        for (int i = 0; i < line.Count;)
        {
            var c = line[i].Char;
            if (c == '\t')
                col = col / _tabSize * _tabSize + _tabSize;
            else
                col++;
            i++;
        }

        return col;
    }

    bool IsOnWordBoundary(Coordinates position)
    {
        if (position.Line >= _lines.Count || position.Column == 0)
            return true;

        var line = _lines[position.Line].Glyphs;
        var cindex = GetCharacterIndex(position);
        if (cindex >= line.Count)
            return true;

        if (IsColorizerEnabled)
            return line[cindex].ColorIndex != line[cindex - 1].ColorIndex;

        return char.IsWhiteSpace(line[cindex].Char) != char.IsWhiteSpace(line[cindex - 1].Char);
    }

    void RemoveLine(int start, int end)
    {
        Util.Assert(!IsReadOnly);
        Util.Assert(end >= start);
        Util.Assert(_lines.Count > end - start);

        var tempErrors = new Dictionary<int, string>();
        foreach (var kvp in _errorMarkers)
        {
            int key = kvp.Key >= start ? kvp.Key - 1 : kvp.Key;
            if (key >= start && key <= end)
                continue;

            tempErrors[key] = kvp.Value;
        }
        _errorMarkers = tempErrors;

        HashSet<int> btmp = new HashSet<int>();
        foreach (var i in _breakpoints)
        {
            if (i >= start && i <= end)
                continue;
            btmp.Add(i >= start ? i - 1 : i);
        }
        _breakpoints = btmp;

        _lines.RemoveRange(start, end - start);
        Util.Assert(_lines.Count != 0);

        IsTextChanged = true;
    }

    void RemoveLine(int lineNumber)
    {
        Util.Assert(!IsReadOnly);
        Util.Assert(_lines.Count > 1);

        var tempErrors = new Dictionary<int, string>();
        foreach (var i in _errorMarkers)
        {
            var key = i.Key > lineNumber ? i.Key - 1 : i.Key;
            if (key - 1 == lineNumber)
                continue;
            tempErrors[key] = i.Value;
        }

        _errorMarkers = tempErrors;

        HashSet<int> tempBreakpoints = new();
        foreach (var i in _breakpoints)
        {
            if (i == lineNumber)
                continue;

            tempBreakpoints.Add(i >= lineNumber ? i - 1 : i);
        }
        _breakpoints = tempBreakpoints;

        _lines.RemoveAt(lineNumber);
        Util.Assert(_lines.Count != 0);

        IsTextChanged = true;
    }

    List<Glyph> InsertLine(int lineNumber)
    {
        Util.Assert(!IsReadOnly);

        var result = new Line();
        _lines.Insert(lineNumber, result);

        var tempErrors = new Dictionary<int, string>();
        foreach (var i in _errorMarkers)
            tempErrors[i.Key >= lineNumber ? i.Key + 1 : i.Key] = i.Value;
        _errorMarkers = tempErrors;

        HashSet<int> btmp = new();
        foreach (var i in _breakpoints)
            btmp.Add(i >= lineNumber ? i + 1 : i);
        _breakpoints = btmp;

        return result.Glyphs;
    }

    public void EnterCharacter(char c)
    {
        Util.Assert(!IsReadOnly);
        UndoRecord u = new() { Before = _state };

        if (HasSelection)
        {
            u.Removed = GetSelectedText();
            u.RemovedStart = _state.SelectionStart;
            u.RemovedEnd = _state.SelectionEnd;
            DeleteSelection();
        } // HasSelection

        var coord = GetActualCursorCoordinates();
        u.AddedStart = coord;

        Util.Assert(_lines.Count != 0);

        if (c == '\n')
        {
            InsertLine(coord.Line + 1);
            var line = _lines[coord.Line].Glyphs;
            var newLine = _lines[coord.Line + 1].Glyphs;

            if (SyntaxHighlighter.AutoIndentation)
                for (int it = 0; it < line.Count && char.IsAscii(line[it].Char) && IsBlank(line[it].Char); ++it)
                    newLine.Add(line[it]);

            int whitespaceSize = newLine.Count;
            var cindex = GetCharacterIndex(coord);
            newLine.AddRange(line.Skip(cindex));
            line.RemoveRange(cindex, line.Count - cindex);
            CursorPosition = (coord.Line + 1, GetCharacterColumn(coord.Line + 1, whitespaceSize));
            u.Added = "\n";
        }
        else
        {
            var line = _lines[coord.Line].Glyphs;
            var cindex = GetCharacterIndex(coord);

            if (IsOverwrite && cindex < line.Count)
            {
                u.RemovedStart = _state.CursorPosition;
                u.RemovedEnd = (coord.Line, GetCharacterColumn(coord.Line, cindex + 1));

                u.Removed += line[cindex].Char;
                line.RemoveAt(cindex);
            }

            line.Insert(cindex, new Glyph(c, PaletteIndex.Default));
            u.Added = _charLabelCache.Get(c);

            CursorPosition = (coord.Line, GetCharacterColumn(coord.Line, cindex + 1));
        }

        IsTextChanged = true;

        u.AddedEnd = GetActualCursorCoordinates();
        u.After = _state;

        AddUndo(u);

        InvalidateColor(coord.Line - 1, 3);
        EnsureCursorVisible();
    }

    public void IndentSelection(bool shift)
    {
        Util.Assert(!IsReadOnly);

        UndoRecord u = new() { Before = _state };

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
            if (shift)
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
            start = (start.Line, GetCharacterColumn(start.Line, 0));
            Coordinates rangeEnd;
            if (originalEnd.Column != 0)
            {
                end = (end.Line, GetLineMaxColumn(end.Line));
                rangeEnd = end;
                u.Added = GetText(start, end);
            }
            else
            {
                end = (originalEnd.Line, 0);
                rangeEnd = (end.Line - 1, GetLineMaxColumn(end.Line - 1));
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
    }

    static bool IsBlank(char c) => c is ' ' or '\t';

    public void Backspace()
    {
        Util.Assert(!IsReadOnly);

        if (_lines.Count == 0)
            return;

        UndoRecord u = new() { Before = _state };

        if (HasSelection)
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
                u.RemovedStart = u.RemovedEnd = (pos.Line - 1, GetLineMaxColumn(pos.Line - 1));
                Advance(u.RemovedEnd);

                var line = _lines[_state.CursorPosition.Line].Glyphs;
                var prevLine = _lines[_state.CursorPosition.Line - 1].Glyphs;
                var prevSize = GetLineMaxColumn(_state.CursorPosition.Line - 1);
                prevLine.AddRange(line);

                var tempErrors = new Dictionary<int, string>();
                foreach (var kvp in _errorMarkers)
                    tempErrors[kvp.Key - 1 == _state.CursorPosition.Line ? kvp.Key - 1 : kvp.Key] = kvp.Value;
                _errorMarkers = tempErrors;

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

    string GetWordAt(Coordinates position)
    {
        var start = FindWordStart(position);
        var end = FindWordEnd(position);

        var sb = new StringBuilder();

        var istart = GetCharacterIndex(start);
        var iend = GetCharacterIndex(end);

        for (var it = istart; it < iend; ++it)
            sb.Append(_lines[position.Line].Glyphs[it].Char);

        return sb.ToString();
    }

    uint GetGlyphColor(Glyph glyph) => _palette[(int)glyph.ColorIndex];

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
                    _textStart + TextDistanceToLineStart((lineNo, GetLineMaxColumn(lineNo))),
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
                    if (!HasSelection)
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

                            if (IsOverwrite && cindex < line.Count)
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

                        if (IsShowingWhitespace)
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
                        if (IsShowingWhitespace)
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
                    var tooltip = SyntaxHighlighter.GetTooltip(id);
                    if (!string.IsNullOrEmpty(tooltip))
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
