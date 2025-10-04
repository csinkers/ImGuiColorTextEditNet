using System.Text;

namespace ImGuiColorTextEditNet.Operations;

internal class ModifyLineOperation : IEditorOperation
{
    public int Line;
    public int AddedColumn = 0;
    public string Added = "";
    public int RemovedColumn = 0;
    public string Removed = "";

    public void Apply(TextEditor editor)
    {
        if (!string.IsNullOrEmpty(Removed))
            editor.Text.RemoveInLine(Line, RemovedColumn, RemovedColumn + Removed.Length);

        if (!string.IsNullOrEmpty(Added))
            editor.Text.InsertTextAt((Line, AddedColumn), Added);

        editor.Color.InvalidateColor(Line - 1, 1);
    }

    public void Undo(TextEditor editor)
    {
        if (!string.IsNullOrEmpty(Added))
            editor.Text.RemoveInLine(Line, AddedColumn, AddedColumn + Added.Length);

        if (!string.IsNullOrEmpty(Removed))
            editor.Text.InsertTextAt((Line, RemovedColumn), Removed);

        editor.Color.InvalidateColor(Line - 1, 1);
    }

    public object SerializeState() =>
        new
        {
            Line,
            AddedColumn,
            Added,
            RemovedColumn,
            Removed,
        };

    public override string ToString()
    {
        var sb = new StringBuilder();
        sb.Append($"Mod{Line}:");

        if (!string.IsNullOrEmpty(Removed))
        {
            sb.Append("-\"");
            sb.Append(Removed);
            sb.Append("\" @ ");
            sb.Append(RemovedColumn);
        }

        if (!string.IsNullOrEmpty(Added))
        {
            sb.Append("+\"");
            sb.Append(Added);
            sb.Append("\" @ ");
            sb.Append(AddedColumn);
        }

        return sb.ToString();
    }
}
