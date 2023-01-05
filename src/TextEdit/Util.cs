using System.Runtime.CompilerServices;

namespace ImGuiColorTextEditNet;

public static class Util
{
    public delegate void AssertionFailedHandler(string? expression, string? file, int line, string? method);
    public static event AssertionFailedHandler AssertionFailed;

    public static void Assert(bool condition,
        [CallerArgumentExpression("condition")] string? expression = null,
        [CallerFilePath] string? file = null,
        [CallerLineNumber] int line = 0,
        [CallerMemberName] string? method = null)
    {
        if (!condition)
            AssertionFailed?.Invoke(expression, file, line, method);
    }
}