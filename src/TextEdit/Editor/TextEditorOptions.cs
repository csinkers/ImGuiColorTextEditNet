namespace ImGuiColorTextEditNet.Editor;

/// <summary>
/// Represents options for configuring the behavior and appearance of the text editor.
/// </summary>
public class TextEditorOptions
{
    /// <summary>
    /// Whether the text editor is read-only or allows editing.
    /// </summary>
    public bool IsReadOnly { get; set; }

    /// <summary>
    /// Whether the editor is in overwrite or insert mode.
    /// </summary>
    public bool IsOverwrite { get; set; }

    /// <summary>
    /// Whether the editor should colorize text using a syntax highlighter.
    /// </summary>
    public bool IsColorizerEnabled { get; set; } = true;
}
