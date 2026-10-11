using Hexa.NET.SDL3;

namespace TextEdit.HexaDemo;

internal unsafe class BindingsContext : HexaGen.Runtime.IGLContext
{
    private readonly SDLWindow* _window;
    private readonly SDLGLContext _context;

    public BindingsContext(SDLWindow* window, SDLGLContext context)
    {
        _window = window;
        _context = context;
    }

    public nint Handle => (nint)_window;
    public bool IsCurrent => SDL.GLGetCurrentContext() == _context;
    public void Dispose() { }

    public nint GetProcAddress(string procName) => (nint)SDL.GLGetProcAddress(procName);
    public bool IsExtensionSupported(string extensionName) => SDL.GLExtensionSupported(extensionName);
    public void MakeCurrent() => SDL.GLMakeCurrent(_window, _context);
    public void SwapBuffers() => SDL.GLSwapWindow(_window);
    public void SwapInterval(int interval) => SDL.GLSetSwapInterval(interval);

    public bool TryGetProcAddress(string procName, out nint procAddress)
    {
        procAddress = (nint)SDL.GLGetProcAddress(procName);
        return procAddress != 0;
    }
}