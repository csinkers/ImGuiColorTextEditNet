using System;
using ImGuiNET;

namespace ImGuiColorTextEditNet;

public class StandardKeyboardInput : ITextEditorKeyboardInput
{
    readonly TextEditor _editor;
    public StandardKeyboardInput(TextEditor editor) => _editor = editor ?? throw new ArgumentNullException(nameof(editor));
    public bool ColemakMode { get; set; } = true;

    public void HandleKeyboardInputs()
    {
        if (!ImGui.IsWindowFocused())
            return;

        var io = ImGui.GetIO();
        var shift = io.KeyShift;
        var ctrl = io.ConfigMacOSXBehaviors ? io.KeySuper : io.KeyCtrl;
        var alt = io.ConfigMacOSXBehaviors ? io.KeyCtrl : io.KeyAlt;

        io.WantCaptureKeyboard = true;
        io.WantTextInput = true;

        if (!_editor.Options.IsReadOnly)
        {
            switch (ctrl, shift, alt)
            {
                case (true, false, false)  when ImGui.IsKeyPressed(ImGui.GetKeyIndex(ImGuiKey.Z)): _editor.UndoStack.Undo(); break;
                case (true, false, false)  when ImGui.IsKeyPressed(ImGui.GetKeyIndex(ImGuiKey.Y)): _editor.UndoStack.Redo(); break;
                case (false, false, false) when ImGui.IsKeyPressed(ImGui.GetKeyIndex(ImGuiKey.Delete)): _editor.Modify.Delete(); break;
                case (false, false, false) when ImGui.IsKeyPressed(ImGui.GetKeyIndex(ImGuiKey.Backspace)): _editor.Modify.Backspace(); break;
                case (false, false, false) when ImGui.IsKeyPressed(ImGui.GetKeyIndex(ImGuiKey.CapsLock)): if (ColemakMode) _editor.Modify.Backspace(); break;
                case (true, false, false)  when ImGui.IsKeyPressed(ImGui.GetKeyIndex(ImGuiKey.V)): _editor.Modify.Paste(); break;
                case (true, false, false)  when ImGui.IsKeyPressed(ImGui.GetKeyIndex(ImGuiKey.X)): _editor.Modify.Cut(); break;
                case (false, false, false) when ImGui.IsKeyPressed(ImGui.GetKeyIndex(ImGuiKey.Enter)): _editor.Modify.EnterCharacter('\n'); break;
                case (false, _, false)     when ImGui.IsKeyPressed(ImGui.GetKeyIndex(ImGuiKey.Tab)):
                {
                    if (_editor.Selection.HasSelection && _editor.Selection.Start.Line != _editor.Selection.End.Line)
                        _editor.Modify.IndentSelection(shift);
                    else
                        _editor.Modify.EnterCharacter('\t');
                    break;
                }
            }
        }

        switch (ctrl, shift, alt)
        {
            case (false, _, false) when ImGui.IsKeyPressed(ImGui.GetKeyIndex(ImGuiKey.UpArrow)):    _editor.Movement.MoveUp(1, shift); break;
            case (false, _, false) when ImGui.IsKeyPressed(ImGui.GetKeyIndex(ImGuiKey.DownArrow)):  _editor.Movement.MoveDown(1, shift); break;
            case (_, _, false)     when ImGui.IsKeyPressed(ImGui.GetKeyIndex(ImGuiKey.LeftArrow)):  _editor.Movement.MoveLeft(1, shift, ctrl); break;
            case (_, _, false)     when ImGui.IsKeyPressed(ImGui.GetKeyIndex(ImGuiKey.RightArrow)): _editor.Movement.MoveRight(1, shift, ctrl); break;
            case (_, _, false)     when ImGui.IsKeyPressed(ImGui.GetKeyIndex(ImGuiKey.PageUp)):     _editor.Movement.MoveUp(_editor.Renderer.PageSize - 4, shift); break;
            case (_, _, false)     when ImGui.IsKeyPressed(ImGui.GetKeyIndex(ImGuiKey.PageDown)):   _editor.Movement.MoveDown(_editor.Renderer.PageSize - 4, shift); break;
            case (true, _, false)  when ImGui.IsKeyPressed(ImGui.GetKeyIndex(ImGuiKey.Home)):       _editor.Movement.MoveToStartOfFile(shift); break;
            case (true, _, false)  when ImGui.IsKeyPressed(ImGui.GetKeyIndex(ImGuiKey.End)):        _editor.Movement.MoveToEndOfFile(shift); break;
            case (false, _, false) when ImGui.IsKeyPressed(ImGui.GetKeyIndex(ImGuiKey.Home)):       _editor.Movement.MoveToStartOfLine(shift); break;
            case (false, _, false) when ImGui.IsKeyPressed(ImGui.GetKeyIndex(ImGuiKey.End)):        _editor.Movement.MoveToEndOfLine(shift); break;
            case (false, false, false) when ImGui.IsKeyPressed(ImGui.GetKeyIndex(ImGuiKey.Insert)): _editor.Options.IsOverwrite = !_editor.Options.IsOverwrite; break;
            case (true, false, false)  when ImGui.IsKeyPressed(ImGui.GetKeyIndex(ImGuiKey.C)):      _editor.Modify.Copy(); break;
            case (true, false, false)  when ImGui.IsKeyPressed(ImGui.GetKeyIndex(ImGuiKey.A)):      _editor.Selection.SelectAll(); break;
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