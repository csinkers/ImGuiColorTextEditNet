namespace ImGuiColorTextEditNet;
public struct Glyph
{
    public readonly char Char;
    public readonly PaletteIndex ColorIndex = PaletteIndex.Default;
    public override string ToString() => $"{Char} {ColorIndex}";
    public Glyph(char aChar, PaletteIndex aColorIndex)
    {
        Char = aChar;
        ColorIndex = aColorIndex;
    }
}