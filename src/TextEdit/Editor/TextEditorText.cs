using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace ImGuiColorTextEditNet.Editor;

internal class TextEditorText
{
    readonly TextEditorOptions _options;
    readonly List<Line> _lines = new();
    int _tabSize = 4;

    internal TextEditorText(TextEditorOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _lines.Add(new Line());
    }

    internal int LineCount => _lines.Count;
    internal bool ScrollToCursor { get; set; }

    internal delegate void LineAddedHandler(int index);
    internal delegate void LinesRemovedHandler(int start, int end);

    internal event Action? AllTextReplaced;
    internal event LineAddedHandler? LineAdded;
    internal event LinesRemovedHandler? LinesRemoved;

    internal ReadOnlySpan<Glyph> GetLine(int index) => CollectionsMarshal.AsSpan(_lines[index].Glyphs);
    internal Span<Glyph> GetMutableLine(int index) => CollectionsMarshal.AsSpan(_lines[index].Glyphs);

    internal string GetLineText(int index)
    {
        var line = _lines[index].Glyphs;
        var sb = new StringBuilder(line.Count);
        foreach (var glyph in line)
            sb.Append(glyph.Char);
        return sb.ToString();
    }

    internal int TabSize
    {
        get => _tabSize;
        set => _tabSize = Math.Max(0, Math.Min(32, value));
    }

    internal static bool IsBlank(char c) => c is ' ' or '\t';
    internal string GetText(Coordinates startPos, Coordinates endPos)
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

    internal void SetText(string value)
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

        AllTextReplaced?.Invoke();
    }

    internal IList<string> TextLines
    {
        get
        {
            var result = new string[_lines.Count];

            var sb = new StringBuilder();
            for (int i = 0; i < _lines.Count; i++)
            {
                sb.Clear();
                _lines[i].Append(sb);
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
                    var internalLine = new Line(new List<Glyph>(stringLine.Length));
                    foreach (var c in stringLine)
                        internalLine.Glyphs.Add(new Glyph(c, PaletteIndex.Default));

                    _lines.Add(internalLine);
                }
            }

            AllTextReplaced?.Invoke();
        }
    }

    internal void DeleteRange(Coordinates startPos, Coordinates endPos)
    {
        Util.Assert(endPos >= startPos);
        Util.Assert(!_options.IsReadOnly);

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
    }

    void RemoveLine(int start, int end)
    {
        Util.Assert(!_options.IsReadOnly);
        Util.Assert(end >= start);
        Util.Assert(_lines.Count > end - start);

        _lines.RemoveRange(start, end - start);
        LinesRemoved?.Invoke(start, end);
        Util.Assert(_lines.Count != 0);
    }

    internal void RemoveLine(int lineNumber)
    {
        Util.Assert(!_options.IsReadOnly);
        Util.Assert(_lines.Count > 1);

        _lines.RemoveAt(lineNumber);
        LinesRemoved?.Invoke(lineNumber, lineNumber);
        Util.Assert(_lines.Count != 0);
    }

    internal string RemoveInLine(int lineNum, int start, int end) // Removes range from [start..end), i.e. character at end index is not removed
    {
        if (end < start)
            return "";

        var sb = new StringBuilder();
        var line = _lines[lineNum];

        if (end > line.Glyphs.Count)
            end = line.Glyphs.Count;

        for (int i = start; i < end; i++)
            sb.Append(line.Glyphs[i]);

        line.Glyphs.RemoveRange(start, end - start);
        return sb.ToString();
    }

    List<Glyph> InsertLine(int lineNumber)
    {
        Util.Assert(!_options.IsReadOnly);

        var result = new Line();
        _lines.Insert(lineNumber, result);
        LineAdded?.Invoke(lineNumber);
        return result.Glyphs;
    }

    internal void InsertLine(int lineNumber, string text, PaletteIndex color = PaletteIndex.Default)
    {
        var line = new Line(new List<Glyph>(text.Length));
        foreach(var c in text)
            line.Glyphs.Add(new Glyph(c, color));
        InsertLine(lineNumber, line);
    }

    internal void InsertLine(int lineNumber, Line line)
    {
        Util.Assert(!_options.IsReadOnly);
        _lines.Insert(lineNumber, line);
        LineAdded?.Invoke(lineNumber);
    }

    internal void AppendToLine(int lineNum, string text, PaletteIndex color = PaletteIndex.Default)
    {
        var line = _lines[lineNum];
        foreach (var c in text)
            line.Glyphs.Add(new Glyph(c, color));
    }

    internal void InsertCharAt(Coordinates pos, char c)
    {
        Util.Assert(!_options.IsReadOnly);
        Util.Assert(_lines.Count != 0);


        if (c == '\r')
            return;

        int cindex = GetCharacterIndex(pos);
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
        }
        else
        {
            var line = _lines[pos.Line].Glyphs;
            var glyph = new Glyph(c, PaletteIndex.Default);
            line.Insert(cindex, glyph);
            pos.Column++;
        }
    }

    internal int InsertTextAt(Coordinates pos, string value)
    {
        Util.Assert(!_options.IsReadOnly);

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
        }

        return totalLines;
    }

    internal string GetWordAt(Coordinates position)
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

    internal int GetCharacterIndex(Coordinates position)
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
                c++;
            i++;
        }

        return i;
    }

    internal int GetCharacterColumn(int lineNumber, int columnNumber)
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

    internal int GetLineMaxColumn(int lineNumber)
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

    internal bool IsOnWordBoundary(Coordinates position)
    {
        if (position.Line >= _lines.Count || position.Column == 0)
            return true;

        var line = _lines[position.Line].Glyphs;
        var cindex = GetCharacterIndex(position);
        if (cindex >= line.Count)
            return true;

        if (_options.IsColorizerEnabled)
            return line[cindex].ColorIndex != line[cindex - 1].ColorIndex;

        return char.IsWhiteSpace(line[cindex].Char) != char.IsWhiteSpace(line[cindex - 1].Char);
    }

    internal Coordinates SanitizeCoordinates(Coordinates value)
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

    internal void Advance(Coordinates position)
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

    internal Coordinates FindWordStart(Coordinates position)
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

    internal Coordinates FindWordEnd(Coordinates position)
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

    internal Coordinates FindNextWord(Coordinates from)
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
}