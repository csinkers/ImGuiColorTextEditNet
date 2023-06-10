using System;
using System.Collections.Generic;
using System.Text;

namespace ImGuiColorTextEditNet;

public class Line
{
    public List<Glyph> Glyphs { get; init; }
    public Line() => Glyphs = new List<Glyph>();
    public Line(List<Glyph> glyphs) => Glyphs = glyphs ?? throw new ArgumentNullException(nameof(glyphs));
    public void Append(StringBuilder sb)
    {
        for (int j = 0; j < Glyphs.Count; ++j)
            sb.Append(Glyphs[j].Char);
    }
}