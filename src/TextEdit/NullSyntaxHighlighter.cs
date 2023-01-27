using System;

namespace ImGuiColorTextEditNet;

public class NullSyntaxHighlighter : ISyntaxHighlighter
{
    static readonly object DefaultState = new();
    NullSyntaxHighlighter() { }
    public static NullSyntaxHighlighter Instance { get; } = new();
    public bool AutoIndentation { get; init; }
    public int MaxLinesPerFrame { get; init; } = 1000;
    public string? GetTooltip(string id) => null;
    public object Colorize(Span<Glyph> line, object? state) => DefaultState;
}