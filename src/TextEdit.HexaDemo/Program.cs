namespace TextEdit.HexaDemo;

public static class Program
{
    public static int Main(string[] args)
    {
        var ctx = Engine.Initialise(960, 960);
        
        using (var app = new ExampleApp(ctx))
            Engine.Run(ctx, app);
        
        Engine.Shutdown(ctx);
        return ctx.ExitCode;
    }
}