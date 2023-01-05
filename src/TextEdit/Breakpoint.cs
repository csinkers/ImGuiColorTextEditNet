namespace ImGuiColorTextEditNet;
struct Breakpoint
{
    public int Line = -1;
    public bool Enabled = false;
    public string? Condition = null;
    public Breakpoint() { }
    public override string ToString() => $"BP: {Line}:{Condition} ({Enabled})";
}