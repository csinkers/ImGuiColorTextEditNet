using Hexa.NET.ImGui;
using ImGuiColorTextEditNet;
using ImGuiColorTextEditNet.Syntax;

namespace TextEdit.HexaDemo;

public sealed class ExampleApp : IImGuiApp
{
    private readonly TextEditor _editor;
    private readonly (int, object)[] _demoBreakpoints;
    private readonly Dictionary<int, object> _demoErrors;

    const string DemoText =
        """
        #include <stdio.h>

        void main(int argc, char **argv) {
            printf("Hello world!\n");
            /* A multi-line
            comment which continues on
            to here */

            for (int i = 0; i < 10; i++)
                printf("%d\n", i); // Breakpoint here

            // A single line comment
            int a = 123456;
            int b = 0x123456; // and here
            int c = 0b110101;
            errors on this line!
        }
        """;
        
    public ExampleApp(AppContext ctx)
    {
        ImGuiIOPtr io = ImGui.GetIO();
        io.ConfigFlags |= ImGuiConfigFlags.NavEnableKeyboard;
        io.ConfigFlags |= ImGuiConfigFlags.DockingEnable;
        io.ConfigFlags |= ImGuiConfigFlags.ViewportsEnable;
        io.ConfigViewportsNoAutoMerge = false;
        io.ConfigViewportsNoTaskBarIcon = false;

        var style = ImGui.GetStyle();
        style.ScaleAllSizes(ctx.MainScale);
        style.FontScaleDpi = ctx.MainScale; // Set initial font scale. (using io.ConfigDpiScaleFonts=true makes this unnecessary. We leave both here for documentation purpose)
        io.ConfigDpiScaleFonts = true; // [Experimental] Automatically overwrite style.FontScaleDpi in Begin() when Monitor DPI changes. This will scale fonts but _NOT_ scale sizes/padding for now.
        io.ConfigDpiScaleViewports = true;

        _editor = new TextEditor
        {
            AllText = DemoText,
            SyntaxHighlighter = new CStyleHighlighter(true),
        };

        _demoBreakpoints = [(10, ""), (14, "")];
        _demoErrors = new Dictionary<int, object> { { 16, "Syntax error etc" } };
        _editor.Breakpoints.SetBreakpoints(_demoBreakpoints);
        _editor.ErrorMarkers.SetErrorMarkers(_demoErrors);

        _editor.SetColor(PaletteIndex.Custom, 0xff0000ff);
        _editor.SetColor(PaletteIndex.Custom + 1, 0xff00ffff);
        _editor.SetColor(PaletteIndex.Custom + 2, 0xffffffff);
        _editor.SetColor(PaletteIndex.Custom + 3, 0xff808080);
    }

    public void DrawFrame()
    {
        ImGui.Begin("Demo");

        if (ImGui.Button("Reset"))
        {
            _editor.AllText = DemoText;
            _editor.Breakpoints.SetBreakpoints(_demoBreakpoints);
            _editor.ErrorMarkers.SetErrorMarkers(_demoErrors);
        }

        ImGui.SameLine();
        if (ImGui.Button("err line"))
            _editor.AppendLine("Some error text", PaletteIndex.Custom);

        ImGui.SameLine();
        if (ImGui.Button("warn line"))
            _editor.AppendLine("Some warning text", PaletteIndex.Custom + 1);

        ImGui.SameLine();
        if (ImGui.Button("info line"))
            _editor.AppendLine("Some info text", PaletteIndex.Custom + 2);

        ImGui.SameLine();
        if (ImGui.Button("verbose line"))
            _editor.AppendLine("Some debug text", PaletteIndex.Custom + 3);

        ImGui.Text(
            $"Cur:{_editor.CursorPosition} SEL: {_editor.Selection.Start} - {_editor.Selection.End}"
        );
        _editor.Render("EditWindow");

        ImGui.End();
    }

    public void Dispose()
    {
    }
}