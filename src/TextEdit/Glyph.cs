namespace ImGuiColorTextEditNet;
struct Glyph
{
    public char Char;
    public GlyphFlags Flags;
    public PaletteIndex ColorIndex = PaletteIndex.Default;

    public bool Comment
    {
        get => (Flags & GlyphFlags.Comment) != 0;
        set => Flags = (Flags & ~GlyphFlags.Comment) | (value ? GlyphFlags.Comment : 0);
    }

    public bool MultiLineComment
    {
        get => (Flags & GlyphFlags.MultiLineComment) != 0;
        set => Flags = (Flags & ~GlyphFlags.MultiLineComment) | (value ? GlyphFlags.MultiLineComment : 0);
    }

    public bool Preprocessor
    {
        get => (Flags & GlyphFlags.Preprocessor) != 0;
        set => Flags = (Flags & ~GlyphFlags.Preprocessor) | (value ? GlyphFlags.Preprocessor : 0);
    }

    public Glyph(char aChar, PaletteIndex aColorIndex)
    {
        Char = aChar;
        ColorIndex = aColorIndex;
        Flags = 0;
    }
}