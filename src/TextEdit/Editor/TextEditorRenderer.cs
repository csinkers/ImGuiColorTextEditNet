using System;
using System.Buffers;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using ImGuiNET;

namespace ImGuiColorTextEditNet.Editor;

/// <summary>Renders the text editor, handling the display of text, syntax highlighting, selection, and other visual elements.</summary>
public class TextEditorRenderer
{
    const float LineSpacing = 1.0f;
    const int LeftMargin = 10;
    const int CursorBlinkPeriodMs = 800;
    const uint MagentaUInt = 0xff00ffff;
    static readonly Vector4 MagentaVec4 = new(1.0f, 1.0f, 1.0f, 1.0f);

    // Note: if fonts / sizes can ever be changed the char width cache will need to be invalidated.
    readonly SimpleCache<char, float> _charWidthCache = new(
        "char widths",
        x =>
        {
            var font = ImGui.GetFont();
            float scale = ImGui.GetFontSize() / font.FontSize;
            return font.GetCharAdvance(x) * scale;
        }
    );

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

    /// <summary>Gets or sets the color palette used for syntax highlighting and other visual elements.</summary>
    public uint[] Palette
    {
        get => _palette.ToArray();
        set
        {
            ArgumentNullException.ThrowIfNull(value);

            _palette.Clear();
            _palette.AddRange(value);
            _paletteDirty = true;
        }
    }

    /// <summary>Sets the color for a specific palette index.</summary>
    public void SetColor(PaletteIndex color, uint abgr)
    {
        int index = (int)color;
        while (_palette.Count <= index)
            _palette.Add(MagentaUInt);

        _palette[index] = abgr;
        _paletteDirty = true;
    }

    /// <summary>Gets the total height of the editor in lines.</summary>
    public int PageSize
    {
        get
        {
            var height = ImGui.GetWindowHeight() - 20.0f;
            return (int)MathF.Floor(height / _charAdvance.Y);
        }
    }

    /// <summary>The keyboard input handler.</summary>
    public ITextEditorKeyboardInput? KeyboardInput { get; init; }

    /// <summary>The mouse input handler.</summary>
    public ITextEditorMouseInput? MouseInput { get; init; }

    /// <summary>Indicates whether the ImGui child window should be ignored for input handling.</summary>
    public bool IsImGuiChildIgnored { get; set; }

    /// <summary>Indicates whether mouse inputs should be handled by the editor.</summary>
    public bool IsHandleMouseInputsEnabled { get; set; } = true;

    /// <summary>Indicates whether keyboard inputs should be handled by the editor.</summary>
    public bool IsHandleKeyboardInputsEnabled { get; set; } = true;

    /// <summary>Indicates whether whitespace characters (spaces and tabs) should be made visible in the editor.</summary>
    public bool IsShowingWhitespace { get; set; } = true;

    internal TextEditorRenderer(TextEditor editor, uint[] palette)
    {
        ArgumentNullException.ThrowIfNull(editor);
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
        var background =
            _vec4Palette == null
                ? ImGui.ColorConvertU32ToFloat4(_palette[(int)PaletteIndex.Background])
                : ColorVec(PaletteIndex.Background);

        ImGui.PushStyleColor(ImGuiCol.ChildBg, background);
        ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(0.0f, 0.0f));

