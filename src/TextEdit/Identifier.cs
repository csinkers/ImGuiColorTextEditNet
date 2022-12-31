namespace ImGuiColorTextEditNet;
public struct Identifier
{
    public Identifier(string declaration)
    {
        Location = Coordinates.Invalid;
        Declaration = declaration;
    }
    public Identifier(Coordinates location, string declaration)
    {
        Location = location;
        Declaration = declaration;
    }

    public Coordinates Location;
    public string Declaration;
}