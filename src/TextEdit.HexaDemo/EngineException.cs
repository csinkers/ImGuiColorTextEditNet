namespace TextEdit.HexaDemo;

public class EngineException : Exception
{
    public EngineException() {}
    public EngineException(string message) : base(message) { }
}