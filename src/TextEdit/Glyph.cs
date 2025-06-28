namespace ImGuiColorTextEditNet;

/// <summary>
/// Represents a single character, along with its associated color index.
/// </summary>
public readonly struct Glyph
{
    /// <summary>The character represented by this glyph.</summary>
    public readonly char Char;

    /// <summary>The color index associated with this glyph, used to determine its display color.</summary>
    public readonly PaletteIndex ColorIndex = PaletteIndex.Default;

    /// <summary>Returns a string representation of the glyph, including its character and color index.</summary>
    public override string ToString() => $"{Char} {ColorIndex}";

    /// <summary>
    /// Initializes a new instance of the <see cref="Glyph"/> struct with the specified character and default color index.
    /// </summary>
    public Glyph(char aChar, PaletteIndex aColorIndex)
    {
        Char = aChar;
        ColorIndex = aColorIndex;
    }
}
