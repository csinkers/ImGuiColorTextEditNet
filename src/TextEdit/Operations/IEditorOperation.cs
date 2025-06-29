namespace ImGuiColorTextEditNet.Operations;

internal interface IEditorOperation
{
    void Apply(TextEditor editor);
    void Undo(TextEditor editor);
    object SerializeState(); // Currently just used for unit test assertions
}
