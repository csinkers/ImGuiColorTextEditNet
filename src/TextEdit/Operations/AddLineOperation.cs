namespace ImGuiColorTextEditNet.Operations;

internal class AddLineOperation : IEditorOperation
{
    public int InsertBeforeLine;
    public string Added = "";

    public void Apply(TextEditor editor)
    {
        editor.Text.InsertLine(InsertBeforeLine, Added);
        editor.Color.InvalidateColor(InsertBeforeLine - 1, 2);
    }

    public void Undo(TextEditor editor)
    {
        editor.Text.RemoveLine(InsertBeforeLine);
        editor.Color.InvalidateColor(InsertBeforeLine - 1, 2);
    }

    public object SerializeState()
    {
        return new { InsertBeforeLine, Added };
    }
}
