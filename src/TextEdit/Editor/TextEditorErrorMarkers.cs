using System;
using System.Collections.Generic;

namespace ImGuiColorTextEditNet.Editor;

public class TextEditorErrorMarkers
{
    Dictionary<int, object> _errorMarkers = new();
    public Func<object, string> ErrorMarkerFormatter { get; set; } = x => x.ToString() ?? "";

    internal TextEditorErrorMarkers(TextEditorText text)
    {
        if (text == null) throw new ArgumentNullException(nameof(text));
        text.AllTextReplaced += () => _errorMarkers.Clear();
        text.LineAdded += TextOnLineAdded;
        text.LinesRemoved += _text_LinesRemoved;
    }

    public bool TryGetErrorForLine(int lineNo, out string errorInfo)
    {
        if (!_errorMarkers.TryGetValue(lineNo, out var error))
        {
            errorInfo = "";
            return false;
        }

        errorInfo = ErrorMarkerFormatter(error);
        return true;
    }

    public void Add(int lineNumber, int context) => _errorMarkers[lineNumber] = context;

    public void SetErrorMarkers(Dictionary<int, object> value)
    {
        _errorMarkers.Clear();
        foreach (var kvp in value)
            _errorMarkers[kvp.Key] = kvp.Value;
    }

    internal object SerializeState() => _errorMarkers;

    void TextOnLineAdded(int index)
    {
        var tempErrors = new Dictionary<int, object>(_errorMarkers.Count);
        foreach (var i in _errorMarkers)
            tempErrors[i.Key >= index ? i.Key + 1 : i.Key] = i.Value;
        _errorMarkers = tempErrors;
    }

    void _text_LinesRemoved(int start, int end)
    {
        var tempErrors = new Dictionary<int, object>();
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
}
