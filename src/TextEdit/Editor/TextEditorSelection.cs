using System;

namespace ImGuiColorTextEditNet.Editor;

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

    public string GetSelectedText() => _text.GetText(_state.Start, _state.End);
    internal Coordinates GetActualCursorCoordinates() => _text.SanitizeCoordinates(Cursor);

    public int? HighlightedLine { get; set; }
    public Coordinates Cursor
    {
        get => _state.Cursor;
        internal set => _state.Cursor = value;
    }

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

    public void SelectWordUnderCursor() => Select(_text.FindWordStart(Cursor), _text.FindWordEnd(Cursor));
    public void SelectAll() => Select((0, 0), (_text.LineCount, 0));
    public bool HasSelection => End > Start;

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