using System;
using ImGuiColorTextEditNet;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace TextEdit.Tests;

[TestClass]
public class BasicTests
{
    [TestMethod]
    public void ConstructorTest()
    {
        var t = new TextEditor();
        Assert.AreEqual("", t.AllText);
        Assert.AreEqual(1, t.TotalLines);

        var lines = t.TextLines;
        Assert.AreEqual(1, lines.Count);
        Assert.AreEqual("", lines[0]);
    }

    [TestMethod]
    public void SetTextTest()
    {
        var t = new TextEditor { AllText = "abc" };

        Assert.AreEqual("abc", t.AllText);
        Assert.AreEqual(1, t.TotalLines);

        var lines = t.TextLines;
        Assert.AreEqual(1, lines.Count);
        Assert.AreEqual("abc", lines[0]);
    }

    [TestMethod]
    public void MultiLineSetTextTest()
    {
        const string text =
            @"abc
def";
        var t = new TextEditor { AllText = text };
        Assert.AreEqual(text, t.AllText);
        Assert.AreEqual(2, t.TotalLines);

        var lines = t.TextLines;
        Assert.AreEqual(2, lines.Count);
        Assert.AreEqual("abc", lines[0]);
        Assert.AreEqual("def", lines[1]);
    }

    [TestMethod]
    public void TrailingNewlineSetTextTest()
    {
        const string text =
            @"abc
";
        var t = new TextEditor { AllText = text };
        Assert.AreEqual(text, t.AllText);
        Assert.AreEqual(2, t.TotalLines);

        var lines = t.TextLines;
        Assert.AreEqual(2, lines.Count);
        Assert.AreEqual("abc", lines[0]);
        Assert.AreEqual("", lines[1]);
    }

    [TestMethod]
    public void SetTextEmptyTest()
    {
        var t = new TextEditor { AllText = "" };
        Assert.AreEqual("", t.AllText);
        Assert.AreEqual(1, t.TotalLines);

        var lines = t.TextLines;
        Assert.AreEqual(1, lines.Count);
        Assert.AreEqual("", lines[0]);
    }

    [TestMethod]
    public void SetTextLinesTest()
    {
        var t = new TextEditor { TextLines = ["abc"] };

        Assert.AreEqual("abc", t.AllText);
        Assert.AreEqual(1, t.TotalLines);

        var lines = t.TextLines;
        Assert.AreEqual(1, lines.Count);
        Assert.AreEqual("abc", lines[0]);
    }

    [TestMethod]
    public void MultiLineSetTextLinesTest()
    {
        const string text =
            @"abc
def";
        var t = new TextEditor { TextLines = ["abc", "def"] };

        Assert.AreEqual(text, t.AllText);
        Assert.AreEqual(2, t.TotalLines);

        var lines = t.TextLines;
        Assert.AreEqual(2, lines.Count);
        Assert.AreEqual("abc", lines[0]);
        Assert.AreEqual("def", lines[1]);
    }

    [TestMethod]
    public void SetTextLinesEmptyTest()
    {
        var t = new TextEditor { TextLines = Array.Empty<string>() };
        Assert.AreEqual("", t.AllText);
        Assert.AreEqual(1, t.TotalLines);

        var lines = t.TextLines;
        Assert.AreEqual(1, lines.Count);
        Assert.AreEqual("", lines[0]);
    }
}
