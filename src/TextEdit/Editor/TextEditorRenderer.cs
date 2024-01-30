using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using ImGuiNET;

namespace ImGuiColorTextEditNet.Editor;

public class TextEditorRenderer
{
    const float LineSpacing = 1.0f;
    const int LeftMargin = 10;
    static readonly Vector4 MagentaVec4 = new(1.0f, 1.0f, 1.0f, 1.0f);
    static readonly uint MagentaUInt = 0xff00ffff;

    // Note: if fonts / sizes can ever be changed the char width cache will need to be invalidated.
    readonly SimpleCache<char, float> _charWidthCache = new("char widths", x => ImGui.CalcTextSize(x.ToString()).X);
    readonly SimpleCache<int, string> _lineNumberCache = new("line numbers", x => $"{x} ");
    readonly TextEditorSelection _selection;
    readonly TextEditorText _text;
    readonly TextEditorColor _color;
    readonly TextEditorOptions _options;
    readonly TextEditorBreakpoints _breakpoints;
    readonly TextEditorErrorMarkers _errorMarkers;
    readonly StringBuilder _lineBuffer = new();
    readonly List<uint> _palette = new();

    Vector2 _charAdvance;
    DateTime _startTime = DateTime.UtcNow;
    float _textStart = 20.0f; // position (in pixels) where a code line starts relative to the left of the TextEditor.
    uint[]? _uintPalette;
    Vector4[]? _vec4Palette;
    bool _paletteDirty;
    float _lastAlpha;

