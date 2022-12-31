namespace ImGuiColorTextEditNet;

[Flags]
enum GlyphFlags : byte
{
    Comment = 0x1,
    MultiLineComment = 0x2,
    Preprocessor = 0x4
}