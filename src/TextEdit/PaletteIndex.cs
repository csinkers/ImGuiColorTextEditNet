namespace ImGuiColorTextEditNet;

/// <summary>Defines the color palette indices used for syntax highlighting.</summary>
public enum PaletteIndex : ushort
{
    /// <summary>The default color index used for text that does not match any specific syntax highlighting rules.</summary>
    Default,

    /// <summary>The color for keywords in the language (e.g., `if`, `else`, `while`).</summary>
    Keyword,

    /// <summary>The color for numbers.</summary>
    Number,

    /// <summary>The color for string literals.</summary>
    String,

    /// <summary>The color for character literals.</summary>
    CharLiteral,

    /// <summary>The color for operators, punctuation marks etc.</summary>
    Punctuation,

    /// <summary>The color for preprocessor directives.</summary>
    Preprocessor,

    /// <summary>The color for identifiers (variable names, type names etc.)</summary>
    Identifier,

    /// <summary>The color for known identifiers (e.g. build in functions).</summary>
    KnownIdentifier,

    /// <summary>The color for preprocessor identifiers.</summary>
    PreprocIdentifier,

    /// <summary>The color for single-line comments.</summary>
    Comment,

    /// <summary>The color for multi-line comments.</summary>
    MultiLineComment,

    /// <summary>The background color.</summary>
    Background,

    /// <summary>The color of the cursor.</summary>
    Cursor,

    /// <summary>The color used for text selection.</summary>
    Selection,

    /// <summary>The color for error markers.</summary>
    ErrorMarker,

    /// <summary>The color for breakpoints.</summary>
    Breakpoint,

    /// <summary>The color used for line numbers.</summary>
    LineNumber,

    /// <summary>The fill color for the current line.</summary>
    CurrentLineFill,

    /// <summary>The fill color for the current line when it is inactive.</summary>
    CurrentLineFillInactive,

    /// <summary>The edge color for the current line.</summary>
    CurrentLineEdge,

    /// <summary>The color used for the currently executing line when the editor is used in a debugger.</summary>
    ExecutingLine,

    /// <summary>This index and any values higher than it are for custom user-controlled colors</summary>
    Custom
}
