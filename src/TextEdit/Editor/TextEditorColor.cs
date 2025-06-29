using System;
using System.Collections.Generic;
using ImGuiColorTextEditNet.Syntax;

namespace ImGuiColorTextEditNet.Editor;

internal class TextEditorColor
{
    internal ISyntaxHighlighter SyntaxHighlighter
    {
        get => _syntaxHighlighter;
        set
        {
            _syntaxHighlighter = value;
            InvalidateColor(0, -1);
        }
    }

    readonly TextEditorOptions _options;
    readonly TextEditorText _text;
    readonly List<object?> _lineState = new();
    int _colorRangeMin;
    int _colorRangeMax;
    ISyntaxHighlighter _syntaxHighlighter = NullSyntaxHighlighter.Instance;

    internal TextEditorColor(TextEditorOptions options, TextEditorText text)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _text = text ?? throw new ArgumentNullException(nameof(text));
        _text.AllTextReplaced += () => InvalidateColor(0, -1);
    }

    internal void ColorizeIncremental()
    {
        if (
            _text.LineCount == 0
            || !_options.IsColorizerEnabled
            || _colorRangeMin >= _colorRangeMax
        )
        {
            return;
        }

        int increment = SyntaxHighlighter.MaxLinesPerFrame;
        int to = Math.Min(_colorRangeMin + increment, _colorRangeMax);

        for (int lineIndex = _colorRangeMin; lineIndex < to; lineIndex++)
        {
            if (_lineState.Count <= lineIndex)
                _lineState.Add(null);

            var glyphs = _text.GetMutableLine(lineIndex);
            var state = lineIndex > 0 ? _lineState[lineIndex - 1] : null;
            state = SyntaxHighlighter.Colorize(glyphs, state);
            _lineState[lineIndex] = state;
        }

        _colorRangeMin = Math.Max(0, to);

        if (_colorRangeMax == _colorRangeMin) // Done?
        {
            _colorRangeMin = int.MaxValue;
            _colorRangeMax = 0;
        }
    }

    internal void InvalidateColor(int fromLine, int lineCount) // lineCount -1 = all lines after fromLine
    {
        fromLine = Math.Min(_colorRangeMin, fromLine);
        fromLine = Math.Max(0, fromLine);

        int toLine = _text.LineCount;

        if (lineCount != -1)
            toLine = Math.Min(_text.LineCount, fromLine + lineCount);

        toLine = Math.Max(_colorRangeMax, toLine);
        toLine = Math.Max(fromLine, toLine);

        _colorRangeMin = fromLine;
        _colorRangeMax = toLine;
    }
}
