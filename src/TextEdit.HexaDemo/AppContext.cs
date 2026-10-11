using Hexa.NET.OpenGL;

namespace TextEdit.HexaDemo;

public record AppContext(uint SdlWindowId, GL Gl, float MainScale)
{
    public int ExitCode { get; set; }
}