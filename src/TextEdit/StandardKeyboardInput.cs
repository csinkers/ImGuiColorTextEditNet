using ImGuiNET;

namespace ImGuiColorTextEditNet;

public class StandardKeyboardInput : ITextEditorKeyboardInput
{
    public static StandardKeyboardInput Instance { get; } = new();
    StandardKeyboardInput() { }

    public void HandleKeyboardInputs(TextEditor editor)
    {
        var io = ImGui.GetIO();
        var shift = io.KeyShift;
        var ctrl = io.ConfigMacOSXBehaviors ? io.KeySuper : io.KeyCtrl;
        var alt = io.ConfigMacOSXBehaviors ? io.KeyCtrl : io.KeyAlt;

        if (!ImGui.IsWindowFocused())
            return;

        if (ImGui.IsWindowHovered())
            ImGui.SetMouseCursor(ImGuiMouseCursor.TextInput);
        //ImGui.CaptureKeyboardFromApp(true);

        io.WantCaptureKeyboard = true;
        io.WantTextInput = true;

        if (!editor.IsReadOnly)
        {
            switch (ctrl, shift, alt)
            {
                case (true, false, false)  when ImGui.IsKeyPressed(ImGui.GetKeyIndex(ImGuiKey.Z)): editor.Undo(); break;
                case (true, false, false)  when ImGui.IsKeyPressed(ImGui.GetKeyIndex(ImGuiKey.Y)): editor.Redo(); break;
                case (false, false, false) when ImGui.IsKeyPressed(ImGui.GetKeyIndex(ImGuiKey.Delete)): editor.Delete(); break;
                case (false, false, false) when ImGui.IsKeyPressed(ImGui.GetKeyIndex(ImGuiKey.Backspace)): editor.Backspace(); break;
                case (true, false, false)  when ImGui.IsKeyPressed(ImGui.GetKeyIndex(ImGuiKey.V)): editor.Paste(); break;
                case (true, false, false)  when ImGui.IsKeyPressed(ImGui.GetKeyIndex(ImGuiKey.X)): editor.Cut(); break;
                case (false, false, false) when ImGui.IsKeyPressed(ImGui.GetKeyIndex(ImGuiKey.Enter)): editor.EnterCharacter('\n'); break;
                case (false, _, false)     when ImGui.IsKeyPressed(ImGui.GetKeyIndex(ImGuiKey.Tab)):
                {
                    if (editor.HasSelection && editor.SelectionStart.Line != editor.SelectionEnd.Line)
                        editor.IndentSelection(shift);
                    else
                        editor.EnterCharacter('\t');
                    break;
                }
            }
        }

        switch (ctrl, shift, alt)
        {
            case (false, _, false) when ImGui.IsKeyPressed(ImGui.GetKeyIndex(ImGuiKey.UpArrow)):    editor.MoveUp(1, shift); break;
            case (false, _, false) when ImGui.IsKeyPressed(ImGui.GetKeyIndex(ImGuiKey.DownArrow)):  editor.MoveDown(1, shift); break;
            case (_, _, false)     when ImGui.IsKeyPressed(ImGui.GetKeyIndex(ImGuiKey.LeftArrow)):  editor.MoveLeft(1, shift, ctrl); break;
            case (_, _, false)     when ImGui.IsKeyPressed(ImGui.GetKeyIndex(ImGuiKey.RightArrow)): editor.MoveRight(1, shift, ctrl); break;
            case (_, _, false)     when ImGui.IsKeyPressed(ImGui.GetKeyIndex(ImGuiKey.PageUp)):     editor.MoveUp(editor.PageSize - 4, shift); break;
            case (_, _, false)     when ImGui.IsKeyPressed(ImGui.GetKeyIndex(ImGuiKey.PageDown)):   editor.MoveDown(editor.PageSize - 4, shift); break;
            case (true, _, false)  when ImGui.IsKeyPressed(ImGui.GetKeyIndex(ImGuiKey.Home)):       editor.MoveTop(shift); break;
            case (true, _, false)  when ImGui.IsKeyPressed(ImGui.GetKeyIndex(ImGuiKey.End)):        editor.MoveBottom(shift); break;
            case (false, _, false) when ImGui.IsKeyPressed(ImGui.GetKeyIndex(ImGuiKey.Home)):       editor.MoveHome(shift); break;
            case (false, _, false) when ImGui.IsKeyPressed(ImGui.GetKeyIndex(ImGuiKey.End)):        editor.MoveEnd(shift); break;
            case (false, false, false) when ImGui.IsKeyPressed(ImGui.GetKeyIndex(ImGuiKey.Insert)): editor.IsOverwrite = !editor.IsOverwrite; break;
            case (true, false, false)  when ImGui.IsKeyPressed(ImGui.GetKeyIndex(ImGuiKey.C)):      editor.Copy(); break;
            case (true, false, false)  when ImGui.IsKeyPressed(ImGui.GetKeyIndex(ImGuiKey.A)):      editor.SelectAll(); break;
        }

        if (!editor.IsReadOnly && io.InputQueueCharacters.Size != 0)
        {
            for (int i = 0; i < io.InputQueueCharacters.Size; i++)
            {
                var c = io.InputQueueCharacters[i];
                if (c != 0 && c is '\n' or >= 32)
                    editor.EnterCharacter((char)c);
            }

            // io.InputQueueCharacters.resize(0); // TODO: Revisit
        }

        ImGui.PushAllowKeyboardFocus(true);
    }
}