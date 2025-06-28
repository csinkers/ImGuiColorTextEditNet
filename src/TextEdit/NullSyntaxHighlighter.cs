using System;

namespace ImGuiColorTextEditNet;

/// <summary>A syntax highlighter that does not perform any syntax highlighting.</summary>
public class NullSyntaxHighlighter : ISyntaxHighlighter
{
    static readonly object DefaultState = new();

    NullSyntaxHighlighter() { }

    /// <summary>Singleton instance of the <see cref="NullSyntaxHighlighter"/> class.</summary>
    public static NullSyntaxHighlighter Instance { get; } = new();

    /// <summary>Indicates whether the highlighter supports auto-indentation.</summary>
    public bool AutoIndentation { get; init; }

    /// <summary>The maximum number of lines that can be processed in a single frame.</summary>
    public int MaxLinesPerFrame { get; init; } = 1000;

    /// <summary>Retrieves the tooltip for a given identifier. Since this is a null highlighter, it returns null.</summary>
    public string? GetTooltip(string id) => null;

    /// <summary>Colorizes a line of text. Since this is a null highlighter, it does not perform any colorization and returns the default state.</summary>
    public object Colorize(Span<Glyph> line, object? state) => DefaultState;
}
