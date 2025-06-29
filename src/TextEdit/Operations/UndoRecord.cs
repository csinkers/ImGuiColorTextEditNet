using System.Text;

namespace ImGuiColorTextEditNet.Operations;

internal class UndoRecord : IEditorOperation
{
    public string? Added;
    public Coordinates AddedStart;
    public Coordinates AddedEnd;

    public string? Removed;
    public Coordinates RemovedStart;
    public Coordinates RemovedEnd;

    public SelectionState Before;
    public SelectionState After;

    public override string ToString()
    {
        var sb = new StringBuilder();
        if (Added != null)
        {
            sb.Append("+\"");
            sb.Append(Added);
            sb.Append("\" @ ");
            sb.Append(AddedStart);
        }

        if (Removed != null)
        {
            if (sb.Length > 0)
                sb.Append(' ');
            sb.Append("-\"");
            sb.Append(Removed);
            sb.Append("\" @ ");
            sb.Append(RemovedStart);
        }

        sb.Append(' ');
        sb.Append(Before);
        sb.Append(" => ");
        sb.Append(After);

        return sb.ToString();
    }

    public object SerializeState() =>
        new
        {
            Added,
            AddedStart = AddedStart.ToString(),
            AddedEnd = AddedEnd.ToString(),

            Removed,
            RemovedStart = RemovedStart.ToString(),
            RemovedEnd = RemovedEnd.ToString(),

            Before = Before.ToString(),
            After = After.ToString(),
        };

    public void Apply(TextEditor editor)
    {
        if (!string.IsNullOrEmpty(Removed))
        {
            editor.Text.DeleteRange(RemovedStart, RemovedEnd);
            editor.Color.InvalidateColor(
                RemovedStart.Line - 1,
                RemovedEnd.Line - RemovedStart.Line + 1
            );
        }

        if (!string.IsNullOrEmpty(Added))
        {
            var start = AddedStart;
            editor.Text.InsertTextAt(start, Added);
            editor.Color.InvalidateColor(AddedStart.Line - 1, AddedEnd.Line - AddedStart.Line + 1);
        }

        editor.Selection.Select(After.Start, After.End);
        editor.Selection.Cursor = After.Cursor;
        editor.Text.PendingScrollRequest = editor.Selection.Cursor.Line;
    }

    public void Undo(TextEditor editor)
    {
        if (!string.IsNullOrEmpty(Added))
        {
            editor.Text.DeleteRange(AddedStart, AddedEnd);
            editor.Color.InvalidateColor(AddedStart.Line - 1, AddedEnd.Line - AddedStart.Line + 2);
        }

        if (!string.IsNullOrEmpty(Removed))
        {
            var start = RemovedStart;
            editor.Text.InsertTextAt(start, Removed);

            editor.Color.InvalidateColor(
                RemovedStart.Line - 1,
                RemovedEnd.Line - RemovedStart.Line + 2
            );
        }

        editor.Selection.Select(Before.Start, Before.End);
        editor.Selection.Cursor = Before.Cursor;
        editor.Text.PendingScrollRequest = editor.Selection.Cursor.Line;
    }
}
