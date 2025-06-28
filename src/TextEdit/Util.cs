using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace ImGuiColorTextEditNet;

/// <summary>
/// Utility class for assertion handling.
/// </summary>
public static class Util
{
    /// <summary>
    /// Delegate for assertion handlers.
    /// </summary>
    public delegate void AssertionHandlerDelegate(
        bool condition,
        string? expression,
        string? file,
        int line,
        string? method
    );

    /// <summary>
    /// The active assertion handler
    /// </summary>
    public static AssertionHandlerDelegate AssertionHandler { get; set; } = DefaultHandler;

    static void DefaultHandler(
        bool condition,
        string? expression,
        string? file,
        int line,
        string? method
    ) => Debug.Assert(expression != null);

    /// <summary>
    /// Asserts that a condition is true by calling the AssertionHandler delegate.
    /// </summary>
    public static void Assert(
        bool condition,
        [CallerArgumentExpression("condition")] string? expression = null,
        [CallerFilePath] string? file = null,
        [CallerLineNumber] int line = 0,
        [CallerMemberName] string? method = null
    ) => AssertionHandler(condition, expression, file, line, method);
}
