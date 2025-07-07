using System;
using System.Collections.Generic;
using System.Text;
using ImGuiColorTextEditNet.Editor;

namespace ImGuiColorTextEditNet;

/// <summary>Represents a line of text.</summary>
public class Line
{
    /// <summary>
    /// A list of <see cref="Glyph"/> objects representing the characters in this line.
    /// </summary>
    public List<Glyph> Glyphs { get; init; }

    /// <summary>The length of the line (i.e., the number of glyphs in the line). </summary>
    public int Length => Glyphs.Count;

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

    internal void GetString(StringBuilder sb, int start = 0) => GetString(sb, start, Glyphs.Count);

    internal void GetString(StringBuilder sb, int start, int end)
    {
        if (end > Glyphs.Count)
            end = Glyphs.Count;

        for (int i = start; i < end; i++)
            sb.Append(Glyphs[i].Char);
    }

    internal int GetCharacterIndex(Coordinates position, TextEditorOptions options)
    {
        int i = 0;
        for (int c = 0; i < Glyphs.Count && c < position.Column; )
        {
            if (Glyphs[i].Char == '\t')
                c = options.NextTab(c);
            else
                c++;

            i++;
        }

        return i;
    }

    internal int GetCharacterColumn(int indexInLine, TextEditorOptions options)
    {
        int col = 0;

        int i = 0;
        while (i < indexInLine && i < Glyphs.Count)
        {
            if (Glyphs[i].Char == '\t')
                col = options.NextTab(col);
            else
                col++;
            i++;
        }

        return col;
    }

    internal int GetLineMaxColumn(TextEditorOptions options) =>
        GetCharacterColumn(int.MaxValue, options);
}
