namespace ImGuiColorTextEditNet.Operations;

internal class RemoveLineOperation : IEditorOperation
{
    public int Line;
    public string Removed = "";

    public void Apply(TextEditor editor)
    {
        editor.Text.RemoveLine(Line);
        editor.Color.InvalidateColor(Line - 1, 2);
    }

    public void Undo(TextEditor editor)
    {
        editor.Text.InsertLine(Line, Removed);
        editor.Color.InvalidateColor(Line - 1, 2);
    }

    public object SerializeState()
    {
        return new { Line, Removed };
    }
}
