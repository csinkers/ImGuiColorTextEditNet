using System;

namespace ImGuiColorTextEditNet;

/// <summary>
/// Represents a character coordinate from the user's point of view,
/// i.e. consider a uniform grid (assuming fixed-width font) on the
/// screen as it is rendered, and each cell has its own coordinate, starting from 0.
/// Tabs are counted as between 1 and mTabSize empty spaces, depending on
/// how many spaces are necessary to reach the next tab stop.
/// For example, coordinate (1, 5) represents the character 'B' in a line "\tABC", when mTabSize = 4,
/// because it is rendered as "    ABC" on the screen.
/// </summary>
public struct Coordinates : IEquatable<Coordinates>
{
    /// <summary>The line number, starting from 0.</summary>
    public int Line;

    /// <summary>The column number, starting from 0.</summary>
    public int Column;

    /// <summary>Creates a new instance of Coordinates at (0,0)</summary>
    public Coordinates()
    {
        Line = 0;
        Column = 0;
    }

    /// <summary>Creates a new instance of Coordinates at the specified line and column.</summary>
    public Coordinates(int line, int column)
    {
        Util.Assert(line >= 0);
        Util.Assert(column >= 0);
        Line = line;
        Column = column;
    }

    /// <summary>Implicitly converts a tuple of (line, column) to Coordinates.</summary>
    public static implicit operator Coordinates((int Line, int Column) x) => new(x.Line, x.Column);

    /// <summary>
    /// Returns a string representation of the coordinates in the format "line:column".
    /// </summary>
    public override string ToString() => $"{Line}:{Column}";

    /// <summary>Represents an invalid coordinate, which is used to indicate that a coordinate is not valid or has not been set.</summary>
    public static Coordinates Invalid => new() { Line = -1, Column = -1 };

    /// <summary>Compares two Coordinates for equality.</summary>
    public static bool operator ==(Coordinates x, Coordinates y) =>
        x.Line == y.Line && x.Column == y.Column;

    /// <summary>Compares two Coordinates for inequality.</summary>
    public static bool operator !=(Coordinates x, Coordinates y) =>
        x.Line != y.Line || x.Column != y.Column;

    /// <summary>Compares two Coordinates to determine if one is less than the other.</summary>
    public static bool operator <(Coordinates x, Coordinates y) =>
        x.Line != y.Line ? x.Line < y.Line : x.Column < y.Column;

    /// <summary>Compares two Coordinates to determine if one is greater than the other.</summary>
    public static bool operator >(Coordinates x, Coordinates y) =>
        x.Line != y.Line ? x.Line > y.Line : x.Column > y.Column;

    /// <summary>Compares two Coordinates to determine if one is less than or equal to the other.</summary>
    public static bool operator <=(Coordinates x, Coordinates y) =>
        x.Line != y.Line ? x.Line < y.Line : x.Column <= y.Column;

    /// <summary>Compares two Coordinates to determine if one is greater than or equal to the other.</summary>
    public static bool operator >=(Coordinates x, Coordinates y) =>
        x.Line != y.Line ? x.Line > y.Line : x.Column >= y.Column;

    /// <summary>Checks if the current Coordinates instance is equal to another Coordinates instance.</summary>
    public bool Equals(Coordinates other) => Line == other.Line && Column == other.Column;

    /// <summary>Checks if the current Coordinates instance is equal to another object.</summary>
    public override bool Equals(object? obj) => obj is Coordinates other && Equals(other);

    /// <summary>Returns a hash code for the current Coordinates instance based on its Line and Column values.</summary>
    public override int GetHashCode() => HashCode.Combine(Line, Column);
}
