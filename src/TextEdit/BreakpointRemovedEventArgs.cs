using System;

namespace ImGuiColorTextEditNet;

public class BreakpointRemovedEventArgs : EventArgs
{
    public BreakpointRemovedEventArgs(object context) => Context = context;
    public object Context { get; }
}