using System.Numerics;
using ImGuiColorTextEditNet;
using ImGuiNET;
using Veldrid;
using Veldrid.StartupUtilities;

namespace TextEdit.Test;

public static class Program
{
    public static void Main()
    {
        var windowInfo = new WindowCreateInfo
        {
            X = 100,
            Y = 100,
            WindowWidth = 800,
            WindowHeight = 1024,
            WindowInitialState = WindowState.Normal,
            WindowTitle = "TextEdit.Test"
        };

        var gdOptions = new GraphicsDeviceOptions(
            true,
            PixelFormat.R32_Float,
            true,
            ResourceBindingModel.Improved,
            true,
            true,
            false);

        var window = VeldridStartup.CreateWindow(ref windowInfo);
        var gd = VeldridStartup.CreateGraphicsDevice(window, gdOptions, GraphicsBackend.Direct3D11);

        var imguiRenderer = new ImGuiRenderer(
            gd,
            gd.MainSwapchain.Framebuffer.OutputDescription,
            (int)gd.MainSwapchain.Framebuffer.Width,
            (int)gd.MainSwapchain.Framebuffer.Height);

        var cl = gd.ResourceFactory.CreateCommandList();
        window.Resized += () =>
        {
            gd.ResizeMainWindow((uint)window.Width, (uint)window.Height);
            imguiRenderer.WindowResized(window.Width, window.Height);
        };

        var editor = new TextEditor(new CStyleHighlighter(true))
        {
            Text = @"#include <stdio.h>

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
"
        };

        editor.SetBreakpoints(new HashSet<int> { 10, 14 });
        editor.SetErrorMarkers(new Dictionary<int, string> { { 16, "Syntax error etc" } });

        DateTime lastFrame = DateTime.Now;
        while (window.Exists)
        {
            var input = window.PumpEvents();
            if (!window.Exists)
                break;

            var thisFrame = DateTime.Now;
            imguiRenderer.Update((float)(thisFrame - lastFrame).TotalSeconds, input);
            lastFrame=thisFrame;

            ImGui.SetNextWindowPos(new Vector2(0, 0));
            ImGui.SetNextWindowSize(new Vector2(window.Width, window.Height));
            ImGui.Begin("Demo");

            editor.Render("EditWindow");

            ImGui.End();

            cl.Begin();
            cl.SetFramebuffer(gd.MainSwapchain.Framebuffer);
            cl.ClearColorTarget(0, RgbaFloat.Black);
            imguiRenderer.Render(gd, cl);
            cl.End();
            gd.SubmitCommands(cl);
            gd.SwapBuffers(gd.MainSwapchain);
        }
    }
}
