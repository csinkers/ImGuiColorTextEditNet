using System;
using ImGuiNET;

namespace ImGuiColorTextEditNet;

public class StandardMouseInput : ITextEditorMouseInput
{
    readonly TextEditor _editor;

    public StandardMouseInput(TextEditor editor) 
        => _editor = editor ?? throw new ArgumentNullException(nameof(editor));

    public void HandleMouseInputs()
    {
        var io = ImGui.GetIO();
        var shift = io.KeyShift;
        var ctrl = io.ConfigMacOSXBehaviors ? io.KeySuper : io.KeyCtrl;
        var alt = io.ConfigMacOSXBehaviors ? io.KeyCtrl : io.KeyAlt;

        if (!ImGui.IsWindowHovered())
            return;

        if (shift || alt)
            return;

        var click = ImGui.IsMouseClicked(0);
        var doubleClick = ImGui.IsMouseDoubleClicked(0);

        /* Left mouse button double click */
        if (doubleClick)
        {
            if (!ctrl)
            {
                _editor.Selection.Cursor = _editor.Selection.InteractiveStart = _editor.Selection.InteractiveEnd = _editor.Renderer.ScreenPosToCoordinates(ImGui.GetMousePos());
                _editor.Selection.Mode = _editor.Selection.Mode == SelectionMode.Line ? SelectionMode.Normal : SelectionMode.Word;
                _editor.Selection.Select(_editor.Selection.InteractiveStart, _editor.Selection.InteractiveEnd, _editor.Selection.Mode);
            }
        }
        else if (click) /* Left mouse button click */
        {
            _editor.Selection.Cursor = _editor.Selection.InteractiveStart = _editor.Selection.InteractiveEnd = _editor.Renderer.ScreenPosToCoordinates(ImGui.GetMousePos());
            _editor.Selection.Mode = ctrl ? SelectionMode.Word : SelectionMode.Normal;
            _editor.Selection.Select(_editor.Selection.InteractiveStart, _editor.Selection.InteractiveEnd, _editor.Selection.Mode);
        }
        else if (ImGui.IsMouseDragging(0) && ImGui.IsMouseDown(0)) // Mouse left button dragging (=> update selection)
        {
            io.WantCaptureMouse = true;
            _editor.Selection.Cursor = _editor.Selection.InteractiveEnd = _editor.Renderer.ScreenPosToCoordinates(ImGui.GetMousePos());
            _editor.Selection.Select(_editor.Selection.InteractiveStart, _editor.Selection.InteractiveEnd, _editor.Selection.Mode);
        }
    }
}