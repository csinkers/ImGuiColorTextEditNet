using System;
using System.Collections.Generic;

namespace ImGuiColorTextEditNet.Editor;

/// <summary>Manages error markers.</summary>
public class TextEditorErrorMarkers
{
    Dictionary<int, object> _errorMarkers = new();

    /// <summary>Formatter for error markers, which converts the error context to a string representation.</summary>
    public Func<object, string> ErrorMarkerFormatter { get; set; } = x => x.ToString() ?? "";

    internal TextEditorErrorMarkers(TextEditorText text)
    {
        ArgumentNullException.ThrowIfNull(text);
        text.AllTextReplaced += () => _errorMarkers.Clear();
        text.LineAdded += TextOnLineAdded;
        text.LinesRemoved += _text_LinesRemoved;
    }

    /// <summary>Retrieves the error marker for the specified line number (if any).</summary>
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

    /// <summary>Adds an error marker at the specified line number with the given context.</summary>
    public void Add(int lineNumber, int context) => _errorMarkers[lineNumber] = context;

    /// <summary>Sets the error markers, replacing any existing markers.</summary>
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