    public uint[] Palette
    {
        get => _palette.ToArray();
        set
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));

            _palette.Clear();
            _palette.AddRange(value);
            _paletteDirty = true;
        }
    }

    public void SetColor(PaletteIndex color, uint abgr)
    {
        int index = (int)color;
        while (_palette.Count <= index)
            _palette.Add(MagentaUInt);

        _palette[index] = abgr;
        _paletteDirty = true;
    }

    public int PageSize
    {
        get
        {
            var height = ImGui.GetWindowHeight() - 20.0f;
            return (int)MathF.Floor(height / _charAdvance.Y);
        }
    }

    public ITextEditorKeyboardInput? KeyboardInput { get; init; }
    public ITextEditorMouseInput? MouseInput { get; init; }
    public bool IsImGuiChildIgnored { get; set; }
    public bool IsHandleMouseInputsEnabled { get; set; } = true;
    public bool IsHandleKeyboardInputsEnabled { get; set; } = true;
    public bool IsShowingWhitespace { get; set; } = true;

    internal TextEditorRenderer(TextEditor editor, uint[] palette)
    {
        if (editor == null) throw new ArgumentNullException(nameof(editor));
        _selection = editor.Selection;
        _text = editor.Text;
        _breakpoints = editor.Breakpoints;
        _errorMarkers = editor.ErrorMarkers;
        _options = editor.Options;
        _color = editor.Color;
        Palette = palette;
    }

    uint ColorUInt(PaletteIndex index) =>
        _uintPalette == null || (int)index >= _uintPalette.Length 
            ? MagentaUInt
            : _uintPalette[(int)index];

    Vector4 ColorVec(PaletteIndex index) =>
        _vec4Palette == null || (int)index > _vec4Palette.Length 
            ? MagentaVec4 
            : _vec4Palette[(int)index];


    internal void Render(string title, Vector2 size)
    {
        var background = _vec4Palette == null
            ? ImGui.ColorConvertU32ToFloat4(_palette[(int)PaletteIndex.Background])
            : ColorVec(PaletteIndex.Background);

        ImGui.PushStyleColor(ImGuiCol.ChildBg, background);
        ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(0.0f, 0.0f));

        if (!IsImGuiChildIgnored)
        {
            ImGui.BeginChild(title, size, 
                ImGuiChildFlags.None,
                ImGuiWindowFlags.HorizontalScrollbar
                | ImGuiWindowFlags.AlwaysHorizontalScrollbar
                | ImGuiWindowFlags.NoMove);
        }

        if (IsHandleKeyboardInputsEnabled && KeyboardInput != null)
            KeyboardInput.HandleKeyboardInputs();

        if (IsHandleMouseInputsEnabled && MouseInput != null)
            MouseInput.HandleMouseInputs();

        _color.ColorizeIncremental();
        RenderInner();

        if (_text.PendingScrollRequest != null)
        {
            if (_text.PendingScrollRequest.Value < _text.LineCount)
                EnsurePositionVisible(new Coordinates(_text.PendingScrollRequest.Value, 0));
            ImGui.SetWindowFocus();
            _text.PendingScrollRequest = null;
        }

        if (!IsImGuiChildIgnored)
            ImGui.EndChild();

        ImGui.PopStyleVar();
        ImGui.PopStyleColor();
    }

    void RenderInner()
    {
        /* Compute _charAdvance regarding to scaled font size (Ctrl + mouse wheel)*/
        float fontSize = _charWidthCache.Get('#');
        _charAdvance = new Vector2(fontSize, ImGui.GetTextLineHeightWithSpacing() * LineSpacing);

        var alpha = ImGui.GetStyle().Alpha;
        if (MathF.Abs(_lastAlpha - alpha) > float.Epsilon)
        {
            _paletteDirty = true;
            _lastAlpha = alpha;
        }

        /* Update palette with the current alpha from style */
        if (_paletteDirty)
        {
            _uintPalette = _palette.ToArray();
            _vec4Palette = new Vector4[_palette.Count];

            for (int i = 0; i < Palette.Length; ++i)
            {
                var color = ImGui.ColorConvertU32ToFloat4(_palette[i]);
                color.W *= alpha;
                _vec4Palette[i] = color;
                _uintPalette[i] = ImGui.ColorConvertFloat4ToU32(color);
            }

            _paletteDirty = false;
        }

        Util.Assert(_lineBuffer.Length == 0);

        var contentSize = ImGui.GetWindowContentRegionMax();
        var drawList = ImGui.GetWindowDrawList();
        float longest = _textStart;

        Vector2 cursorScreenPos = ImGui.GetCursorScreenPos();
        var scrollX = ImGui.GetScrollX();
        var scrollY = ImGui.GetScrollY();

        var lineNo = (int)MathF.Floor(scrollY / _charAdvance.Y);
        var globalLineMax = _text.LineCount;
        var lineMax = Math.Max(0, Math.Min(globalLineMax - 1, lineNo + (int)MathF.Floor((scrollY + contentSize.Y) / _charAdvance.Y)));

        // Deduce _textStart by evaluating _lines size (global lineMax) plus two spaces as text width
        float spaceSize = _charWidthCache.Get(' ');
        var buf = _lineNumberCache.Get(globalLineMax);
        _textStart = ImGui.CalcTextSize(buf).X + LeftMargin + spaceSize;

        if (globalLineMax != 0)
        {
            while (lineNo <= lineMax)
            {
                Vector2 lineStartScreenPos = cursorScreenPos with { Y = cursorScreenPos.Y + lineNo * _charAdvance.Y };
                Vector2 textScreenPos = lineStartScreenPos with { X = lineStartScreenPos.X + _textStart };

                var line = _text.GetLine(lineNo);
                longest = Math.Max(
                    _textStart + TextDistanceToLineStart((lineNo, _text.GetLineMaxColumn(lineNo))),
                    longest);

                Coordinates lineStartCoord = new(lineNo, 0);
                Coordinates lineEndCoord = new(lineNo, _text.GetLineMaxColumn(lineNo));

                // Draw selection for the current line
                float sstart = -1.0f;
                float ssend = -1.0f;

                Util.Assert(_selection.Start <= _selection.End);
                if (_selection.Start <= lineEndCoord)
                    sstart = _selection.Start > lineStartCoord ? TextDistanceToLineStart(_selection.Start) : 0.0f;
                if (_selection.End > lineStartCoord)
                    ssend = TextDistanceToLineStart(_selection.End < lineEndCoord ? _selection.End : lineEndCoord);

                if (_selection.End.Line > lineNo)
                    ssend += _charAdvance.X;

                if (sstart != -1 && ssend != -1 && sstart < ssend)
                {
                    Vector2 vstart = lineStartScreenPos with { X = lineStartScreenPos.X + _textStart + sstart };
                    Vector2 vend = new(lineStartScreenPos.X + _textStart + ssend, lineStartScreenPos.Y + _charAdvance.Y);
                    drawList.AddRectFilled(vstart, vend, ColorUInt(PaletteIndex.Selection));
                }

                // Draw breakpoints
                var start = lineStartScreenPos with { X = lineStartScreenPos.X + scrollX };

                if (_breakpoints.IsLineBreakpoint(lineNo + 1))
                {
                    var end = new Vector2(
                        lineStartScreenPos.X + contentSize.X + 2.0f * scrollX,
                        lineStartScreenPos.Y + _charAdvance.Y);

                    drawList.AddRectFilled(start, end, ColorUInt(PaletteIndex.Breakpoint));
                }

                if (lineNo == _selection.HighlightedLine)
                {
                    var end = new Vector2(
                        lineStartScreenPos.X + contentSize.X + 2.0f * scrollX,
                        lineStartScreenPos.Y + _charAdvance.Y);

                    var color = ColorUInt(PaletteIndex.ExecutingLine);
                    drawList.AddRectFilled(start, end, color);
                }

                // Draw error markers
                if (_errorMarkers.TryGetErrorForLine(lineNo + 1, out var error))
                {
                    var end = new Vector2(
                        lineStartScreenPos.X + contentSize.X + 2.0f * scrollX,
                        lineStartScreenPos.Y + _charAdvance.Y);

                    drawList.AddRectFilled(start, end, ColorUInt(PaletteIndex.ErrorMarker));

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
                    ColorUInt(PaletteIndex.LineNumber),
                    buf);

                if (_selection.Cursor.Line == lineNo)
                {
                    var focused = ImGui.IsWindowFocused();

                    // Highlight the current line (where the cursor is)
                    if (!_selection.HasSelection)
                    {
                        var end = new Vector2(start.X + contentSize.X + scrollX, start.Y + _charAdvance.Y);
                        drawList.AddRectFilled(
                            start,
                            end,
                            ColorUInt((focused ? PaletteIndex.CurrentLineFill : PaletteIndex.CurrentLineFillInactive)));

                        drawList.AddRect(start, end, ColorUInt(PaletteIndex.CurrentLineEdge), 1.0f);
                    }

                    // Render the cursor
                    if (focused)
                    {
                        var timeEnd = DateTime.UtcNow;
                        var elapsed = timeEnd - _startTime;
                        if (elapsed.Milliseconds > 400)
                        {
                            float width = 1.0f;
                            var cindex = _text.GetCharacterIndex(_selection.Cursor);
                            float cx = TextDistanceToLineStart(_selection.Cursor);

                            if (_options.IsOverwrite && cindex < line.Length)
                            {
                                var c = line[cindex].Char;
                                if (c == '\t')
                                {
                                    var x = (1.0f + MathF.Floor((1.0f + cx) / (_text.TabSize * spaceSize))) * (_text.TabSize * spaceSize);
                                    width = x - cx;
                                }
                                else
                                {
                                    width = _charWidthCache.Get(line[cindex].Char);
                                }
                            }
                            Vector2 cstart = lineStartScreenPos with { X = textScreenPos.X + cx };
                            Vector2 cend = new(textScreenPos.X + cx + width, lineStartScreenPos.Y + _charAdvance.Y);
                            drawList.AddRectFilled(cstart, cend, ColorUInt(PaletteIndex.Cursor));
                            if (elapsed.Milliseconds > 800)
                                _startTime = timeEnd;
                        }
                    }
                }

                // Render colorized text
                var prevColor = line.Length == 0 ? ColorUInt(PaletteIndex.Default) : ColorUInt(line[0].ColorIndex);
                var bufferOffset = new Vector2();

                for (int i = 0; i < line.Length;)
                {
                    var glyph = line[i];
                    var color = ColorUInt(glyph.ColorIndex);

                    if ((color != prevColor || glyph.Char is '\t' or ' ') && _lineBuffer.Length != 0)
                    {
                        Vector2 newOffset = new(textScreenPos.X + bufferOffset.X, textScreenPos.Y + bufferOffset.Y);
                        var lineText = _lineBuffer.ToString();
                        drawList.AddText(newOffset, prevColor, lineText);
                        var textSize = ImGui.CalcTextSize(lineText);
                        bufferOffset.X += textSize.X;
                        _lineBuffer.Clear();
                    }
                    prevColor = color;

                    if (glyph.Char == '\t')
                    {
                        var oldX = bufferOffset.X;
                        bufferOffset.X = (1.0f + MathF.Floor((1.0f + bufferOffset.X) / (_text.TabSize * spaceSize))) * (_text.TabSize * spaceSize);
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
                        _lineBuffer.Append(line[i++].Char);
                    }
                }

                if (_lineBuffer.Length != 0)
                {
                    Vector2 newOffset = new(textScreenPos.X + bufferOffset.X, textScreenPos.Y + bufferOffset.Y);
                    drawList.AddText(newOffset, prevColor, _lineBuffer.ToString());
                    _lineBuffer.Clear();
                }

                ++lineNo;
            }

            if (ImGui.IsMousePosValid())
            {
                var id = _text.GetWordAt(ScreenPosToCoordinates(ImGui.GetMousePos()));
                if (id.Length != 0)
                {
                    var tooltip = _color.SyntaxHighlighter.GetTooltip(id);
                    if (!string.IsNullOrEmpty(tooltip))
                    {
                        ImGui.BeginTooltip();
                        ImGui.TextUnformatted(tooltip);
                        ImGui.EndTooltip();
                    }
                }
            }
        }

        ImGui.Dummy(new Vector2(longest + 2, globalLineMax * _charAdvance.Y));
    }

    float TextDistanceToLineStart(Coordinates position)
    {
        var line = _text.GetLine(position.Line);
        float distance = 0.0f;
        float spaceSize = _charWidthCache.Get(' '); // remaining

        int colIndex = _text.GetCharacterIndex(position);
        for (int i = 0; i < line.Length && i < colIndex;)
        {
            var c = line[i].Char;
            distance =
                c == '\t'
                    ? (1.0f + MathF.Floor((1.0f + distance) / (_text.TabSize * spaceSize))) * (_text.TabSize * spaceSize)
                    : distance + _charWidthCache.Get(c);

            i++;
        }

        return distance;
    }

    void EnsurePositionVisible(Coordinates pos)
    {
        float scrollX = ImGui.GetScrollX();
        float scrollY = ImGui.GetScrollY();

        var height = ImGui.GetWindowHeight();
        var width = ImGui.GetWindowWidth();

        var top = 1 + (int)MathF.Ceiling(scrollY / _charAdvance.Y);
        var bottom = (int)MathF.Ceiling((scrollY + height) / _charAdvance.Y);

        var left = (int)MathF.Ceiling(scrollX / _charAdvance.X);
        var right = (int)MathF.Ceiling((scrollX + width) / _charAdvance.X);

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

    internal Coordinates ScreenPosToCoordinates(Vector2 position)
    {
        Vector2 origin = ImGui.GetCursorScreenPos();
        Vector2 local = new(position.X - origin.X, position.Y - origin.Y);

        int lineCount = _text.LineCount;
        int lineNo = Math.Max(0, (int)MathF.Floor(local.Y / _charAdvance.Y));
        int columnCoord = 0;

        if (lineNo < lineCount)
        {
            var line = _text.GetLine(lineNo);

            int columnIndex = 0;
            float columnX = 0.0f;

            while (columnIndex < line.Length)
            {
                float columnWidth;

                if (line[columnIndex].Char == '\t')
                {
                    float spaceSize = _charWidthCache.Get(' ');
                    float oldX = columnX;
                    float newColumnX = (1.0f + MathF.Floor((1.0f + columnX) / (_text.TabSize * spaceSize))) * (_text.TabSize * spaceSize);
                    columnWidth = newColumnX - oldX;
                    if (_textStart + columnX + columnWidth * 0.5f > local.X)
                        break;

                    columnX = newColumnX;
                    columnCoord = columnCoord / _text.TabSize * _text.TabSize + _text.TabSize;
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

        return _text.SanitizeCoordinates((lineNo, columnCoord));
    }
}
