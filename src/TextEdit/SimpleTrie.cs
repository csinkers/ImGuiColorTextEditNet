using System;
using System.Buffers;
using System.Collections.Generic;

namespace ImGuiColorTextEditNet;

class SimpleTrie<TInfo> where TInfo : class
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

    /// <summary>
    /// Adds an entry to the trie
    /// </summary>
    /// <param name="name">The name to use for looking up the entry</param>
    /// <param name="info">The entry data</param>
    /// <returns>true if entry added, false if an entry already existed for the given name</returns>
    public bool Add(string name, TInfo info)
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

        if (node.Info != null)
            return false;

        node.Info = info;
        return true;
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

    public bool Remove(string name)
    {
        if (string.IsNullOrEmpty(name))
            return false;

        var node = _root;
        var pool = ArrayPool<Node>.Shared;
        var nodes = pool.Rent(name.Length + 1);
        nodes[0] = _root;

        for (int index = 0; index < name.Length; index++)
        {
            var c = name[index];
            if (!node.Children.TryGetValue(c, out var newNode))
                return false;

            nodes[index + 1] = node;
            node = newNode;
        }

        if (node.Info == null)
        {
            pool.Return(nodes);
            return false;
        }

        node.Info = null;

        for (int index = name.Length - 1; index >= 0; index--)
        {
            node = nodes[index + 1];
            if (node.Info != null)
                break;

            if (node.Children.Count == 0)
                nodes[index].Children.Remove(name[index]);
        }

        pool.Return(nodes);
        return true;
    }
}