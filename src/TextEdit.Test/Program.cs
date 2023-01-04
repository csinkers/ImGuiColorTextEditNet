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

        var editor = new TextEditor();
        var buf = new byte[256];

        while (window.Exists)
        {
            var input = window.PumpEvents();
            if (!window.Exists)
                break;

            imguiRenderer.Update(1f / 60f, input);

            ImGui.SetNextWindowPos(new Vector2(0, 0));
            ImGui.SetNextWindowSize(new Vector2(window.Width, window.Height));
            ImGui.Begin("Demo");

            ImGui.InputText("Foo", buf, (uint)buf.Length);
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
