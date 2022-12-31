using System.Numerics;
using System.Text;
using System.Text.RegularExpressions;
using ImGuiNET;

namespace ImGuiColorTextEditNet;

public class TextEditor
{
    readonly List<UndoRecord> _undoBuffer = new();
    readonly List<(Regex, PaletteIndex)> _regexList = new();
    readonly List<List<Glyph>> _lines = new();
    readonly float _lineSpacing;
    readonly int _leftMargin;
    readonly uint[] _palette = new uint[(int)PaletteIndex.Max];

    internal EditorState _state;
    int _undoIndex;

    int _tabSize;
    bool _overwrite;
    bool _withinRender;
    bool _scrollToCursor;
    bool _scrollToTop;
    bool _textChanged;
    float _textStart; // position (in pixels) where a code line starts relative to the left of the TextEditor.
    bool _cursorPositionChanged;
    int _colorRangeMin, _colorRangeMax;
    SelectionMode _selectionMode;

    LanguageDefinition _languageDefinition;

    bool _checkComments;
    HashSet<int> _breakpoints = new();
    Dictionary<int, string> _errorMarkers = new();
    Vector2 _charAdvance;
    Coordinates _interactiveStart, _interactiveEnd;
    string _lineBuffer = "";
    DateTime _startTime;

    float _lastClick;

    public TextEditor()
    {
        _lineSpacing = 1.0f;
        _undoIndex = 0;
        _tabSize = 4;
        _overwrite = false;
        IsReadOnly = false;
        _withinRender = false;
        _scrollToCursor = false;
        _scrollToTop = false;
        _textChanged = false;
        IsColorizerEnabled = true;
        _textStart = 20.0f;
        _leftMargin = 10;
        _cursorPositionChanged = false;
        _colorRangeMin = 0;
        _colorRangeMax = 0;
        _selectionMode = SelectionMode.Normal;
        _checkComments = true;
        _lastClick = -1.0f;
        IsHandleMouseInputsEnabled = true;
        IsHandleKeyboardInputsEnabled = true;
        IsImGuiChildIgnored = false;
        IsShowingWhitespaces = true;
        _startTime = DateTime.UtcNow;
        Palette = DarkPalette;
        LanguageDefinition = LanguageDefinition.HLSL();
        _lines.Add(new List<Glyph>());
    }

    public LanguageDefinition LanguageDefinition
    {
        get => _languageDefinition;
        set
        {
            _languageDefinition = value;
            _regexList.Clear();

            foreach (var r in _languageDefinition.TokenRegexStrings)
                _regexList.Add((new Regex(r.Item1, RegexOptions.Compiled), r.Item2));

            Colorize();
        }
    }

    public uint[] Palette { get; set; } = new uint[(int)PaletteIndex.Max];
    public void SetErrorMarkers(Dictionary<int, string> value) => _errorMarkers = value;
    public void SetBreakpoints(HashSet<int> value) => _breakpoints = value;

    public void Render(string aTitle, Vector2 aSize = new(), bool aBorder = false)
    {
        _withinRender = true;
        _textChanged = false;
        _cursorPositionChanged = false;

        ImGui.PushStyleColor(ImGuiCol.ChildBg, ImGui.ColorConvertU32ToFloat4(_palette[(int)PaletteIndex.Background]));
        ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(0.0f, 0.0f));
        if (!IsImGuiChildIgnored)
            ImGui.BeginChild(aTitle, aSize, aBorder, ImGuiWindowFlags.HorizontalScrollbar | ImGuiWindowFlags.AlwaysHorizontalScrollbar | ImGuiWindowFlags.NoMove);

        if (IsHandleMouseInputsEnabled)
        {
            HandleKeyboardInputs();
            ImGui.PushAllowKeyboardFocus(true);
        }

        if (IsHandleMouseInputsEnabled)
            HandleMouseInputs();

        ColorizeInternal();
        Render();

        if (IsHandleMouseInputsEnabled)
            ImGui.PopAllowKeyboardFocus();

        if (!IsImGuiChildIgnored)
            ImGui.EndChild();

        ImGui.PopStyleVar();
        ImGui.PopStyleColor();

