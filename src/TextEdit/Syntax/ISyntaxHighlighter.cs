using System;

namespace ImGuiColorTextEditNet.Syntax;

/// <summary>
/// Defines the interface for syntax highlighters.
/// </summary>
public interface ISyntaxHighlighter
{
    /// <summary>Indicates whether the highlighter supports auto-indentation.</summary>
    bool AutoIndentation { get; }

    /// <summary>The maximum number of lines that can be processed in a single frame.</summary>
    int MaxLinesPerFrame { get; }

    /// <summary>Retrieves the tooltip for a given identifier.</summary>
    string? GetTooltip(string id);

    /// <summary>Colorizes a line of text.</summary>
    object Colorize(Span<Glyph> line, object? state);
}
