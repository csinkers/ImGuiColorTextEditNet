using System;
using System.Collections.Generic;
using ImGuiNET;

namespace ImGuiColorTextEditNet.Input;

/// <summary>Represents the standard keyboard input handling.</summary>
public class StandardKeyboardInput : ITextEditorKeyboardInput
{
    readonly TextEditor _editor;
    readonly Dictionary<EditorKeybind, (object? Context, EditorKeybindAction Action)> _bindings =
    [];

    /// <summary>Initializes a new instance of the <see cref="StandardKeyboardInput"/> class.</summary>
    public StandardKeyboardInput(TextEditor editor)
    {
        _editor = editor ?? throw new ArgumentNullException(nameof(editor));
        AddReadOnlyBinding("Ctrl + Z", e => e.Undo());
        AddReadOnlyBinding("Ctrl + Y", e => e.Redo());
        AddReadOnlyBinding("Delete", e => e.Modify.Delete());
        AddReadOnlyBinding("Backspace", e => e.Modify.Backspace());
        AddReadOnlyBinding("Ctrl + V", e => e.Modify.Paste());
        AddReadOnlyBinding("Ctrl + X", e => e.Modify.Cut());
        AddReadOnlyBinding("Enter", e => e.Modify.EnterCharacter('\n'));
        AddReadOnlyBinding("Tab", e => Indent(false, e));
        AddReadOnlyBinding("Shift + Tab", e => Indent(true, e));

        AddReadOnlyBinding(
            "CapsLock",
            e =>
            {
                if (ColemakMode)
                    e.Modify.Backspace();
            }
        );

        AddMutatingBinding("UpArrow", e => e.Movement.MoveUp());
        AddMutatingBinding("Shift + UpArrow", e => e.Movement.MoveUp(1, true));

        AddMutatingBinding("DownArrow", e => e.Movement.MoveDown());
        AddMutatingBinding("Shift + DownArrow", e => e.Movement.MoveDown(1, true));

        AddMutatingBinding("LeftArrow", e => e.Movement.MoveLeft());
        AddMutatingBinding("Shift + LeftArrow", e => e.Movement.MoveLeft(1, true));
        AddMutatingBinding("Ctrl + LeftArrow", e => e.Movement.MoveLeft(1, false, true));
        AddMutatingBinding("Ctrl + Shift + LeftArrow", e => e.Movement.MoveLeft(1, true, true));

        AddMutatingBinding("RightArrow", e => e.Movement.MoveRight());
        AddMutatingBinding("Shift + RightArrow", e => e.Movement.MoveRight(1, true));
        AddMutatingBinding("Ctrl + RightArrow", e => e.Movement.MoveRight(1, false, true));
        AddMutatingBinding("Ctrl + Shift + RightArrow", e => e.Movement.MoveRight(1, true, true));

        AddMutatingBinding("PageUp", e => e.Movement.MoveUp(e.Renderer.PageSize - 4));
        AddMutatingBinding("Shift + PageUp", e => e.Movement.MoveUp(e.Renderer.PageSize - 4, true));
        AddMutatingBinding("PageDown", e => e.Movement.MoveDown(e.Renderer.PageSize - 4));
        AddMutatingBinding(
            "Shift + PageDown",
            e => e.Movement.MoveDown(e.Renderer.PageSize - 4, true)
        );
        AddMutatingBinding("Home", e => e.Movement.MoveToStartOfLine());
        AddMutatingBinding("End", e => e.Movement.MoveToEndOfLine());
        AddMutatingBinding("Ctrl + Home", e => e.Movement.MoveToStartOfFile());
        AddMutatingBinding("Ctrl + Shift + Home", e => e.Movement.MoveToStartOfFile(true));
        AddMutatingBinding("Ctrl + End", e => e.Movement.MoveToEndOfFile());
        AddMutatingBinding("Ctrl + Shift + End", e => e.Movement.MoveToEndOfFile(true));
        AddMutatingBinding("Insert", e => e.Options.IsOverwrite = !e.Options.IsOverwrite);
        AddMutatingBinding("Ctrl + C", e => e.Modify.Copy());
        AddMutatingBinding("Ctrl + A", e => e.Selection.SelectAll());

        return;

        void Indent(bool shifted, TextEditor e)
        {
            if (e.Selection.HasSelection && e.Selection.Start.Line != e.Selection.End.Line)
                e.Modify.IndentSelection(shifted);
            else
                e.Modify.EnterCharacter('\t');
        }
    }

    /// <summary>
    /// Removes all key-bindings
    /// </summary>
    public void ClearBindings() => _bindings.Clear();

    /// <summary>
    /// Adds a key binding to the text editor with an associated action and context.
    /// </summary>
    public void AddBinding(EditorKeybind binding, object? context, EditorKeybindAction action) =>
        _bindings[binding] = (context, action);

    /// <summary>
    /// Adds a key binding to the text editor with an associated action and context.
    /// </summary>
    public void AddBinding(string binding, object? context, EditorKeybindAction action)
    {
        if (!EditorKeybind.TryParse(binding, out var keybind))
            throw new ArgumentException($"Invalid key binding: {binding}", nameof(binding));

        AddBinding(keybind, context, action);
    }

    /// <summary>
    /// Adds a key binding to the text editor with an associated action and context.
    /// </summary>
    public void AddBinding(string binding, EditorKeybindAction action) =>
        AddBinding(binding, null, action);

    /// <summary>
    /// Adds a simple key binding to the text editor that executes an action without context and is safe to run on a read-only editor.
    /// </summary>
    public void AddReadOnlyBinding(string binding, Action<TextEditor> action) =>
        AddBinding(
            binding,
            (editor, _) =>
            {
                action(editor);
                return true;
            }
        );

    /// <summary>
    /// Adds a read-only key binding to the text editor that executes an action only if the editor is not in read-only mode.
    /// </summary>
    public void AddMutatingBinding(string binding, Action<TextEditor> action) =>
        AddBinding(
            binding,
            (editor, _) =>
            {
                if (editor.Options.IsReadOnly)
                    return false;

                action(editor);
                return true;
            }
        );

    /// <summary>Gets or sets a value indicating whether Colemak keyboard layout mode is enabled.</summary>
    public bool ColemakMode { get; set; } = false;

    /// <summary>Handles keyboard inputs for the text editor.</summary>
    public void HandleKeyboardInputs()
    {
        if (!ImGui.IsWindowFocused())
            return;

        var io = ImGui.GetIO();
        var shift = io.KeyShift;
        var ctrl = io.ConfigMacOSXBehaviors ? io.KeySuper : io.KeyCtrl;

        io.WantCaptureKeyboard = true;
        io.WantTextInput = true;

        foreach (var (binding, value) in _bindings)
            if (binding.Ctrl == ctrl && binding.Shift == shift && ImGui.IsKeyPressed(binding.Key))
                if (value.Action(_editor, value.Context))
                    break;

        if (!_editor.Options.IsReadOnly && io.InputQueueCharacters.Size != 0)
        {
            for (int i = 0; i < io.InputQueueCharacters.Size; i++)
            {
                var c = io.InputQueueCharacters[i];
                if (c != 0 && c is '\n' or >= 32)
                    _editor.Modify.EnterCharacter((char)c);
            }
        }
    }
}
