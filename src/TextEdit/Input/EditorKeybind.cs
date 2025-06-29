using System;
using ImGuiNET;

namespace ImGuiColorTextEditNet.Input;

/// <summary>
/// Details of an editor key-bind
/// </summary>
public record struct EditorKeybind(bool Shift, bool Ctrl, ImGuiKey Key)
{
    /// <summary>
    /// Parses a string representation of a key-bind, e.g. Ctrl+S.
    /// </summary>
    public static bool TryParse(string s, out EditorKeybind result)
    {
        bool shift = false;
        bool ctrl = false;
        ImGuiKey? key = null;

        var parts = s.Split(['+', ' '], StringSplitOptions.RemoveEmptyEntries);
        foreach (var part in parts)
        {
            var p = part.Trim();
            switch (p.ToLowerInvariant())
            {
                case "ctrl":
                case "control":
                    ctrl = true;
                    break;

                case "shift":
                    shift = true;
                    break;

                default:
                    if (key != null)
                    {
                        result = default;
                        return false;
                    }
                    if (p.Length == 1) // Try to parse as a single character (A-Z, 0-9)
                    {
                        char c = char.ToUpperInvariant(p[0]);
                        if (c is >= 'A' and <= 'Z')
                            key = ImGuiKey.A + (c - 'A');
                        else if (c is >= '0' and <= '9')
                            key = ImGuiKey._0 + (c - '0');
                    }

                    // Try to parse as a named key
                    if (Enum.TryParse<ImGuiKey>(p, ignoreCase: true, out var temp))
                        key = temp;

                    break;
            }
        }

        if (key == null)
        {
            result = default;
            return false;
        }

        result = new EditorKeybind(shift, ctrl, key.Value);
        return true;
    }
}
