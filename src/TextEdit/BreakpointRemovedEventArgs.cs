using System;

namespace ImGuiColorTextEditNet;

/// <summary>Event arguments for when a breakpoint is removed.</summary>
public class BreakpointRemovedEventArgs : EventArgs
{
    /// <summary>Initializes a new instance of the <see cref="BreakpointRemovedEventArgs"/> class with the specified context.</summary>
    public BreakpointRemovedEventArgs(object context) => Context = context;

    /// <summary>Gets the context associated with the breakpoint removal event.</summary>
    public object Context { get; }
}