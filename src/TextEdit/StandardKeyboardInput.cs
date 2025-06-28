using System;
using ImGuiNET;

namespace ImGuiColorTextEditNet;

/// <summary>Represents the standard keyboard input handling.</summary>
public class StandardKeyboardInput : ITextEditorKeyboardInput
{
    readonly TextEditor _editor;

    /// <summary>Initializes a new instance of the <see cref="StandardKeyboardInput"/> class.</summary>
    public StandardKeyboardInput(TextEditor editor) =>
        _editor = editor ?? throw new ArgumentNullException(nameof(editor));

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

        if (!_editor.Options.IsReadOnly)
        {
            switch (ctrl, shift)
            {
                case (true, false) when ImGui.IsKeyPressed(ImGuiKey.Z):
                    _editor.UndoStack.Undo();
                    break;

                case (true, false) when ImGui.IsKeyPressed(ImGuiKey.Y):
                    _editor.UndoStack.Redo();
                    break;

                case (false, false) when ImGui.IsKeyPressed(ImGuiKey.Delete):
                    _editor.Modify.Delete();
                    break;

                case (false, false) when ImGui.IsKeyPressed(ImGuiKey.Backspace):
                    _editor.Modify.Backspace();
                    break;

                case (false, false) when ImGui.IsKeyPressed(ImGuiKey.CapsLock):
                    if (ColemakMode)
                        _editor.Modify.Backspace();
                    break;

                case (true, false) when ImGui.IsKeyPressed(ImGuiKey.V):
                    _editor.Modify.Paste();
                    break;

                case (true, false) when ImGui.IsKeyPressed(ImGuiKey.X):
                    _editor.Modify.Cut();
                    break;

                case (false, false) when ImGui.IsKeyPressed(ImGuiKey.Enter):
                    _editor.Modify.EnterCharacter('\n');
                    break;

                case (false, _) when ImGui.IsKeyPressed(ImGuiKey.Tab):
                {
                    if (
                        _editor.Selection.HasSelection
                        && _editor.Selection.Start.Line != _editor.Selection.End.Line
                    )
                    {
                        _editor.Modify.IndentSelection(shift);
                    }
                    else
                    {
                        _editor.Modify.EnterCharacter('\t');
                    }
                    break;
                }
            }
        }

        switch (ctrl, shift)
        {
            case (false, _) when ImGui.IsKeyPressed(ImGuiKey.UpArrow):
                _editor.Movement.MoveUp(1, shift);
                break;
            case (false, _) when ImGui.IsKeyPressed(ImGuiKey.DownArrow):
                _editor.Movement.MoveDown(1, shift);
                break;
            case (_, _) when ImGui.IsKeyPressed(ImGuiKey.LeftArrow):
                _editor.Movement.MoveLeft(1, shift, ctrl);
                break;
            case (_, _) when ImGui.IsKeyPressed(ImGuiKey.RightArrow):
                _editor.Movement.MoveRight(1, shift, ctrl);
                break;
            case (_, _) when ImGui.IsKeyPressed(ImGuiKey.PageUp):
                _editor.Movement.MoveUp(_editor.Renderer.PageSize - 4, shift);
                break;
            case (_, _) when ImGui.IsKeyPressed(ImGuiKey.PageDown):
                _editor.Movement.MoveDown(_editor.Renderer.PageSize - 4, shift);
                break;
            case (true, _) when ImGui.IsKeyPressed(ImGuiKey.Home):
                _editor.Movement.MoveToStartOfFile(shift);
                break;
            case (true, _) when ImGui.IsKeyPressed(ImGuiKey.End):
                _editor.Movement.MoveToEndOfFile(shift);
                break;
            case (false, _) when ImGui.IsKeyPressed(ImGuiKey.Home):
                _editor.Movement.MoveToStartOfLine(shift);
                break;
            case (false, _) when ImGui.IsKeyPressed(ImGuiKey.End):
                _editor.Movement.MoveToEndOfLine(shift);
                break;
            case (false, false) when ImGui.IsKeyPressed(ImGuiKey.Insert):
                _editor.Options.IsOverwrite = !_editor.Options.IsOverwrite;
                break;
            case (true, false) when ImGui.IsKeyPressed(ImGuiKey.C):
                _editor.Modify.Copy();
                break;
            case (true, false) when ImGui.IsKeyPressed(ImGuiKey.A):
                _editor.Selection.SelectAll();
                break;
        }

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
