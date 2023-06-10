using System.Text;

namespace ImGuiColorTextEditNet;

class UndoRecord
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

    public UndoRecord() { }
    public UndoRecord(
        string added, Coordinates addedStart, Coordinates addedEnd,
        string removed, Coordinates removedStart, Coordinates removedEnd,
        SelectionState before, SelectionState after)
    {
        Util.Assert(AddedStart <= AddedEnd);
        Util.Assert(RemovedStart <= RemovedEnd);

        Added = added;
        AddedStart = addedStart;
        AddedEnd = addedEnd;

        Removed = removed;
        RemovedStart = removedStart;
        RemovedEnd = removedEnd;

        Before = before;
        After = after;
    }
}