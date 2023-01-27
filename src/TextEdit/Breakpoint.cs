namespace ImGuiColorTextEditNet
{
    readonly struct Breakpoint
    {
        public readonly int Line;
        public readonly bool Enabled;
        public readonly string? Condition;

        public Breakpoint(int line, bool enabled, string? condition)
        {
            Line = line;
            Enabled = enabled;
            Condition = condition;
        }

        public override string ToString() => $"BP: {Line}:{Condition} ({Enabled})";
    }
}