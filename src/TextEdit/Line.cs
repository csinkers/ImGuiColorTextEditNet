using System;
using System.Collections.Generic;

namespace ImGuiColorTextEditNet;

public class Line
{
    public List<Glyph> Glyphs { get; init; }
    public object? SyntaxState { get; init; }

    public Line() => Glyphs = new List<Glyph>();

    public Line(List<Glyph> glyphs, object? syntaxState)
    {
        Glyphs = glyphs ?? throw new ArgumentNullException(nameof(glyphs));
        SyntaxState = syntaxState;
    }

    public void Deconstruct(out List<Glyph> glyphs, out object? syntaxState)
    {
        glyphs = Glyphs;
        syntaxState = SyntaxState;
    }
}