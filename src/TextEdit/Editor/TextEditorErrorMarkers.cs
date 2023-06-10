using System;
using System.Collections.Generic;

namespace ImGuiColorTextEditNet.Editor;

public class TextEditorErrorMarkers
{
    Dictionary<int, string> _errorMarkers = new();

    internal TextEditorErrorMarkers(TextEditorText text)
    {
        if (text == null) throw new ArgumentNullException(nameof(text));
        text.AllTextReplaced += () => _errorMarkers.Clear();
        text.LineAdded += TextOnLineAdded;
        text.LinesRemoved += _text_LinesRemoved;
    }

    void TextOnLineAdded(int index)
    {
        var tempErrors = new Dictionary<int, string>(_errorMarkers.Count);
        foreach (var i in _errorMarkers)
            tempErrors[i.Key >= index ? i.Key + 1 : i.Key] = i.Value;
        _errorMarkers = tempErrors;
    }

    public void SetErrorMarkers(Dictionary<int, string> value)
    {
        _errorMarkers.Clear();
        foreach(var kvp in value)
            _errorMarkers[kvp.Key] = kvp.Value;
    }

    void _text_LinesRemoved(int start, int end)
    {
        var tempErrors = new Dictionary<int, string>();
        int lineCount = end - start + 1;
        foreach (var kvp in _errorMarkers)
        {
            int key = kvp.Key >= start ? kvp.Key - lineCount : kvp.Key;
            if (key >= start && key <= end)
                continue;

            tempErrors[key] = kvp.Value;
        }
        _errorMarkers = tempErrors;
    }

    public bool TryGetErrorForLine(int lineNo, out string? errorInfo) 
        => _errorMarkers.TryGetValue(lineNo, out errorInfo);
}