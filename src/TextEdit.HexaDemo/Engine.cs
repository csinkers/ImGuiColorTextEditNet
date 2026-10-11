using Hexa.NET.ImGui;
using Hexa.NET.ImGui.Backends.OpenGL3;
using Hexa.NET.ImGui.Backends.SDL3;
using Hexa.NET.OpenGL;
using Hexa.NET.SDL3;
using SDLEvent = Hexa.NET.SDL3.SDLEvent;

namespace TextEdit.HexaDemo;

public static class Engine
{
    public static unsafe AppContext Initialise(int width, int height)
    {
        SDL.SetHint(SDL.SDL_HINT_MOUSE_FOCUS_CLICKTHROUGH, "1");
        SDL.Init(SDLInitFlags.Events | SDLInitFlags.Video);

        float mainScale = SDL.GetDisplayContentScale(SDL.GetPrimaryDisplay());
        var window = SDL.CreateWindow(
            "TextEdit.HexaTest",
            (int)(width * mainScale),
            (int)(height * mainScale),
            SDLWindowFlags.Resizable | SDLWindowFlags.Opengl | SDLWindowFlags.HighPixelDensity
        );

        uint windowId = SDL.GetWindowID(window);
        var guiContext = ImGui.CreateContext();
        ImGui.SetCurrentContext(guiContext);

        var context = SDL.GLCreateContext(window);
        ImGuiImplSDL3.SetCurrentContext(guiContext);
        var windowPtr = new SDLWindowPtr((Hexa.NET.ImGui.Backends.SDL3.SDLWindow*)window);
        
        if (!ImGuiImplSDL3.InitForOpenGL(windowPtr, (void*)context.Handle))
        {
            SDL.Quit();
            throw new EngineException("Failed to init ImGui Impl SDL3");
        }

        ImGuiImplOpenGL3.SetCurrentContext(guiContext);
        if (!ImGuiImplOpenGL3.Init((byte*)null))
        {
            SDL.Quit();
            throw new EngineException("Failed to init ImGui Impl OpenGL3");
        }

        GL gl = new(new BindingsContext(window, context));
        return new AppContext(windowId, gl, mainScale);
    }

    public static void Run(AppContext ctx, IImGuiApp app)
    {
        while (PumpEvents(ctx))
        {
            BeginFrame(ctx.Gl);
            app.DrawFrame();
            EndFrame(ctx.Gl);
        }
    }

    public static unsafe void Shutdown(AppContext ctx)
    {
        ImGuiImplOpenGL3.Shutdown();
        ImGuiImplSDL3.Shutdown();
        ImGui.DestroyContext();
        ctx.Gl.Dispose();

        var window = SDL.GetWindowFromID(ctx.SdlWindowId);
        SDL.DestroyWindow(window);
        SDL.Quit();
    }

    private static unsafe bool PumpEvents(AppContext ctx)
    {
        SDL.PumpEvents();

        SDLEvent sdlEvent = default;
        while (SDL.PollEvent(ref sdlEvent))
        {
            ImGuiImplSDL3.ProcessEvent((Hexa.NET.ImGui.Backends.SDL3.SDLEvent*)&sdlEvent);

            switch ((SDLEventType)sdlEvent.Type)
            {
                case SDLEventType.Quit:
                case SDLEventType.Terminating:
                case SDLEventType.WindowCloseRequested when sdlEvent.Window.WindowID == ctx.SdlWindowId:
                    return false;
            }
        }

        return true;
    }

    private static void BeginFrame(GL gl)
    {
        gl.MakeCurrent();
        gl.ClearColor(0, 0, 0, 1);
        gl.Clear(GLClearBufferMask.ColorBufferBit);

        ImGuiImplOpenGL3.NewFrame();
        ImGuiImplSDL3.NewFrame();
        ImGui.NewFrame();
    }

    private static void EndFrame(GL gl)
    {
        ImGui.Render();
        ImGui.EndFrame();

        gl.MakeCurrent();
        ImGuiImplOpenGL3.RenderDrawData(ImGui.GetDrawData());

        ImGuiIOPtr io = ImGui.GetIO();
        if ((io.ConfigFlags & ImGuiConfigFlags.ViewportsEnable) != 0)
        {
            ImGui.UpdatePlatformWindows();
            ImGui.RenderPlatformWindowsDefault();
        }

        gl.MakeCurrent();
        gl.SwapBuffers();
    }
}