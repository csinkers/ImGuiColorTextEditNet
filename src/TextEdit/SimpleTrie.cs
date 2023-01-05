namespace ImGuiColorTextEditNet;

public class SimpleTrie<TInfo> where TInfo : class
{
    class Node
    {
        public readonly string Path;
        public TInfo? Info;
        public readonly Dictionary<char, Node> Children = new();
        public Node(string path) => Path = path;
        public override string ToString() => $"{Path} ({Children.Count}): {Info}";
    }

    readonly Node _root = new("");
    public SimpleTrie(IEnumerable<(string Name, TInfo Info)> strings)
    {
        foreach (var s in strings)
            Add(s.Name, s.Info);
    }

    public void Add(string name, TInfo info)
    {
        var node = _root;
        foreach (var c in name)
        {
            if (!node.Children.TryGetValue(c, out var newNode))
            {
                newNode = new Node(node.Path + c);
                node.Children[c] = newNode;
            }

            node = newNode;
        }

        node.Info = info;
    }

    public TInfo? Get<T>(ReadOnlySpan<T> key, Func<T, char> toChar)
    {
        var node = _root;
        foreach (var c in key)
        {
            if (node.Children.TryGetValue(toChar(c), out var newNode))
                node = newNode;
            else
                return null;
        }

        return node.Info;
    }

    public TInfo? Get(ReadOnlySpan<char> key)
    {
        var node = _root;
        foreach (var c in key)
        {
            if (node.Children.TryGetValue(c, out var newNode))
                node = newNode;
            else
                return null;
        }

        return node.Info;
    }
}