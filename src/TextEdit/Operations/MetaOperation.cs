using System;
using System.Collections.Generic;
using System.Linq;

namespace ImGuiColorTextEditNet.Operations;

internal class MetaOperation : IEditorOperation
{
    readonly List<IEditorOperation> _operations = [];

    public void Add(IEditorOperation operation)
    {
        ArgumentNullException.ThrowIfNull(operation);
        _operations.Add(operation);
    }

    public void Apply(TextEditor editor)
    {
        foreach (var op in _operations)
            op.Apply(editor);
    }

    public void Undo(TextEditor editor)
    {
        foreach (var op in _operations.AsEnumerable().Reverse())
            op.Undo(editor);
    }

    public object SerializeState() => _operations.Select(op => op.SerializeState()).ToList();
}
