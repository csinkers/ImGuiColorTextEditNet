namespace ImGuiColorTextEditNet;
struct EditorState
{
    public Coordinates SelectionStart;
    public Coordinates SelectionEnd;
    public Coordinates CursorPosition;
    public override string ToString() => $"SEL [{SelectionStart}-{SelectionEnd}] CUR {CursorPosition}";
}