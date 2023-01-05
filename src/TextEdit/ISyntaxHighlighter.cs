namespace ImGuiColorTextEditNet;

public interface ISyntaxHighlighter
{
    bool AutoIndentation { get; }
    int MaxLinesPerFrame { get; }
    string? GetTooltip(string id);
    object Colorize(Span<Glyph> line, object? state);
}