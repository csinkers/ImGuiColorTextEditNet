namespace ImGuiColorTextEditNet.Input;

/// <summary>
/// Delegate type for actions that are invoked due to a key binding.
/// </summary>
/// <returns>True if the key was handled</returns>
public delegate bool EditorKeybindAction(TextEditor editor, object? context);
