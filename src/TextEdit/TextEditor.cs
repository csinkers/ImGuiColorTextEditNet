using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text.Json;
using ImGuiColorTextEditNet.Editor;
using ImGuiColorTextEditNet.Input;
using ImGuiColorTextEditNet.Syntax;

// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedMember.Global
// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace ImGuiColorTextEditNet;

/// <summary>Text editor component that provides functionality for editing text with syntax highlighting, undo/redo, selection, and more.</summary>
public class TextEditor
{
    internal TextEditorUndoStack UndoStack { get; }
    internal TextEditorColor Color { get; }
    internal TextEditorText Text { get; }

    /// <summary>Gets the selection manager, allowing for text selection and manipulation.</summary>
    public TextEditorSelection Selection { get; }

    /// <summary>Gets the options for configuring the text editor's behavior and appearance.</summary>
    public TextEditorOptions Options { get; }

    /// <summary>Gets the breakpoints manager, allowing for setting and managing breakpoints in the text.</summary>
    public TextEditorBreakpoints Breakpoints { get; }

    /// <summary>Gets the error markers manager, allowing for setting and managing error markers in the text.</summary>
    public TextEditorErrorMarkers ErrorMarkers { get; }

    /// <summary>Gets the renderer, responsible for rendering the text and its syntax highlighting.</summary>
    public TextEditorRenderer Renderer { get; }

    /// <summary>Gets the modification manager, allowing for text modifications such as insertions and deletions.</summary>
    public TextEditorModify Modify { get; }

    /// <summary>Gets the movement manager, allowing for cursor movement and navigation within the text.</summary>
    public TextEditorMovement Movement { get; }

    /// <summary>Initializes a new instance of the <see cref="TextEditor"/> class with default options and configurations.</summary>
    public TextEditor()
    {
        Options = new();
        Text = new(Options);
        Selection = new(Text);
        Breakpoints = new(Text);
        ErrorMarkers = new(Text);
        Color = new(Options, Text);
        Movement = new(Selection, Text);
        UndoStack = new(Text, Color, Options, Selection);
        Modify = new(Selection, Text, UndoStack, Options, Color);
        Renderer = new(this, Palettes.Dark)
        {
            KeyboardInput = new StandardKeyboardInput(this),
            MouseInput = new StandardMouseInput(this),
        };
    }

    /// <summary>Gets the total number of lines in the text editor, excluding the last empty line.</summary>
    public int TotalLines => Text.LineCount;

    /// <summary>Gets or sets the complete text content of the editor, including all lines.</summary>
    public string AllText
    {
        get => Text.GetText((0, 0), (Text.LineCount, 0));
        set => Text.SetText(value);
    }

    /// <summary>Gets or sets the lines of text in the editor.</summary>
    public IList<string> TextLines
    {
        get => Text.TextLines;
        set => Text.TextLines = value;
    }

    /// <summary>Appends a line of text to the end of the editor.</summary>
    public void AppendLine(string text)
    {
        UndoStack.Clear();
        Text.InsertLine(Text.LineCount - 1, text);
    }

    /// <summary>Appends a line of text with a specific color to the end of the editor.</summary>
    public void AppendLine(string text, PaletteIndex color)
    {
        UndoStack.Clear();
        Text.InsertLine(Text.LineCount - 1, text, color);
    }

    /// <summary>Appends a span of text to the end of the editor.</summary>
    public void Append(ReadOnlySpan<char> text, PaletteIndex color) => Text.Append(text, color);

    /// <summary>Appends a line of text represented by a <see cref="Line"/> object to the end of the editor.</summary>
    public void AppendLine(Line line)
    {
        UndoStack.Clear();
        Text.InsertLine(Text.LineCount - 1, line);
    }

    /// <summary>Gets or sets the syntax highlighter used for syntax highlighting in the text editor.</summary>
    public ISyntaxHighlighter SyntaxHighlighter
    {
        get => Color.SyntaxHighlighter;
        set => Color.SyntaxHighlighter = value;
    }

    /// <summary>Sets the color for a specific palette index in the text editor.</summary>
    public void SetColor(PaletteIndex color, uint abgr) => Renderer.SetColor(color, abgr);

    /// <summary>Gets or sets the tab size, which determines the number of spaces used for a tab character.</summary>
    public int TabSize
    {
        get => Text.TabSize;
        set => Text.TabSize = value;
    }

    /// <summary>Gets the text of the current line where the cursor is located.</summary>
    public string GetCurrentLineText()
    {
        var lineLength = Text.GetLineMaxColumn(Selection.Cursor.Line);
        return Text.GetText((Selection.Cursor.Line, 0), (Selection.Cursor.Line, lineLength));
    }

    /// <summary>Gets or sets the current cursor position in the text editor.</summary>
    public Coordinates CursorPosition
    {
        get => Selection.GetActualCursorCoordinates();
        set
        {
            if (Selection.Cursor == value)
                return;

            Selection.Cursor = value;
            Selection.Select(value, value);
            ScrollToLine(value.Line);
        }
    }

    /// <summary>Renders the text editor with the specified title and size.</summary>
    public void Render(string title, Vector2 size = new()) => Renderer.Render(title, size);

    /// <summary>Undoes the last action in the text editor, allowing for reverting changes made to the text.</summary>
    public void Undo() => UndoStack.Undo();

    /// <summary>Redoes the last undone action in the text editor, allowing for reapplying changes that were previously undone.</summary>
    public void Redo() => UndoStack.Redo();

    /// <summary>Gets the number of actions that can be undone in the text editor.</summary>
    public int UndoCount => UndoStack.UndoCount;

    /// <summary>Gets the index of the current undo action in the undo stack.</summary>
    public int UndoIndex => UndoStack.UndoIndex;

    /// <summary>Serializes the current state of the text editor, including options, selection, breakpoints, error markers, and text lines, to a JSON string.</summary>
    public string SerializeState()
    {
        var state = new
        {
            Options,
            Selection = Selection.SerializeState(),
            Breakpoints = Breakpoints.SerializeState(),
            ErrorMarkers = ErrorMarkers.SerializeState(),
            Text = TextLines,
        };

        return JsonSerializer.Serialize(state, new JsonSerializerOptions { WriteIndented = true });
    }

    /// <summary>Scrolls the text editor to a specific line number, making it visible in the viewport.</summary>
    public void ScrollToLine(int lineNumber) => Text.PendingScrollRequest = lineNumber;
}
