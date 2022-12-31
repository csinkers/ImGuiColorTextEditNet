namespace ImGuiColorTextEditNet;

class UndoRecord
{
    public string? Added;
    public Coordinates AddedStart;
    public Coordinates AddedEnd;

    public string? Removed;
    public Coordinates RemovedStart;
    public Coordinates RemovedEnd;

    public EditorState Before;
    public EditorState After;

    public UndoRecord() { }
    public UndoRecord(
        string added, Coordinates addedStart, Coordinates addedEnd,
        string removed, Coordinates removedStart, Coordinates removedEnd,
        EditorState before, EditorState after)
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

    public void Undo(TextEditor aEditor)
    {
        if (!string.IsNullOrEmpty(Added))
        {
            aEditor.DeleteRange(AddedStart, AddedEnd);
            aEditor.Colorize(AddedStart.Line - 1, AddedEnd.Line - AddedStart.Line + 2);
        }

        if (!string.IsNullOrEmpty(Removed))
        {
            var start = RemovedStart;
            aEditor.InsertTextAt(start, Removed);
            aEditor.Colorize(RemovedStart.Line - 1, RemovedEnd.Line - RemovedStart.Line + 2);
        }

        aEditor._state = Before;
        aEditor.EnsureCursorVisible();

    }

    public void Redo(TextEditor aEditor)
    {
        if (!string.IsNullOrEmpty(Removed))
        {
            aEditor.DeleteRange(RemovedStart, RemovedEnd);
            aEditor.Colorize(RemovedStart.Line - 1, RemovedEnd.Line - RemovedStart.Line + 1);
        }

        if (!string.IsNullOrEmpty(Added))
        {
            var start = AddedStart;
            aEditor.InsertTextAt(start, Added);
            aEditor.Colorize(AddedStart.Line - 1, AddedEnd.Line - AddedStart.Line + 1);
        }

        aEditor._state = After;
        aEditor.EnsureCursorVisible();
    }

    /*
    template<class InputIt1, class InputIt2, class BinaryPredicate>
    bool equals(InputIt1 first1, InputIt1 last1,
        InputIt2 first2, InputIt2 last2, BinaryPredicate p)
    {
        for (; first1 != last1 && first2 != last2; ++first1, ++first2)
        {
            if (!p(*first1, *first2))
                return false;
        }
        return first1 == last1 && first2 == last2;
    } */
}