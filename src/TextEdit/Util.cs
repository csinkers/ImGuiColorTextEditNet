using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace ImGuiColorTextEditNet;

public static class Util
{
    public delegate void AssertionHandlerDelegate(bool condition, string? expression, string? file, int line, string? method);
    public static AssertionHandlerDelegate AssertionHandler { get; set; } = DefaultHandler;
    static void DefaultHandler(bool condition, string? expression, string? file, int line, string? method) 
        => Debug.Assert(expression != null);

    public static void Assert(bool condition,
        [CallerArgumentExpression("condition")] string? expression = null,
        [CallerFilePath] string? file = null,
        [CallerLineNumber] int line = 0,
        [CallerMemberName] string? method = null) => AssertionHandler(condition, expression, file, line, method);
}