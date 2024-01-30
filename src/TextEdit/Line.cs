using System;
using System.Collections.Generic;

namespace ImGuiColorTextEditNet;

public class Line
{
    public List<Glyph> Glyphs { get; init; }
    public Line() => Glyphs = new List<Glyph>();
    public Line(List<Glyph> glyphs) => Glyphs = glyphs ?? throw new ArgumentNullException(nameof(glyphs));

    public void Append(string s)
    {
        var color = Glyphs.Count > 0 ? Glyphs[^1].ColorIndex : PaletteIndex.Default;
        Append(color, s);
    }

    public void Append(PaletteIndex color, string s)
    {
        foreach (var c in s)
            Glyphs.Add(new Glyph(c, color));
    }
}