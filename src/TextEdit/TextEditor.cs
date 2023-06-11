using System.Collections.Generic;
using System.Numerics;
using System.Text.Json;
using ImGuiColorTextEditNet.Editor;

// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedMember.Global
// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace ImGuiColorTextEditNet;

public class TextEditor
{
    internal TextEditorUndoStack UndoStack { get; }
    internal TextEditorColor Color { get; }
    internal TextEditorText Text { get; }
    public TextEditorSelection Selection { get; }
    public TextEditorOptions Options { get; }
    public TextEditorBreakpoints Breakpoints { get; }
    public TextEditorErrorMarkers ErrorMarkers { get; }
    public TextEditorRenderer Renderer { get; }
    public TextEditorModify Modify { get; }
    public TextEditorMovement Movement { get; }

    public TextEditor()
    {
        Options = new TextEditorOptions();
        Text = new TextEditorText(Options);
        Selection = new TextEditorSelection(Text);
        Breakpoints = new TextEditorBreakpoints(Text);
        ErrorMarkers =  new TextEditorErrorMarkers(Text);
        Color = new TextEditorColor(Options, Text);
        Movement = new TextEditorMovement(Selection, Text);
        UndoStack = new TextEditorUndoStack(Text, Color, Options, Selection);
        Modify = new TextEditorModify(Selection, Text, UndoStack, Options, Color);
        Renderer = new TextEditorRenderer(this, Palettes.Dark)
        {
            KeyboardInput = new StandardKeyboardInput(this),
            MouseInput = new StandardMouseInput(this)
        };
    }

    public int TotalLines => Text.LineCount;
    public string AllText { get => Text.GetText((0, 0), (Text.LineCount, 0)); set => Text.SetText(value); }
    public IList<string> TextLines { get => Text.TextLines; set => Text.TextLines = value; }

    public void AppendLine(string text)
    {
        UndoStack.Clear();
        Text.InsertLine(Text.LineCount - 1, text);
    }

    public void AppendLine(string text, PaletteIndex color)
    {
        UndoStack.Clear();
        UndoStack.Clear();
        Text.InsertLine(Text.LineCount - 1, text, color);
    }

    public ISyntaxHighlighter SyntaxHighlighter { get => Color.SyntaxHighlighter; set => Color.SyntaxHighlighter = value; }
    public void SetColor(PaletteIndex color, uint abgr) => Renderer.SetColor(color, abgr);
    public int TabSize { get => Text.TabSize; set => Text.TabSize = value; }

    public string GetCurrentLineText()
    {
        var lineLength = Text.GetLineMaxColumn(Selection.Cursor.Line);
        return Text.GetText(
                (Selection.Cursor.Line, 0),
                (Selection.Cursor.Line, lineLength));
    }

    public Coordinates CursorPosition
    {
        get => Selection.GetActualCursorCoordinates();
        set
        {
            if (Selection.Cursor == value) return;
            Selection.Cursor = value;
            Text.ScrollToCursor = true;
        }
    }

    public void Render(string title, Vector2 size = new(), bool showBorder = false)
        => Renderer.Render(title, size, showBorder);
    public void Undo() => UndoStack.Undo();
    public void Redo() => UndoStack.Redo();
    public int UndoCount => UndoStack.UndoCount;
    public int UndoIndex => UndoStack.UndoIndex;

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
}