        _withinRender = false;
    }

    public string Text
    {
        get => GetText(new Coordinates(0, 0), new Coordinates(_lines.Count, 0));
        set
        {
            _lines.Clear();
            _lines.Add(new List<Glyph>());

            foreach (var chr in value)
            {
                if (chr == '\r')
                {
                    // ignore the carriage return character
                }
                else if (chr == '\n')
                    _lines.Add(new List<Glyph>());
                else
                {
                    _lines[^1].Add(new Glyph(chr, PaletteIndex.Default));
                }
            }

            _textChanged = true;
            _scrollToTop = true;

            _undoBuffer.Clear();
            _undoIndex = 0;

            Colorize();
        }
    }

    public List<string> TextLines
    {
        get
        {
            var result = new List<string>(_lines.Count);

            foreach (var line in _lines)
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
                _lines.Add(new List<Glyph>());
            }
            else
            {
                _lines.Capacity = value.Count;
                foreach (var aLine in value)
                {
                    var line = new List<Glyph>(aLine.Length);
                    _lines.Add(line);

                    foreach (var c in aLine)
                        line.Add(new Glyph(c, PaletteIndex.Default));
                }
            }

            _textChanged = true;
            _scrollToTop = true;

            _undoBuffer.Clear();
            _undoIndex = 0;

            Colorize();
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
    public bool IsTextChanged => _textChanged;
    public bool IsCursorPositionChanged => _cursorPositionChanged;
    public bool IsColorizerEnabled { get; set; }
    public Coordinates CursorPosition
    {
        get => GetActualCursorCoordinates();
        set
        {
            if (_state.CursorPosition != value)
            {
                _state.CursorPosition = value;
                _cursorPositionChanged = true;
                EnsureCursorVisible();
            }
        }
    }

    public bool IsHandleMouseInputsEnabled { get; set; }
    public bool IsHandleKeyboardInputsEnabled { get; set; }
    public bool IsImGuiChildIgnored { get; set; }
    public bool IsShowingWhitespaces { get; set; }
    public int TabSize
    {
        get => _tabSize;
        set => _tabSize = Math.Max(0, Math.Min(32, value));
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
        Colorize(start.Line - 1, totalLines + 2);
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
                    var lineSize = lineNo < _lines.Count ? _lines[lineNo].Count : 0;
                    _state.SelectionStart = new Coordinates(_state.SelectionStart.Line, 0);
                    _state.SelectionEnd = new Coordinates(lineNo, GetLineMaxColumn(lineNo));
                    break;
                }
        }

        if (_state.SelectionStart != oldSelStart ||
            _state.SelectionEnd != oldSelEnd)
            _cursorPositionChanged = true;
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

    // https://en.wikipedia.org/wiki/UTF-8
    // We assume that the char is a standalone character (<128) or a leading byte of an UTF-8 code sequence (non-10xxxxxx code)
    static int UTF8CharLength(char c)
    {
        if ((c & 0xFE) == 0xFC) return 6;
        if ((c & 0xFC) == 0xF8) return 5;
        if ((c & 0xF8) == 0xF0) return 4;
        if ((c & 0xF0) == 0xE0) return 3;
        if ((c & 0xE0) == 0xC0) return 2;
        return 1;
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
                    if (_lines.Count > line)
                        cindex = _lines[line].Count;
                    else
                        cindex = 0;
                }
            }
            else
            {
                --cindex;
                if (cindex > 0)
                {
                    if (_lines.Count > line)
                    {
                        while (cindex > 0 && IsUTFSequence(_lines[line][cindex].Char))
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

            if (cindex >= line.Count)
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
                cindex += UTF8CharLength(line[cindex].Char);
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

                foreach (var g in line)
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
            CursorPosition=pos;
            var line = _lines[pos.Line];

            if (pos.Column == GetLineMaxColumn(pos.Line))
            {
                if (pos.Line == _lines.Count - 1)
                    return;

                u.Removed = "\n";
                u.RemovedStart = u.RemovedEnd = GetActualCursorCoordinates();
                Advance(u.RemovedEnd);

                var nextLine = _lines[pos.Line + 1];
                line.AddRange(nextLine);
                RemoveLine(pos.Line + 1);
            }
            else
            {
                var cindex = GetCharacterIndex(pos);
                u.RemovedStart = u.RemovedEnd = GetActualCursorCoordinates();
                u.RemovedEnd.Column++;
                u.Removed = GetText(u.RemovedStart, u.RemovedEnd);

                var d = UTF8CharLength(line[cindex].Char);
                while (d-- > 0 && cindex < line.Count)
                    line.RemoveAt(cindex);
            }

            _textChanged = true;

            Colorize(pos.Line, 1);
        }

        u.After = _state;
        AddUndo(u);
    }

    public bool CanUndo() => !IsReadOnly && _undoIndex > 0;
    public bool CanRedo() => !IsReadOnly && _undoIndex < _undoBuffer.Count;

    public void Undo(int aSteps = 1)
    {
        while (CanUndo() && aSteps-- > 0)
            _undoBuffer[--_undoIndex].Undo(this);
    }

    public void Redo(int aSteps = 1)
    {
        while (CanRedo() && aSteps-- > 0)
            _undoBuffer[_undoIndex++].Redo(this);
    }

    public static readonly uint[] DarkPalette = {
            0xff7f7f7f, // Default
            0xffd69c56, // Keyword
            0xff00ff00, // Number
            0xff7070e0, // String
            0xff70a0e0, // Char literal
            0xffffffff, // Punctuation
            0xff408080, // Preprocessor
            0xffaaaaaa, // Identifier
            0xff9bc64d, // Known identifier
            0xffc040a0, // Preproc identifier
            0xff206020, // Comment (single line)
            0xff406020, // Comment (_Ulti line)
            0xff101010, // Background
            0xffe0e0e0, // Cursor
            0x80a06020, // Selection
            0x800020ff, // ErrorMarker
            0x40f08000, // Breakpoint
            0xff707000, // Line number
            0x40000000, // Current line fill
            0x40808080, // Current line fill (inactive)
            0x40a0a0a0, // Current line edge
        };

    public static readonly uint[] LightPalette = {
            0xff7f7f7f, // None
            0xffff0c06, // Keyword
            0xff008000, // Number
            0xff2020a0, // String
            0xff304070, // Char literal
            0xff000000, // Punctuation
            0xff406060, // Preprocessor
            0xff404040, // Identifier
            0xff606010, // Known identifier
            0xffc040a0, // Preproc identifier
            0xff205020, // Comment (single line)
            0xff405020, // Comment (_Ulti line)
            0xffffffff, // Background
            0xff000000, // Cursor
            0x80600000, // Selection
            0xa00010ff, // ErrorMarker
            0x80f08000, // Breakpoint
            0xff505000, // Line number
            0x40000000, // Current line fill
            0x40808080, // Current line fill (inactive)
            0x40000000, // Current line edge
        };

    public static readonly uint[] RetroBluePalette = {
            0xff00ffff, // None
            0xffffff00, // Keyword
            0xff00ff00, // Number
            0xff808000, // String
            0xff808000, // Char literal
            0xffffffff, // Punctuation
            0xff008000, // Preprocessor
            0xff00ffff, // Identifier
            0xffffffff, // Known identifier
            0xffff00ff, // Preproc identifier
            0xff808080, // Comment (single line)
            0xff404040, // Comment (_Ulti line)
            0xff800000, // Background
            0xff0080ff, // Cursor
            0x80ffff00, // Selection
            0xa00000ff, // ErrorMarker
            0x80ff8000, // Breakpoint
            0xff808000, // Line number
            0x40000000, // Current line fill
            0x40808080, // Current line fill (inactive)
            0x40000000, // Current line edge
        };

    internal void Colorize(int aFromLine = 0, int aLines = -1)
    {
        int toLine = aLines == -1 ? _lines.Count : Math.Min(_lines.Count, aFromLine + aLines);
        _colorRangeMin = Math.Min(_colorRangeMin, aFromLine);
        _colorRangeMax = Math.Max(_colorRangeMax, toLine);
        _colorRangeMin = Math.Max(0, _colorRangeMin);
        _colorRangeMax = Math.Max(_colorRangeMin, _colorRangeMax);
        _checkComments = true;
    }

    void ColorizeRange(int aFromLine = 0, int aToLine = 0)
    {
        /*
        if (_lines.Count == 0 || aFromLine >= aToLine)
            return;

        string buffer;
        std::cmatch results;
        string id;

        int endLine = Math.Max(0, Math.Min(_lines.Count, aToLine));
        for (int i = aFromLine; i < endLine; ++i)
        {
            var line = _lines[i];

            if (line.Count == 0)
                continue;

            buffer.resize(line.Count);
            for (int j = 0; j < line.Count; ++j)
            {
                var col = line[j];
                buffer[j] = col._char;
                col._colorIndex = PaletteIndex.Default;
            }

            char* bufferBegin = buffer.front();
            char* bufferEnd = bufferBegin + buffer.Count;

            var last = bufferEnd;

            for (var first = bufferBegin; first != last;)
            {
                char* token_begin = null;
                char* token_end = null;
                PaletteIndex token_color = PaletteIndex.Default;

                bool hasTokenizeResult = false;

                if (_languageDefinition._tokenize != null)
                {
                    if (_languageDefinition._tokenize(first, last, token_begin, token_end, token_color))
                        hasTokenizeResult = true;
                }

                if (hasTokenizeResult == false)
                {
                    // todo : remove
                    //printf("using regex for %.*s\n", first + 10 < last ? 10 : int(last - first), first);

                    foreach (var p in _regexList)
                    {
                        if (std::regex_search(first, last, results, p.first, std::regex_constants::match_continuous))
                        {
                            hasTokenizeResult = true;

                            var v = *results.begin();
                            token_begin = v.first;
                            token_end = v.Value;
                            token_color = p.Value;
                            break;
                        }
                    }
                }

                if (hasTokenizeResult == false)
                {
                    first++;
                }
                else
                {
                    int token_length = token_end - token_begin;

                    if (token_color == PaletteIndex.Identifier)
                    {
                        id.assign(token_begin, token_end);

                        // todo : allmost all language definitions use lower case to specify keywords, so shouldn't this use ::tolower ?
                        if (!_languageDefinition._caseSensitive)
                            std::transform(id.begin(), id.end(), id.begin(), ::toupper);

                        if (!line[first - bufferBegin]._preprocessor)
                        {
                            if (_languageDefinition._keywords.count(id) != 0)
                                token_color = PaletteIndex.Keyword;
                            else if (_languageDefinition._identifiers.count(id) != 0)
                                token_color = PaletteIndex.KnownIdentifier;
                            else if (_languageDefinition._preprocIdentifiers.count(id) != 0)
                                token_color = PaletteIndex.PreprocIdentifier;
                        }
                        else
                        {
                            if (_languageDefinition._preprocIdentifiers.count(id) != 0)
                                token_color = PaletteIndex.PreprocIdentifier;
                        }
                    }

                    for (int j = 0; j < token_length; ++j)
                        line[(token_begin - bufferBegin) + j]._colorIndex = token_color;

                    first = token_end;
                }
            }
        }
        */
    }

    void ColorizeInternal()
    {
        if (_lines.Count == 0 || !IsColorizerEnabled)
            return;

        /*
        if (_checkComments)
        {
            var endLine = _lines.Count;
            var endIndex = 0;
            var commentStartLine = endLine;
            var commentStartIndex = endIndex;
            var withinString = false;
            var withinSingleLineComment = false;
            var withinPreproc = false;
            var firstChar = true;           // there is no other non-whitespace characters in the line before
            var concatenate = false;        // '\' on the very end of the line
            var currentLine = 0;
            var currentIndex = 0;
            while (currentLine < endLine || currentIndex < endIndex)
            {
                var line = _lines[currentLine];

                if (currentIndex == 0 && !concatenate)
                {
                    withinSingleLineComment = false;
                    withinPreproc = false;
                    firstChar = true;
                }

                concatenate = false;

                if (line.Count != 0)
                {
                    var g = line[currentIndex];
                    var c = g._char;

                    if (c != _languageDefinition._preprocChar && !char.IsWhiteSpace(c))
                        firstChar = false;

                    if (currentIndex == line.Count - 1 && line[^1]._char == '\\')
                        concatenate = true;

                    bool inComment = (commentStartLine < currentLine || (commentStartLine == currentLine && commentStartIndex <= currentIndex));

                    if (withinString)
                    {
                        g._multiLineComment = inComment;

                        if (c == '\"')
                        {
                            if (currentIndex + 1 < line.Count && line[currentIndex + 1]._char == '\"')
                            {
                                currentIndex += 1;
                                if (currentIndex < line.Count)
                                    g._multiLineComment = inComment;
                            }
                            else
                                withinString = false;
                        }
                        else if (c == '\\')
                        {
                            currentIndex += 1;
                            if (currentIndex < line.Count)
                                g._multiLineComment = inComment;
                        }
                    }
                    else
                    {
                        if (firstChar && c == _languageDefinition._preprocChar)
                            withinPreproc = true;

                        if (c == '\"')
                        {
                            withinString = true;
                            g._multiLineComment = inComment;
                        }
                        else
                        {
                            bool pred(char a, Glyph b) { return a == b._char; }
                            var from = line.begin() + currentIndex;
                            var startStr = _languageDefinition._commentStart;
                            var singleStartStr = _languageDefinition._singleLineComment;

                            if (singleStartStr.Count > 0 &&
                                currentIndex + singleStartStr.Count <= line.Count &&
                                equals(singleStartStr.begin(), singleStartStr.end(), from, from + singleStartStr.Count, pred))
                            {
                                withinSingleLineComment = true;
                            }
                            else if (!withinSingleLineComment && currentIndex + startStr.Count <= line.Count &&
                                equals(startStr.begin(), startStr.end(), from, from + startStr.Count, pred))
                            {
                                commentStartLine = currentLine;
                                commentStartIndex = currentIndex;
                            }

                            inComment = inComment = (commentStartLine < currentLine || (commentStartLine == currentLine && commentStartIndex <= currentIndex));

                            g._multiLineComment = inComment;
                            g._comment = withinSingleLineComment;

                            var endStr = _languageDefinition._commentEnd;
                            if (currentIndex + 1 >= (int)endStr.Count &&
                                equals(endStr.begin(), endStr.end(), from + 1 - endStr.Count, from + 1, pred))
                            {
                                commentStartIndex = endIndex;
                                commentStartLine = endLine;
                            }
                        }
                    }

                    g._preprocessor = withinPreproc;
                    line[currentIndex] = g;

                    currentIndex += UTF8CharLength(c);
                    if (currentIndex >= line.Count)
                    {
                        currentIndex = 0;
                        ++currentLine;
                    }
                }
                else
                {
                    currentIndex = 0;
                    ++currentLine;
                }
            }
            _checkComments = false;
        }
        */

        if (_colorRangeMin < _colorRangeMax)
        {
            int increment = (_languageDefinition.Tokenize == null) ? 10 : 10000;
            int to = Math.Min(_colorRangeMin + increment, _colorRangeMax);
            ColorizeRange(_colorRangeMin, to);
            _colorRangeMin = to;

            if (_colorRangeMax == _colorRangeMin)
            {
                _colorRangeMin = int.MaxValue;
                _colorRangeMax = 0;
            }
            return;
        }
    }

    float TextDistanceToLineStart(Coordinates aFrom)
    {
        var line = _lines[aFrom.Line];
        float distance = 0.0f;
        float spaceSize = ImGui.CalcTextSize(" ", false, -1.0f).X; // remaining

        int colIndex = GetCharacterIndex(aFrom);
        for (int it = 0; it < line.Count && it < colIndex;)
        {
            if (line[it].Char == '\t')
            {
                distance = (1.0f + MathF.Floor((1.0f + distance) / (_tabSize * spaceSize))) * (_tabSize * spaceSize);
                ++it;
            }
            else
            {
                var d = UTF8CharLength(line[it].Char);
                var sb = new StringBuilder(7);
                for (int i = 0; i < 6 && d-- > 0 && it < line.Count; i++, it++)
                    sb.Append(line[it].Char);

                distance += ImGui.CalcTextSize(sb.ToString(), false, -1.0f).X;
            }
        }

        return distance;
    }

    internal void EnsureCursorVisible()
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
            s += _lines[i].Count;

        var result = new StringBuilder(s + s / 8);
        while (istart < iend || lstart < lend)
        {
            if (lstart >= _lines.Count)
                break;

            var line = _lines[lstart];
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
            var line = _lines[aCoordinates.Line];
            var cindex = GetCharacterIndex(aCoordinates);

            if (cindex + 1 < line.Count)
            {
                var delta = UTF8CharLength(line[cindex].Char);
                cindex = Math.Min(cindex + delta, line.Count - 1);
            }
            else
            {
                ++aCoordinates.Line;
                cindex = 0;
            }
            aCoordinates.Column = GetCharacterColumn(aCoordinates.Line, cindex);
        }
    }

    internal void DeleteRange(Coordinates aStart, Coordinates aEnd)
    {
        Util.Assert(aEnd >= aStart);
        Util.Assert(!IsReadOnly);

        //printf("D(%d.%d)-(%d.%d)\n", aStart._line, aStart._column, aEnd._line, aEnd._column);

        if (aEnd == aStart)
            return;

        var start = GetCharacterIndex(aStart);
        var end = GetCharacterIndex(aEnd);

        if (aStart.Line == aEnd.Line)
        {
            var line = _lines[aStart.Line];
            var n = GetLineMaxColumn(aStart.Line);
            if (aEnd.Column >= n)
                line.RemoveRange(start, line.Count - start);
            else
                line.RemoveRange(start, end - start);
        }
        else
        {
            var firstLine = _lines[aStart.Line];
            var lastLine = _lines[aEnd.Line];

            firstLine.RemoveRange(start, firstLine.Count - start);
            lastLine.RemoveRange(0, end);

            if (aStart.Line < aEnd.Line)
                firstLine.AddRange(lastLine);

            if (aStart.Line < aEnd.Line)
                RemoveLine(aStart.Line + 1, aEnd.Line + 1);
        }

        _textChanged = true;
    }

    internal int InsertTextAt(Coordinates aWhere, string aValue)
    {
        Util.Assert(!IsReadOnly);

        int cindex = GetCharacterIndex(aWhere);
        int totalLines = 0;
        for (var index = 0; index < aValue.Length; index++)
        {
            var c = aValue[index];
            Util.Assert(_lines.Count != 0);

            if (c == '\r')
                continue;

            if (c == '\n')
            {
                if (cindex < _lines[aWhere.Line].Count)
                {
                    var newLine = InsertLine(aWhere.Line + 1);
                    var line = _lines[aWhere.Line];
                    newLine.InsertRange(0, line.Skip(cindex));
                    line.RemoveRange(cindex, line.Count - cindex);
                }
                else
                {
                    InsertLine(aWhere.Line + 1);
                }

                ++aWhere.Line;
                aWhere.Column = 0;
                cindex = 0;
                ++totalLines;
            }
            else
            {
                var line = _lines[aWhere.Line];
                var d = UTF8CharLength(c);
                while (d-- > 0 && index < aValue.Length)
                {
                    line.Insert(cindex++, new Glyph(aValue[index], PaletteIndex.Default));
                    index++;
                }

                ++aWhere.Column;
            }

            _textChanged = true;
        }

        return totalLines;
    }

    void AddUndo(UndoRecord aValue)
    {
        Util.Assert(!IsReadOnly);
        // printf("AddUndo: (@%d.%d) +\'%s' [%d.%d .. %d.%d], -\'%s', [%d.%d .. %d.%d] (@%d.%d)\n",
        //     aValue._before._cursorPosition._line, aValue._before._cursorPosition._column,
        //     aValue._added, aValue._addedStart._line, aValue._addedStart._column, aValue._addedEnd._line, aValue._addedEnd._column,
        //     aValue._removed, aValue._removedStart._line, aValue._removedStart._column, aValue._removedEnd._line, aValue._removedEnd._column,
        //     aValue._after._cursorPosition._line, aValue._after._cursorPosition._column
        // );

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
            var line = _lines[lineNo];

            int columnIndex = 0;
            float columnX = 0.0f;

            while (columnIndex < line.Count)
            {
                float columnWidth;

                if (line[columnIndex].Char == '\t')
                {
                    float spaceSize = ImGui.CalcTextSize(" ", false, -1.0f).X;
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
                    var sb = new StringBuilder(7);
                    var d = UTF8CharLength(line[columnIndex].Char);
                    while (sb.Length < 6 && d-- > 0)
                        sb.Append(line[columnIndex++].Char);

                    columnWidth = ImGui.CalcTextSize(sb.ToString(), -1.0f).X;
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

        var line = _lines[aFrom.Line];
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
                if (cstart != line[(int)(cindex - 1)].ColorIndex)
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

        var line = _lines[at.Line];
        var cindex = GetCharacterIndex(at);

        if (cindex >= line.Count)
            return at;

        bool prevspace = char.IsWhiteSpace(line[cindex].Char);
        var cstart = line[cindex].ColorIndex;
        while (cindex < line.Count)
        {
            var c = line[cindex].Char;
            var d = UTF8CharLength(c);
            if (cstart != line[cindex].ColorIndex)
                break;

            if (prevspace != char.IsWhiteSpace(c))
            {
                if (char.IsWhiteSpace(c))
                    while (cindex < line.Count && char.IsWhiteSpace(line[cindex].Char))
                        ++cindex;
                break;
            }
            cindex += d;
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
        if (cindex < _lines[at.Line].Count)
        {
            var line = _lines[at.Line];
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

            var line = _lines[at.Line];
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
        var line = _lines[aCoordinates.Line];
        int c = 0;
        int i = 0;
        for (; i < line.Count && c < aCoordinates.Column;)
        {
            if (line[i].Char == '\t')
                c = (c / _tabSize) * _tabSize + _tabSize;
            else
                ++c;
            i += UTF8CharLength(line[i].Char);
        }
        return i;
    }

    int GetCharacterColumn(int aLine, int aIndex)
    {
        if (aLine >= _lines.Count)
            return 0;
        var line = _lines[aLine];
        int col = 0;
        int i = 0;
        while (i < aIndex && i < line.Count)
        {
            var c = line[i].Char;
            i += UTF8CharLength(c);
            if (c == '\t')
                col = (col / _tabSize) * _tabSize + _tabSize;
            else
                col++;
        }
        return col;
    }

    int GetLineCharacterCount(int aLine)
    {
        if (aLine >= _lines.Count)
            return 0;
        var line = _lines[aLine];
        int c = 0;
        for (int i = 0; i < line.Count; c++)
            i += UTF8CharLength(line[i].Char);
        return c;
    }

    int GetLineMaxColumn(int aLine)
    {
        if (aLine >= _lines.Count)
            return 0;
        var line = _lines[aLine];
        int col = 0;
        for (int i = 0; i < line.Count;)
        {
            var c = line[i].Char;
            if (c == '\t')
                col = (col / _tabSize) * _tabSize + _tabSize;
            else
                col++;
            i += UTF8CharLength(c);
        }
        return col;
    }

    bool IsOnWordBoundary(Coordinates aAt)
    {
        if (aAt.Line >= _lines.Count || aAt.Column == 0)
            return true;

        var line = _lines[aAt.Line];
        var cindex = GetCharacterIndex(aAt);
        if (cindex >= line.Count)
            return true;

        if (IsColorizerEnabled)
            return line[cindex].ColorIndex != line[(int)(cindex - 1)].ColorIndex;

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

        _textChanged = true;
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

        _textChanged = true;
    }

    List<Glyph> InsertLine(int aIndex)
    {
        Util.Assert(!IsReadOnly);

        var result = new List<Glyph>();
        _lines.Insert(aIndex, result);

        Dictionary<int, string> etmp = new();
        foreach (var i in _errorMarkers)
            etmp[i.Key >= aIndex ? i.Key + 1 : i.Key] = i.Value;
        _errorMarkers = etmp;

        HashSet<int> btmp = new();
        foreach (var i in _breakpoints)
            btmp.Add(i >= aIndex ? i + 1 : i);
        _breakpoints = btmp;

        return result;
    }

    void EnterCharacter(ushort aChar, bool aShift)
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

                bool _Odified = false;

                for (int i = start.Line; i <= end.Line; i++)
                {
                    var line = _lines[i];
                    if (aShift)
                    {
                        if (line.Count != 0)
                        {
                            if (line[0].Char == '\t')
                            {
                                line.RemoveAt(0);
                                _Odified = true;
                            }
                            else
                            {
                                for (int j = 0; j < _tabSize && line.Count != 0 && line[0].Char == ' '; j++)
                                {
                                    line.RemoveAt(0);
                                    _Odified = true;
                                }
                            }
                        }
                    }
                    else
                    {
                        line.Insert(0, new Glyph('\t', PaletteIndex.Background));
                        _Odified = true;
                    }
                }

                if (_Odified)
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

                    _textChanged = true;

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
            var line = _lines[coord.Line];
            var newLine = _lines[coord.Line + 1];

            if (_languageDefinition.AutoIndentation)
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
            var buf = new char[7];
            int e = ImTextCharToUtf8(buf, 7, aChar);
            if (e > 0)
            {
                buf[e] = '\0';
                var line = _lines[coord.Line];
                var cindex = GetCharacterIndex(coord);

                if (_overwrite && cindex < line.Count)
                {
                    var d = UTF8CharLength(line[cindex].Char);

                    u.RemovedStart = _state.CursorPosition;
                    u.RemovedEnd = new Coordinates(coord.Line, GetCharacterColumn(coord.Line, cindex + d));

                    while (d-- > 0 && cindex < line.Count)
                    {
                        u.Removed += line[cindex].Char;
                        line.RemoveAt(cindex);
                    }
                }

                var sb = new StringBuilder();
                for (var i = 0; i < buf.Length; i++, ++cindex)
                {
                    var c = buf[i];
                    if (c == '\0')
                        break;

                    line.Insert(cindex, new Glyph(c, PaletteIndex.Default));
                    sb.Append(c);
                }

                u.Added = sb.ToString();

                CursorPosition = new Coordinates(coord.Line, GetCharacterColumn(coord.Line, cindex));
            }
            else
                return;
        }

        _textChanged = true;

        u.AddedEnd = GetActualCursorCoordinates();
        u.After = _state;

        AddUndo(u);

        Colorize(coord.Line - 1, 3);
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

                var line = _lines[_state.CursorPosition.Line];
                var prevLine = _lines[_state.CursorPosition.Line - 1];
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
                var line = _lines[_state.CursorPosition.Line];
                var cindex = GetCharacterIndex(pos) - 1;
                var cend = cindex + 1;
                while (cindex > 0 && IsUTFSequence(line[cindex].Char))
                    --cindex;

                //if (cindex > 0 && UTF8CharLength(line[cindex]._char) > 1)
                //    --cindex;

                u.RemovedStart = u.RemovedEnd = GetActualCursorCoordinates();
                --u.RemovedStart.Column;
                --_state.CursorPosition.Column;

                while (cindex < line.Count && cend-- > cindex)
                {
                    u.Removed += line[cindex].Char;
                    line.RemoveAt(cindex);
                }
            }

            _textChanged = true;

            EnsureCursorVisible();
            Colorize(_state.CursorPosition.Line, 1);
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
        Colorize(_state.SelectionStart.Line, 1);
    }

    string GetWordUnderCursor() => GetWordAt(CursorPosition);
    string GetWordAt(Coordinates aCoords)
    {
        var start = FindWordStart(aCoords);
        var end = FindWordEnd(aCoords);

        var sb = new StringBuilder();

        var istart = GetCharacterIndex(start);
        var iend = GetCharacterIndex(end);

        for (var it = istart; it < iend; ++it)
            sb.Append(_lines[aCoords.Line][it].Char);

        return sb.ToString();
    }

    uint GetGlyphColor(Glyph aGlyph)
    {
        if (!IsColorizerEnabled)
            return _palette[(int)PaletteIndex.Default];

        if (aGlyph.Comment)
            return _palette[(int)PaletteIndex.Comment];

        if (aGlyph.MultiLineComment)
            return _palette[(int)PaletteIndex.MultiLineComment];

        var color = _palette[(int)aGlyph.ColorIndex];
        if (aGlyph.Preprocessor)
        {
            uint ppcolor = _palette[(int)PaletteIndex.Preprocessor];
            uint c0 = ((ppcolor & 0xff) + (color & 0xff)) / 2;
            uint c1 = (((ppcolor >> 8) & 0xff) + ((color >> 8) & 0xff)) / 2;
            uint c2 = (((ppcolor >> 16) & 0xff) + ((color >> 16) & 0xff)) / 2;
            uint c3 = (((ppcolor >> 24) & 0xff) + ((color >> 24) & 0xff)) / 2;
            return (c0 | (c1 << 8) | (c2 << 16) | (c3 << 24));
        }

        return color;
    }

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
                    if (c != 0 && (c == '\n' || c >= 32))
                        EnterCharacter(c, shift);
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
        var tripleClick = click && !doubleClick && (_lastClick != -1.0f && (t - _lastClick) < io.MouseDoubleClickTime);

        /* Left _Ouse button triple click */
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

        /* Left _Ouse button double click */
        else if (doubleClick)
        {
            if (!ctrl)
            {
                _state.CursorPosition = _interactiveStart = _interactiveEnd = ScreenPosToCoordinates(ImGui.GetMousePos());
                if (_selectionMode == SelectionMode.Line)
                    _selectionMode = SelectionMode.Normal;
                else
                    _selectionMode = SelectionMode.Word;
                SetSelection(_interactiveStart, _interactiveEnd, _selectionMode);
            }

            _lastClick = (float)ImGui.GetTime();
        }

        /* Left _Ouse button click */
        else if (click)
        {
            _state.CursorPosition = _interactiveStart = _interactiveEnd = ScreenPosToCoordinates(ImGui.GetMousePos());
            if (ctrl)
                _selectionMode = SelectionMode.Word;
            else
                _selectionMode = SelectionMode.Normal;
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
        /* Compute _charAdvance regarding to scaled font size (Ctrl + _Ouse wheel)*/
        float fontSize = ImGui.CalcTextSize("#", false, -1.0f).X;
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
        var buf = $" {globalLineMax} ";
        _textStart = ImGui.CalcTextSize(buf, false, -1.0f).X + _leftMargin;

        if (_lines.Count != 0)
        {
            float spaceSize = ImGui.CalcTextSize(" ", false, -1.0f).X;

            while (lineNo <= lineMax)
            {
                Vector2 lineStartScreenPos = new Vector2(cursorScreenPos.X, cursorScreenPos.Y + lineNo * _charAdvance.Y);
                Vector2 textScreenPos = new Vector2(lineStartScreenPos.X + _textStart, lineStartScreenPos.Y);

                var line = _lines[lineNo];
                longest = Math.Max(_textStart + TextDistanceToLineStart(new Coordinates(lineNo, GetLineMaxColumn(lineNo))), longest);
                var columnNo = 0;
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
                    Vector2 vstart = new(lineStartScreenPos.X + _textStart + sstart, lineStartScreenPos.Y);
                    Vector2 vend = new(lineStartScreenPos.X + _textStart + ssend, lineStartScreenPos.Y + _charAdvance.Y);
                    drawList.AddRectFilled(vstart, vend, _palette[(int)PaletteIndex.Selection]);
                }

                // Draw breakpoints
                var start = new Vector2(lineStartScreenPos.X + scrollX, lineStartScreenPos.Y);

                if (_breakpoints.Contains(lineNo + 1))
                {
                    var end = new Vector2(lineStartScreenPos.X + contentSize.X + 2.0f * scrollX, lineStartScreenPos.Y + _charAdvance.Y);
                    drawList.AddRectFilled(start, end, _palette[(int)PaletteIndex.Breakpoint]);
                }

                // Draw error _Arkers
                if (_errorMarkers.TryGetValue(lineNo + 1, out var error))
                {
                    var end = new Vector2(lineStartScreenPos.X + contentSize.X + 2.0f * scrollX, lineStartScreenPos.Y + _charAdvance.Y);
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
                buf = $"{lineNo + 1}  ";

                var lineNoWidth = ImGui.CalcTextSize(buf, false, -1.0f).X;
                drawList.AddText(new Vector2(lineStartScreenPos.X + _textStart - lineNoWidth, lineStartScreenPos.Y), _palette[(int)PaletteIndex.LineNumber], buf);

                if (_state.CursorPosition.Line == lineNo)
                {
                    var focused = ImGui.IsWindowFocused();

                    // Highlight the current line (where the cursor is)
                    if (!HasSelection())
                    {
                        var end = new Vector2(start.X + contentSize.X + scrollX, start.Y + _charAdvance.Y);
                        drawList.AddRectFilled(start, end, _palette[(int)(focused ? PaletteIndex.CurrentLineFill : PaletteIndex.CurrentLineFillInactive)]);
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
                                    var buf2 = line[cindex].Char.ToString();
                                    width = ImGui.CalcTextSize(buf2, false, -1.0f).X;
                                }
                            }
                            Vector2 cstart = new(textScreenPos.X + cx, lineStartScreenPos.Y);
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
                        var textSize = ImGui.CalcTextSize(_lineBuffer, false, -1.0f);
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
                        var l = UTF8CharLength(glyph.Char);
                        while (l-- > 0)
                            _lineBuffer += line[i++].Char;
                    }
                    ++columnNo;
                }

                if (_lineBuffer.Length != 0)
                {
                    Vector2 newOffset = new(textScreenPos.X + bufferOffset.X, textScreenPos.Y + bufferOffset.Y);
                    drawList.AddText(newOffset, prevColor, _lineBuffer);
                    _lineBuffer = "";
                }

                ++lineNo;
            }

            // Draw a tooltip on known identifiers/preprocessor symbols
            if (ImGui.IsMousePosValid())
            {
                var id = GetWordAt(ScreenPosToCoordinates(ImGui.GetMousePos()));
                if (id.Length != 0)
                {
                    if (_languageDefinition.Identifiers.TryGetValue(id, out var identifier))
                    {
                        ImGui.BeginTooltip();
                        ImGui.TextUnformatted(identifier.Declaration);
                        ImGui.EndTooltip();
                    }
                    else
                    {
                        if (_languageDefinition.PreprocIdentifiers.TryGetValue(id, out identifier))
                        {
                            ImGui.BeginTooltip();
                            ImGui.TextUnformatted(identifier.Declaration);
                            ImGui.EndTooltip();
                        }
                    }
                }
            }
        }

        ImGui.Dummy(new Vector2((longest + 2), _lines.Count * _charAdvance.Y));

        if (_scrollToCursor)
        {
            EnsureCursorVisible();
            ImGui.SetWindowFocus();
            _scrollToCursor = false;
        }
    }

    // "Borrowed" from ImGui source
    static int ImTextCharToUtf8(Span<char> buf, int buf_size, uint c)
    {
        if (c < 0x80)
        {
            buf[0] = (char)c;
            return 1;
        }
        if (c < 0x800)
        {
            if (buf_size < 2) return 0;
            buf[0] = (char)(0xc0 + (c >> 6));
            buf[1] = (char)(0x80 + (c & 0x3f));
            return 2;
        }
        if (c is >= 0xdc00 and < 0xe000)
        {
            return 0;
        }
        if (c is >= 0xd800 and < 0xdc00)
        {
            if (buf_size < 4) return 0;
            buf[0] = (char)(0xf0 + (c >> 18));
            buf[1] = (char)(0x80 + ((c >> 12) & 0x3f));
            buf[2] = (char)(0x80 + ((c >> 6) & 0x3f));
            buf[3] = (char)(0x80 + ((c) & 0x3f));
            return 4;
        }
        //else if (c < 0x10000)
        {
            if (buf_size < 3) return 0;
            buf[0] = (char)(0xe0 + (c >> 12));
            buf[1] = (char)(0x80 + ((c >> 6) & 0x3f));
            buf[2] = (char)(0x80 + ((c) & 0x3f));
            return 3;
        }
    }
}
