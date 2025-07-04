﻿using System.Numerics;
using ImGuiColorTextEditNet;
using ImGuiColorTextEditNet.Syntax;
using ImGuiNET;
using Veldrid;
using Veldrid.Sdl2;
using Veldrid.StartupUtilities;

namespace TextEdit.Demo;

public static class Program
{
    public static void Main()
    {
        var (window, gd) = CreateWindow();
        using var controller = new ImGuiController(
            gd,
            gd.MainSwapchain.Framebuffer.OutputDescription,
            window.Width,
            window.Height
        );

        var font = SetupFont(controller);
        var cl = gd.ResourceFactory.CreateCommandList();

        window.Resized += () =>
        {
            gd.ResizeMainWindow((uint)window.Width, (uint)window.Height);
            controller.WindowResized(window.Width, window.Height);
        };

        const string demoText = """
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

        var editor = new TextEditor
        {
            AllText = demoText,
            SyntaxHighlighter = new CStyleHighlighter(true),
        };

        (int, object)[] demoBreakpoints = [(10, ""), (14, "")];
        var demoErrors = new Dictionary<int, object> { { 16, "Syntax error etc" } };
        editor.Breakpoints.SetBreakpoints(demoBreakpoints);
        editor.ErrorMarkers.SetErrorMarkers(demoErrors);

        editor.SetColor(PaletteIndex.Custom, 0xff0000ff);
        editor.SetColor(PaletteIndex.Custom + 1, 0xff00ffff);
        editor.SetColor(PaletteIndex.Custom + 2, 0xffffffff);
        editor.SetColor(PaletteIndex.Custom + 3, 0xff808080);

        DateTime lastFrame = DateTime.Now;

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

            ImGui.SameLine();
            if (ImGui.Button("err line"))
                editor.AppendLine("Some error text", PaletteIndex.Custom);

            ImGui.SameLine();
            if (ImGui.Button("warn line"))
                editor.AppendLine("Some warning text", PaletteIndex.Custom + 1);

            ImGui.SameLine();
            if (ImGui.Button("info line"))
                editor.AppendLine("Some info text", PaletteIndex.Custom + 2);

            ImGui.SameLine();
            if (ImGui.Button("verbose line"))
                editor.AppendLine("Some debug text", PaletteIndex.Custom + 3);

            ImGui.Text(
                $"Cur:{editor.CursorPosition} SEL: {editor.Selection.Start} - {editor.Selection.End}"
            );
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

    static (Sdl2Window, GraphicsDevice) CreateWindow()
    {
        var windowInfo = new WindowCreateInfo
        {
            X = 100,
            Y = 100,
            WindowWidth = 960,
            WindowHeight = 960,
            WindowInitialState = WindowState.Normal,
            WindowTitle = "TextEdit.Test",
        };

        var gdOptions = new GraphicsDeviceOptions(
            true,
            null,
            true,
            ResourceBindingModel.Improved,
            true,
            true,
            false
        );

        VeldridStartup.CreateWindowAndGraphicsDevice(
            windowInfo,
            gdOptions,
            out Sdl2Window? window,
            out GraphicsDevice? gd
        );

        return (window, gd);
    }

    static unsafe ImFontPtr SetupFont(ImGuiController controller)
    {
        var io = ImGui.GetIO();
        var nativeConfig = ImGuiNative.ImFontConfig_ImFontConfig();
        nativeConfig->OversampleH = 8;
        nativeConfig->OversampleV = 8;
        nativeConfig->RasterizerMultiply = 1f;
        nativeConfig->GlyphOffset = new Vector2(0);

        var dir = Directory.GetCurrentDirectory();
        var fontPath = Path.Combine(dir, "SpaceMono-Regular.ttf");

        if (!File.Exists(fontPath))
            throw new FileNotFoundException("Could not find font file at " + fontPath);

        var font = io.Fonts.AddFontFromFileTTF(
            fontPath,
            16, // size in pixels
            nativeConfig
        );

        if (font.NativePtr == (ImFont*)0)
            throw new InvalidOperationException("Font could not be loaded");

        controller.RecreateFontDeviceTexture();

        io.FontGlobalScale = 2.0f;
        return font;
    }
}
