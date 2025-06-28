using System;
using System.Collections.Generic;

namespace ImGuiColorTextEditNet.Editor;

/// <summary>Manages breakpoints, allowing for setting, checking, and removing breakpoints on specific lines.</summary>
public class TextEditorBreakpoints
{
    Dictionary<int, object> _breakpoints = new();

    internal TextEditorBreakpoints(TextEditorText text)
    {
        ArgumentNullException.ThrowIfNull(text);
        text.AllTextReplaced += () => _breakpoints.Clear();
        text.LineAdded += TextOnLineAdded;
        text.LinesRemoved += TextOnLinesRemoved;
    }

    /// <summary>Event that is raised when a breakpoint is removed.</summary>
    public event EventHandler<BreakpointRemovedEventArgs>? BreakpointRemoved;

    /// <summary>Checks if a breakpoint exists on the specified line number.</summary>
    public bool IsLineBreakpoint(int lineNumber) => _breakpoints.ContainsKey(lineNumber);

    /// <summary>Adds a breakpoint at the specified line number with the given context.</summary>
    public void Add(int lineNumber, object context) => _breakpoints[lineNumber] = context;

    /// <summary>Removes the breakpoint at the specified line number, if it exists.</summary>
    public void SetBreakpoints(IEnumerable<(int, object)> breakpoints)
    {
        _breakpoints.Clear();
        foreach (var (line, context) in breakpoints)
            _breakpoints[line] = context;
    }

    /// <summary>Removes the breakpoint at the specified line number, if it exists.</summary>
    public object? GetBreakpoint(int lineNumber)
    {
        _breakpoints.TryGetValue(lineNumber, out var value);
        return value;
    }

    internal object SerializeState() => _breakpoints;

    void TextOnLineAdded(int index)
    {
        Dictionary<int, object> newBreakpoints = new();
        foreach (var kvp in _breakpoints)
        {
            int newIndex = kvp.Key >= index ? kvp.Key + 1 : kvp.Key;
            newBreakpoints[newIndex] = kvp.Value;
        }
        _breakpoints = newBreakpoints;
    }

    void TextOnLinesRemoved(int start, int end)
    {
        var newBreakpoints = new Dictionary<int, object>();
        int lineCount = end - start + 1;
        foreach (var kvp in _breakpoints)
        {
            var i = kvp.Key;
            if (i >= start && i <= end)
            {
                BreakpointRemoved?.Invoke(this, new(kvp.Value));
                continue;
            }

            var newIndex = i >= start ? i - lineCount : i;
            newBreakpoints[newIndex] = kvp.Value;
        }

        _breakpoints = newBreakpoints;
    }
}
