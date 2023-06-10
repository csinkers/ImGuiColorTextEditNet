using System;
using System.Collections.Generic;

namespace ImGuiColorTextEditNet.Editor;

public class TextEditorBreakpoints
{
    HashSet<int> _breakpoints = new();

    internal TextEditorBreakpoints(TextEditorText text)
    {
        if (text == null) throw new ArgumentNullException(nameof(text));
        text.AllTextReplaced += () => _breakpoints.Clear();
        text.LineAdded += TextOnLineAdded;
        text.LinesRemoved += TextOnLinesRemoved;
    }

    public bool IsLineBreakpoint(int lineNumber) => _breakpoints.Contains(lineNumber);
    public void SetBreakpoints(IEnumerable<int> lines)
    {
        _breakpoints.Clear();
        foreach (var line in lines)
            _breakpoints.Add(line);
    }

    void TextOnLineAdded(int index)
    {
        HashSet<int> btmp = new();
        foreach (var i in _breakpoints)
            btmp.Add(i >= index ? i + 1 : i);
        _breakpoints = btmp;
    }

    void TextOnLinesRemoved(int start, int end)
    {
        var btmp = new HashSet<int>();
        int lineCount = end - start + 1;
        foreach (var i in _breakpoints)
        {
            if (i >= start && i <= end)
                continue;

            btmp.Add(i >= start ? i - lineCount : i);
        }
        _breakpoints = btmp;
    }
}