namespace ImGuiColorTextEditNet;
struct SelectionState
{
    public Coordinates Start;
    public Coordinates End;
    public Coordinates Cursor;
    public override string ToString() => $"SEL [{Start}-{End}] CUR {Cursor}";
}