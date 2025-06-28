namespace ImGuiColorTextEditNet;

/// <summary>
/// Built-in color schemes
/// </summary>
public static class Palettes
{
    /// <summary>Default dark theme</summary>
    public static readonly uint[] Dark =
    [
        0xff7f7f7f, // Default
        0xffd69c56, // Keyword
        0xff00ff00, // Number
        0xff7070e0, // String
        0xff70a0e0, // Char literal
        0xffffffff, // Punctuation
        0xff408080, // Preprocessor
        0xffaaaaaa, // Identifier
        0xff9bc64d, // Known identifier
        0xffc040a0, // Preproc identifier
        0xff50c050, // Comment (single line)
        0xff70c050, // Comment (multi line)
        0xff101010, // Background
        0xffe0e0e0, // Cursor
        0x80a06020, // Selection
        0x800020ff, // ErrorMarker
        0x40f08000, // Breakpoint
        0xff707000, // Line number
        0x40000000, // Current line fill
        0x40808080, // Current line fill (inactive)
        0x40a0a0a0, // Current line edge
        0xa0a0a0a0 // Executing Line
    ];

    /// <summary>Default light theme</summary>
    public static readonly uint[] Light =
    [
        0xff7f7f7f, // None
        0xffff0c06, // Keyword
        0xff008000, // Number
        0xff2020a0, // String
        0xff304070, // Char literal
        0xff000000, // Punctuation
        0xff406060, // Preprocessor
        0xff404040, // Identifier
        0xff606010, // Known identifier
        0xffc040a0, // Preproc identifier
        0xff205020, // Comment (single line)
        0xff405020, // Comment (multi line)
        0xffffffff, // Background
        0xff000000, // Cursor
        0x80600000, // Selection
        0xa00010ff, // ErrorMarker
        0x80f08000, // Breakpoint
        0xff505000, // Line number
        0x40000000, // Current line fill
        0x40808080, // Current line fill (inactive)
        0x40000000, // Current line edge
        0xa0a0a0a0 // Executing Line
    ];

    /// <summary>Default blue theme</summary>
    public static readonly uint[] RetroBlue =
    [
        0xff00ffff, // None
        0xffffff00, // Keyword
        0xff00ff00, // Number
        0xff808000, // String
        0xff808000, // Char literal
        0xffffffff, // Punctuation
        0xff008000, // Preprocessor
        0xff00ffff, // Identifier
        0xffffffff, // Known identifier
        0xffff00ff, // Preproc identifier
        0xff808080, // Comment (single line)
        0xff404040, // Comment (multi line)
        0xff800000, // Background
        0xff0080ff, // Cursor
        0x80ffff00, // Selection
        0xa00000ff, // ErrorMarker
        0x80ff8000, // Breakpoint
        0xff808000, // Line number
        0x40000000, // Current line fill
        0x40808080, // Current line fill (inactive)
        0x40000000, // Current line edge
        0xa0a0a0a0 // Executing Line
    ];
}