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
        var control = new TextEditor();
        Assert.AreEqual("", control.Text);
        Assert.AreEqual(1, control.TotalLines);

        var lines = control.TextLines;
        Assert.AreEqual(1, lines.Count);
        Assert.AreEqual("", lines[0]);
    }

    [TestMethod]
    public void SetTextTest()
    {
        var control = new TextEditor();
        control.Text = "abc";

        Assert.AreEqual("abc", control.Text);
        Assert.AreEqual(1, control.TotalLines);

        var lines = control.TextLines;
        Assert.AreEqual(1, lines.Count);
        Assert.AreEqual("abc", lines[0]);
    }

    [TestMethod]
    public void MultiLineSetTextTest()
    {
        const string text = @"abc
def";
        var control = new TextEditor();
        control.Text = text;
        Assert.AreEqual(text, control.Text);
        Assert.AreEqual(2, control.TotalLines);

        var lines = control.TextLines;
        Assert.AreEqual(2, lines.Count);
        Assert.AreEqual("abc", lines[0]);
        Assert.AreEqual("def", lines[1]);
    }

    [TestMethod]
    public void TrailingNewlineSetTextTest()
    {
        const string text = @"abc
";
        var control = new TextEditor();
        control.Text = text;
        Assert.AreEqual(text, control.Text);
        Assert.AreEqual(2, control.TotalLines);

        var lines = control.TextLines;
        Assert.AreEqual(2, lines.Count);
        Assert.AreEqual("abc", lines[0]);
        Assert.AreEqual("", lines[1]);
    }

    [TestMethod]
    public void SetTextEmptyTest()
    {
        var control = new TextEditor();
        control.Text = "";
        Assert.AreEqual("", control.Text);
        Assert.AreEqual(1, control.TotalLines);

        var lines = control.TextLines;
        Assert.AreEqual(1, lines.Count);
        Assert.AreEqual("", lines[0]);
    }

    [TestMethod]
    public void SetTextLinesTest()
    {
        var control = new TextEditor();
        control.TextLines = new[] { "abc" };

        Assert.AreEqual("abc", control.Text);
        Assert.AreEqual(1, control.TotalLines);

        var lines = control.TextLines;
        Assert.AreEqual(1, lines.Count);
        Assert.AreEqual("abc", lines[0]);
    }

    [TestMethod]
    public void MultiLineSetTextLinesTest()
    {
        const string text = @"abc
def";
        var control = new TextEditor();
        control.TextLines = new[] { "abc", "def" };

        Assert.AreEqual(text, control.Text);
        Assert.AreEqual(2, control.TotalLines);

        var lines = control.TextLines;
        Assert.AreEqual(2, lines.Count);
        Assert.AreEqual("abc", lines[0]);
        Assert.AreEqual("def", lines[1]);
    }

    [TestMethod]
    public void SetTextLinesEmptyTest()
    {
        var control = new TextEditor();
        control.TextLines = Array.Empty<string>();
        Assert.AreEqual("", control.Text);
        Assert.AreEqual(1, control.TotalLines);

        var lines = control.TextLines;
        Assert.AreEqual(1, lines.Count);
        Assert.AreEqual("", lines[0]);
    }
}