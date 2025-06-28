using System;

namespace ImGuiColorTextEditNet.Editor;

/// <summary>Handles text selection, allowing for selecting text ranges, words, or lines.</summary>
public class TextEditorSelection
{
    readonly TextEditorText _text;
    SelectionState _state;

    internal SelectionState State => _state;
    internal SelectionMode Mode = SelectionMode.Normal;
    internal Coordinates InteractiveStart;
    internal Coordinates InteractiveEnd;

    internal TextEditorSelection(TextEditorText text)
    {
        _text = text ?? throw new ArgumentNullException(nameof(text));
    }

    /// <summary>Gets the currently selected text.</summary>
    public string GetSelectedText() => _text.GetText(_state.Start, _state.End);

    internal Coordinates GetActualCursorCoordinates() => _text.SanitizeCoordinates(Cursor);

    /// <summary>Gets or sets the line number that is highlighted (if any).</summary>
    public int? HighlightedLine { get; set; }


    /// <summary>Gets or sets the current cursor position.</summary>
    public Coordinates Cursor
    {
        get => _state.Cursor;
        internal set => _state.Cursor = value;
    }

    /// <summary>Gets or sets the start coordinates of the selection.</summary>
    public Coordinates Start
    {
        get => _state.Start;
        set
        {
            _state.Start = _text.SanitizeCoordinates(value);
            if (_state.Start > _state.End)
                (_state.Start, _state.End) = (_state.End, _state.Start);
        }
    }

    /// <summary>Gets or sets the end coordinates of the selection.</summary>
    public Coordinates End
    {
        get => _state.End;
        set
        {
            _state.End = _text.SanitizeCoordinates(value);
            if (_state.Start > _state.End)
                (_state.Start, _state.End) = (_state.End, _state.Start);
        }
    }

    internal object SerializeState() =>
        new
        {
            Cursor = Cursor.ToString(),
            Start = Start.ToString(),
            End = End.ToString(),
            Mode,
        };

    /// <summary>Selects the word that is currently under the cursor.</summary>
    public void SelectWordUnderCursor() => Select(_text.FindWordStart(Cursor), _text.FindWordEnd(Cursor));

    /// <summary>Selects all text.</summary>
    public void SelectAll() => Select((0, 0), (_text.LineCount, 0));

    /// <summary>Indicates whether there is an active selection.</summary>
    public bool HasSelection => End > Start;

    /// <summary>Selects a range of text based on the specified start and end coordinates.</summary>
    public void Select(Coordinates start, Coordinates end, SelectionMode mode = SelectionMode.Normal)
    {
        _state.Start = _text.SanitizeCoordinates(start);
        End = _text.SanitizeCoordinates(end);

        switch (mode)
        {
            case SelectionMode.Normal:
                break;

            case SelectionMode.Word:
            {
                Start = _text.FindWordStart(Start);
                if (!_text.IsOnWordBoundary(End))
                    End = _text.FindWordEnd(_text.FindWordStart(End));
                break;
            }

            case SelectionMode.Line:
            {
                Start = (Start.Line, 0);
                End = (End.Line, _text.GetLineMaxColumn(End.Line));
                break;
            }
        }
    }
}