        if (!IsImGuiChildIgnored)
        {
            ImGui.BeginChild(
                title,
                size,
                ImGuiChildFlags.None,
                ImGuiWindowFlags.HorizontalScrollbar
                    | ImGuiWindowFlags.AlwaysHorizontalScrollbar
                    | ImGuiWindowFlags.NoMove
            );
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
                EnsurePositionVisible(new(_text.PendingScrollRequest.Value, 0));

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
        _charAdvance = new(fontSize, ImGui.GetTextLineHeightWithSpacing() * LineSpacing);

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

        var contentSize = ImGui.GetContentRegionAvail();
        ImDrawListPtr drawList = ImGui.GetWindowDrawList();
        float longest = _textStart;

        Vector2 cursorScreenPos = ImGui.GetCursorScreenPos();
        var scrollY = ImGui.GetScrollY();

        var lineNo = (int)MathF.Floor(scrollY / _charAdvance.Y);
        var globalLineMax = _text.LineCount;
        var lineMax = Math.Max(
            0,
            Math.Min(
                globalLineMax - 1,
                lineNo + (int)MathF.Floor((scrollY + contentSize.Y) / _charAdvance.Y)
            )
        );

        // Deduce _textStart by evaluating _lines size (global lineMax) plus two spaces as text width
        float spaceWidth = _charWidthCache.Get(' ');
        var buf = _lineNumberCache.Get(globalLineMax);
        _textStart = ImGui.CalcTextSize(buf).X + LeftMargin + spaceWidth;

        if (globalLineMax != 0)
        {
            for (; lineNo <= lineMax; ++lineNo)
            {
                RenderInnerLine(
                    cursorScreenPos,
                    lineNo,
                    drawList,
                    contentSize.X,
                    spaceWidth,
                    ref longest
                );
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

        ImGui.Dummy(new(longest + 2, globalLineMax * _charAdvance.Y));
    }

    void RenderInnerLine(
        Vector2 cursorScreenPos,
        int lineNo,
        ImDrawListPtr drawList,
        float contentWidth,
        float spaceWidth,
        ref float longest
    )
    {
        Vector2 lineStartScreenPos = cursorScreenPos with
        {
            Y = cursorScreenPos.Y + lineNo * _charAdvance.Y,
        };

        Vector2 textScreenPos = lineStartScreenPos with { X = lineStartScreenPos.X + _textStart };

        var line = _text.GetLine(lineNo);
        longest = Math.Max(
            _textStart + TextDistanceToLineStart((lineNo, _text.GetLineMaxColumn(lineNo))),
            longest
        );

        Coordinates lineStartCoord = new(lineNo, 0);
        Coordinates lineEndCoord = new(lineNo, _text.GetLineMaxColumn(lineNo));

        // Draw selection for the current line
        float selectionStart = float.NegativeInfinity;
        float selectionEnd = float.NegativeInfinity;

        Util.Assert(_selection.Start <= _selection.End);
        if (_selection.Start <= lineEndCoord)
        {
            selectionStart =
                _selection.Start > lineStartCoord
                    ? TextDistanceToLineStart(_selection.Start)
                    : 0.0f;
        }

        if (_selection.End > lineStartCoord)
        {
            selectionEnd = TextDistanceToLineStart(
                _selection.End < lineEndCoord ? _selection.End : lineEndCoord
            );
        }

        if (_selection.End.Line > lineNo && line.Length == 0)
            selectionEnd += _charAdvance.X;

        if (
            !float.IsNegativeInfinity(selectionStart)
            && !float.IsNegativeInfinity(selectionEnd)
            && selectionStart < selectionEnd
        )
        {
            Vector2 vstart = lineStartScreenPos with
            {
                X = lineStartScreenPos.X + _textStart + selectionStart,
            };

            Vector2 vend = new(
                lineStartScreenPos.X + _textStart + selectionEnd,
                lineStartScreenPos.Y + _charAdvance.Y
            );

            drawList.AddRectFilled(vstart, vend, ColorUInt(PaletteIndex.Selection));
        }

        // Draw breakpoints
        var scrollX = ImGui.GetScrollX();
        var start = lineStartScreenPos with { X = lineStartScreenPos.X + scrollX };

        if (_breakpoints.IsLineBreakpoint(lineNo + 1))
        {
            var end = new Vector2(
                lineStartScreenPos.X + contentWidth + 2.0f * scrollX,
                lineStartScreenPos.Y + _charAdvance.Y
            );

            drawList.AddRectFilled(start, end, ColorUInt(PaletteIndex.Breakpoint));
        }

        if (lineNo == _selection.HighlightedLine)
        {
            var end = new Vector2(
                lineStartScreenPos.X + contentWidth + 2.0f * scrollX,
                lineStartScreenPos.Y + _charAdvance.Y
            );

            var color = ColorUInt(PaletteIndex.ExecutingLine);
            drawList.AddRectFilled(start, end, color);
        }

        // Draw error markers
        if (_errorMarkers.TryGetErrorForLine(lineNo + 1, out var error))
        {
            var end = new Vector2(
                lineStartScreenPos.X + contentWidth + 2.0f * scrollX,
                lineStartScreenPos.Y + _charAdvance.Y
            );

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
        string buf = _lineNumberCache.Get(lineNo + 1);

        var lineNoWidth = ImGui.CalcTextSize(buf).X;
        drawList.AddText(
            lineStartScreenPos with
            {
                X = lineStartScreenPos.X + _textStart - lineNoWidth,
            },
            ColorUInt(PaletteIndex.LineNumber),
            buf
        );

        if (_selection.Cursor.Line == lineNo)
        {
            var focused = ImGui.IsWindowFocused();

            // Highlight the current line (where the cursor is)
            if (!_selection.HasSelection)
            {
                var end = new Vector2(start.X + contentWidth + scrollX, start.Y + _charAdvance.Y);
                drawList.AddRectFilled(
                    start,
                    end,
                    ColorUInt(
                        (
                            focused
                                ? PaletteIndex.CurrentLineFill
                                : PaletteIndex.CurrentLineFillInactive
                        )
                    )
                );

                drawList.AddRect(start, end, ColorUInt(PaletteIndex.CurrentLineEdge), 1.0f);
            }

            // Render the cursor
            if (focused)
            {
                var timeEnd = DateTime.UtcNow;
                var elapsed = timeEnd - _startTime;

                if (elapsed.Milliseconds > CursorBlinkPeriodMs / 2)
                {
                    float width = 1.0f;
                    var cindex = _text.GetCharacterIndex(_selection.Cursor);
                    float cx = TextDistanceToLineStart(_selection.Cursor);

                    if (_options.IsOverwrite && cindex < line.Length)
                    {
                        var c = line[cindex].Char;
                        if (c == '\t')
                        {
                            var x =
                                (1.0f + MathF.Floor((1.0f + cx) / (_text.TabSize * spaceWidth)))
                                * (_text.TabSize * spaceWidth);
                            width = x - cx;
                        }
                        else
                        {
                            width = _charWidthCache.Get(line[cindex].Char);
                        }
                    }

                    Vector2 cstart = lineStartScreenPos with { X = textScreenPos.X + cx };
                    Vector2 cend = new(
                        textScreenPos.X + cx + width,
                        lineStartScreenPos.Y + _charAdvance.Y
                    );

                    drawList.AddRectFilled(cstart, cend, ColorUInt(PaletteIndex.Cursor));

                    if (elapsed.Milliseconds > CursorBlinkPeriodMs)
                        _startTime = timeEnd;
                }
            }
        }

        // Render colorized text
        uint prevColor =
            line.Length == 0 ? ColorUInt(PaletteIndex.Default) : ColorUInt(line[0].ColorIndex);
        var bufferOffset = new Vector2();

        for (int i = 0; i < line.Length; )
        {
            var glyph = line[i];
            var color = ColorUInt(glyph.ColorIndex);

            if ((color != prevColor || glyph.Char is '\t' or ' ') && _lineBuffer.Length != 0)
            {
                Vector2 newOffset = new(
                    textScreenPos.X + bufferOffset.X,
                    textScreenPos.Y + bufferOffset.Y
                );

                var textSize = DrawText(drawList, newOffset, prevColor, _lineBuffer);
                bufferOffset.X += textSize.X;
                _lineBuffer.Clear();
            }

            prevColor = color;

            if (glyph.Char == '\t')
            {
                var oldX = bufferOffset.X;
                bufferOffset.X =
                    (1.0f + MathF.Floor((1.0f + bufferOffset.X) / (_text.TabSize * spaceWidth)))
                    * (_text.TabSize * spaceWidth);
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
                    var x = textScreenPos.X + bufferOffset.X + spaceWidth * 0.5f;
                    var y = textScreenPos.Y + bufferOffset.Y + s * 0.5f;
                    drawList.AddCircleFilled(new(x, y), 1.5f, 0x80808080, 4);
                }

                bufferOffset.X += spaceWidth;
                i++;
            }
            else
            {
                _lineBuffer.Append(line[i++].Char);
            }
        }

        if (_lineBuffer.Length != 0)
        {
            Vector2 newOffset = textScreenPos + bufferOffset;
            DrawText(drawList, newOffset, prevColor, _lineBuffer);
            _lineBuffer.Clear();
        }
    }

    static Vector2 DrawText(ImDrawListPtr drawList, Vector2 offset, uint color, StringBuilder sb)
    {
        char[]? tempArray = null;
        if (sb.Length > 1024)
            tempArray = ArrayPool<char>.Shared.Rent(sb.Length);

        try
        {
            Span<char> temp =
                sb.Length > 1024 ? tempArray.AsSpan(0, sb.Length) : stackalloc char[sb.Length];

            int i = 0;

            foreach (var chunk in sb.GetChunks())
            {
                chunk.Span.CopyTo(temp[i..]);
                i += chunk.Length;
            }

            drawList.AddText(offset, color, temp);
            return ImGui.CalcTextSize(temp);
        }
        finally
        {
            if (tempArray != null)
                ArrayPool<char>.Shared.Return(tempArray);
        }
    }

    float TextDistanceToLineStart(Coordinates position)
    {
        var line = _text.GetLine(position.Line);
        float distance = 0.0f;
        float spaceSize = _charWidthCache.Get(' '); // remaining

        int colIndex = _text.GetCharacterIndex(position);
        PaletteIndex lastColor = 0;
        for (int i = 0; i < line.Length && i < colIndex; )
        {
            var glyph = line[i];
            if (lastColor != glyph.ColorIndex && glyph.Char != ' ')
            {
                lastColor = glyph.ColorIndex;
                // Each 'text block' that ImGui draws gets aligned to a 1-pixel grid
                // so when the color changes we need to round up!
                distance = MathF.Ceiling(distance);
            }

            var c = glyph.Char;
            distance =
                c == '\t'
                    ? (1.0f + MathF.Floor((1.0f + distance) / (_text.TabSize * spaceSize)))
                        * (_text.TabSize * spaceSize)
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
                    float newColumnX =
                        (1.0f + MathF.Floor((1.0f + columnX) / (_text.TabSize * spaceSize)))
                        * (_text.TabSize * spaceSize);

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
