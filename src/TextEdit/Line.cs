using System;
using System.Collections.Generic;

namespace ImGuiColorTextEditNet;

/// <summary>Represents a line of text.</summary>
public class Line
{
    /// <summary>
    /// A list of <see cref="Glyph"/> objects representing the characters in this line.
    /// </summary>
    public List<Glyph> Glyphs { get; init; }

    /// <summary>Initializes a new instance of the <see cref="Line"/> class with an empty list of glyphs.</summary>
    public Line() => Glyphs = [];

    /// <summary>Initializes a new instance of the <see cref="Line"/> class with the specified list of glyphs.</summary>
    public Line(List<Glyph> glyphs) =>
        Glyphs = glyphs ?? throw new ArgumentNullException(nameof(glyphs));

    /// <summary>Appends a string to the line using the color of the last glyph, or the default color if there are no glyphs.</summary>
    public void Append(string s)
    {
        var color = Glyphs.Count > 0 ? Glyphs[^1].ColorIndex : PaletteIndex.Default;
        Append(color, s);
    }

    /// <summary>Appends a string to the line using the specified color index for all characters in the string.</summary>
    public void Append(PaletteIndex color, string s)
    {
        foreach (var c in s)
            Glyphs.Add(new Glyph(c, color));
    }
}
