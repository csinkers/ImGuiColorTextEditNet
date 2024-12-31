using System.Numerics;
using ImGuiColorTextEditNet;
using ImGuiNET;
using Veldrid;
using Veldrid.StartupUtilities;

namespace TextEdit.Demo;

public static class Program
{
    public static void Main()
    {
        var windowInfo = new WindowCreateInfo
        {
            X = 100,
            Y = 100,
            WindowWidth = 960,
            WindowHeight = 1280,
            WindowInitialState = WindowState.Normal,
            WindowTitle = "TextEdit.Test"
        };

        var gdOptions = new GraphicsDeviceOptions(
            true,
            null,
            true,
            ResourceBindingModel.Improved,
            true,
            true,
            false);

        VeldridStartup.CreateWindowAndGraphicsDevice(
            new WindowCreateInfo(50, 50, 1280, 720, WindowState.Normal, "Control"),
            new GraphicsDeviceOptions(true, null, true, ResourceBindingModel.Improved, true, true),
            out var window,
            out var gd);

        var controller = new ImGuiController(gd, gd.MainSwapchain.Framebuffer.OutputDescription, window.Width, window.Height);

        var cl = gd.ResourceFactory.CreateCommandList();
        window.Resized += () =>
        {
            gd.ResizeMainWindow((uint)window.Width, (uint)window.Height);
            controller.WindowResized(window.Width, window.Height);
        };

        var demoText = @"#include <stdio.h>

void main(int argc, char **argv) {
	printf(""Hello world!\n"");
	/* A multi-line
	comment which continues on
	to here */

	for (int i = 0; i < 10; i++)
		printf(""%d\n"", i); // Breakpoint here

	// A single line comment
	int a = 123456;
	int b = 0x123456; // and here
	int c = 0b110101;
    errors on this line!
}
";

        var editor = new TextEditor
        {
            AllText = demoText,
            SyntaxHighlighter = new CStyleHighlighter(true)
        };

        var demoBreakpoints = new (int, object)[] { (10, ""), (14, "") };
        var demoErrors = new Dictionary<int, object> { { 16, "Syntax error etc" } };
        editor.Breakpoints.SetBreakpoints(demoBreakpoints);
        editor.ErrorMarkers.SetErrorMarkers(demoErrors);

        editor.SetColor(PaletteIndex.Custom, 0xff0000ff);
        editor.SetColor(PaletteIndex.Custom + 1, 0xff00ffff);
        editor.SetColor(PaletteIndex.Custom + 2, 0xffffffff);
        editor.SetColor(PaletteIndex.Custom + 3, 0xff808080);

        DateTime lastFrame = DateTime.Now;

        var io = ImGui.GetIO();
        ImFontPtr font;
        unsafe
        {
            var nativeConfig = ImGuiNative.ImFontConfig_ImFontConfig();
            nativeConfig->OversampleH = 8;
            nativeConfig->OversampleV = 8;
            nativeConfig->RasterizerMultiply = 1f;
            nativeConfig->GlyphOffset = new Vector2(0);

            var dir = Directory.GetCurrentDirectory();
            font = io.Fonts.AddFontFromFileTTF(@"../../../../../SpaceMono-Regular.ttf",
                       16, // size in pixels
                       nativeConfig);

            if (font.NativePtr == (ImFont *)0 )
                throw new InvalidOperationException("Font could not be loaded");
            controller.RecreateFontDeviceTexture();
        }

        io.FontGlobalScale = 2.0f;

        while (window.Exists)
        {
            var input = window.PumpEvents();
            if (!window.Exists)
                break;

            var thisFrame = DateTime.Now;
            controller.Update((float)(thisFrame - lastFrame).TotalSeconds, input);
            lastFrame = thisFrame;

            ImGui.SetNextWindowPos(new Vector2(0, 0));
            ImGui.SetNextWindowSize(new Vector2(window.Width, window.Height));
            ImGui.PushFont(font);
            ImGui.Begin("Demo");

            if (ImGui.Button("Reset"))
            {
                editor.AllText = demoText;
                editor.Breakpoints.SetBreakpoints(demoBreakpoints);
                editor.ErrorMarkers.SetErrorMarkers(demoErrors);
            }

            ImGui.SameLine(); if (ImGui.Button("err line")) editor.AppendLine("Some error text", PaletteIndex.Custom);
            ImGui.SameLine(); if (ImGui.Button("warn line")) editor.AppendLine("Some warning text", PaletteIndex.Custom + 1);
            ImGui.SameLine(); if (ImGui.Button("info line")) editor.AppendLine("Some info text", PaletteIndex.Custom + 2);
            ImGui.SameLine(); if (ImGui.Button("verbose line")) editor.AppendLine("Some debug text", PaletteIndex.Custom + 3);

            ImGui.Text($"Cur:{editor.CursorPosition} SEL: {editor.Selection.Start} - {editor.Selection.End}");
            editor.Render("EditWindow");

            ImGui.End();
            ImGui.PopFont();

            cl.Begin();
            cl.SetFramebuffer(gd.MainSwapchain.Framebuffer);
            cl.ClearColorTarget(0, RgbaFloat.Black);
            controller.Render(gd, cl);
            cl.End();
            gd.SubmitCommands(cl);
            gd.SwapBuffers(gd.MainSwapchain);
        }
    }
}
