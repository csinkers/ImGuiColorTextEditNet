using ImGuiColorTextEditNet;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace TextEdit.Tests;

[TestClass]
public class MovementTests
{
    const string sample = @"
one two three
3.14
test.com
";

    [TestMethod]
    public void MoveLeftTest1()
    {
        var t = new TextEditor { Text = sample };
        Assert.AreEqual((0, 0), t.CursorPosition);
        Assert.AreEqual((0, 0), t.SelectionStart);
        Assert.AreEqual((0, 0), t.SelectionEnd);

        t.MoveLeft();
        Assert.AreEqual((0, 0), t.CursorPosition);
        Assert.AreEqual((0, 0), t.SelectionStart);
        Assert.AreEqual((0, 0), t.SelectionEnd);
    }

    [TestMethod]
    public void MoveLeftTest2()
    {
        var t = new TextEditor { Text = sample };
        t.MoveLeft(2);
        Assert.AreEqual((0,0), t.CursorPosition);
        Assert.AreEqual((0,0), t.SelectionStart);
        Assert.AreEqual((0,0), t.SelectionEnd);
    }

    [TestMethod]
    public void MoveLeftTest3()
    {
        var t = new TextEditor { Text = sample };
        t.MoveLeft(1, true);
        Assert.AreEqual((0,0), t.CursorPosition);
        Assert.AreEqual((0,0), t.SelectionStart);
        Assert.AreEqual((0,0), t.SelectionEnd);
    }

    [TestMethod]
    public void MoveLeftTest4()
    {
        var t = new TextEditor { Text = sample };
        t.MoveLeft(1, false, true);
        Assert.AreEqual((0,0), t.CursorPosition);
        Assert.AreEqual((0,0), t.SelectionStart);
        Assert.AreEqual((0,0), t.SelectionEnd);
    }

    [TestMethod]
    public void MoveLeftTest5()
    {
        var t = new TextEditor { Text = sample };
        t.MoveLeft(1, true, true);
        Assert.AreEqual((0,0), t.CursorPosition);
        Assert.AreEqual((0,0), t.SelectionStart);
        Assert.AreEqual((0,0), t.SelectionEnd);
    }

    [TestMethod]
    public void MoveLeftTest6()
    {
        var t = new TextEditor { Text = sample };
        t.SetSelection((1,0), (1,0));
        t.CursorPosition = (1, 0);
        Assert.AreEqual((1,0), t.CursorPosition);
        Assert.AreEqual((1,0), t.SelectionStart);
        Assert.AreEqual((1,0), t.SelectionEnd);

        t.MoveLeft();
        Assert.AreEqual((0,0), t.CursorPosition);
        Assert.AreEqual((0,0), t.SelectionStart);
        Assert.AreEqual((0,0), t.SelectionEnd);
    }

    [TestMethod]
    public void MoveLeftTest7()
    {
        var t = new TextEditor { Text = sample };
        t.SetSelection((1,3), (1,3));
        t.CursorPosition = (1, 3);
        t.MoveLeft();
        Assert.AreEqual((1,2), t.CursorPosition);
        Assert.AreEqual((1,2), t.SelectionStart);
        Assert.AreEqual((1,2), t.SelectionEnd);
    }

    [TestMethod]
    public void MoveLeftTest8()
    {
        var t = new TextEditor { Text = sample };
        t.SetSelection((1,3), (1,3));
        t.CursorPosition = (1, 3);
        t.MoveLeft(1, true);
        Assert.AreEqual((1,2), t.CursorPosition);
        Assert.AreEqual((1,2), t.SelectionStart);
        Assert.AreEqual((1,3), t.SelectionEnd);
    }

    [TestMethod]
    public void MoveLeftTest9()
    {
        var t = new TextEditor
        {
            Text = sample,
            IsColorizerEnabled = false
        };

        t.SetSelection((1,3), (1,3));
        t.CursorPosition = (1, 3);
        t.MoveLeft(1, true, true);
        Assert.AreEqual((1,0), t.CursorPosition);
        Assert.AreEqual((1,0), t.SelectionStart);
        Assert.AreEqual((1,3), t.SelectionEnd);
    }

    [TestMethod]
    public void MoveLeftTest10()
    {
        var t = new TextEditor
        {
            Text = sample,
            IsColorizerEnabled = false
        };
        t.SetSelection((1,4), (1,4));
        t.CursorPosition = (1, 4);
        t.MoveLeft(1, true, true);
        Assert.AreEqual((1,0), t.CursorPosition);
        Assert.AreEqual((1,0), t.SelectionStart);
        Assert.AreEqual((1,4), t.SelectionEnd);
    }

    [TestMethod]
    public void MoveRightTest1()
    {
        var t = new TextEditor { Text = sample };
        Assert.AreEqual((0, 0), t.CursorPosition);
        Assert.AreEqual((0, 0), t.SelectionStart);
        Assert.AreEqual((0, 0), t.SelectionEnd);

        t.MoveRight();
        Assert.AreEqual((1, 0), t.CursorPosition);
        Assert.AreEqual((1, 0), t.SelectionStart);
        Assert.AreEqual((1, 0), t.SelectionEnd);
    }

    [TestMethod]
    public void MoveRightTest2()
    {
        var t = new TextEditor { Text = sample };
        t.MoveRight(2);
        Assert.AreEqual((1,1), t.CursorPosition);
        Assert.AreEqual((1,1), t.SelectionStart);
        Assert.AreEqual((1,1), t.SelectionEnd);
    }

    [TestMethod]
    public void MoveRightTest3()
    {
        var t = new TextEditor { Text = sample };
        t.MoveRight(1, true);
        Assert.AreEqual((1,0), t.CursorPosition);
        Assert.AreEqual((0,0), t.SelectionStart);
        Assert.AreEqual((1,0), t.SelectionEnd);

        t.MoveRight(1, true);
        Assert.AreEqual((1,1), t.CursorPosition);
        Assert.AreEqual((0,0), t.SelectionStart);
        Assert.AreEqual((1,1), t.SelectionEnd);
    }

    [TestMethod]
    public void MoveRightTest4()
    {
        var t = new TextEditor { Text = sample };
        t.MoveRight(1, false, true);
        Assert.AreEqual((1,0), t.CursorPosition);
        Assert.AreEqual((1,0), t.SelectionStart);
        Assert.AreEqual((1,0), t.SelectionEnd);

        t.MoveRight(1, false, true);
        Assert.AreEqual((1,4), t.CursorPosition);
        Assert.AreEqual((1,4), t.SelectionStart);
        Assert.AreEqual((1,4), t.SelectionEnd);
    }

    [TestMethod]
    public void MoveRightTest5()
    {
        var t = new TextEditor
        {
            Text = sample,
            IsColorizerEnabled = false
        };
        t.MoveRight(1, true, true);
        Assert.AreEqual((1,0), t.CursorPosition);
        Assert.AreEqual((0,0), t.SelectionStart);
        Assert.AreEqual((1,0), t.SelectionEnd);

        t.MoveRight(1, true, true);
        Assert.AreEqual((1,4), t.CursorPosition);
        Assert.AreEqual((0,0), t.SelectionStart);
        Assert.AreEqual((1,4), t.SelectionEnd);
    }

    [TestMethod]
    public void MoveRightTest6()
    {
        var t = new TextEditor { Text = sample };
        t.SetSelection((4,0), (4,0));
        t.CursorPosition = (4, 0);
        Assert.AreEqual((4,0), t.CursorPosition);
        Assert.AreEqual((4,0), t.SelectionStart);
        Assert.AreEqual((4,0), t.SelectionEnd);
    }

    [TestMethod]
    public void MoveRightTest7()
    {
        var t = new TextEditor { Text = sample };
        t.SetSelection((1,3), (1,3));
        t.CursorPosition = (1, 3);
        t.MoveRight();
        Assert.AreEqual((1,4), t.CursorPosition);
        Assert.AreEqual((1,4), t.SelectionStart);
        Assert.AreEqual((1,4), t.SelectionEnd);
    }

    [TestMethod]
    public void MoveRightTest8()
    {
        var t = new TextEditor { Text = sample };
        t.SetSelection((1,3), (1,3));
        t.CursorPosition = (1, 3);
        t.MoveRight(1, true);
        Assert.AreEqual((1,4), t.CursorPosition);
        Assert.AreEqual((1,3), t.SelectionStart);
        Assert.AreEqual((1,4), t.SelectionEnd);
    }

    [TestMethod]
    public void MoveRightTest9()
    {
        var t = new TextEditor
        {
            Text = sample,
            IsColorizerEnabled = false
        };

        t.SetSelection((1,3), (1,3));
        t.CursorPosition = (1, 3);
        t.MoveRight(1, true, true);
        Assert.AreEqual((1,8), t.CursorPosition);
        Assert.AreEqual((1,3), t.SelectionStart);
        Assert.AreEqual((1,8), t.SelectionEnd);
    }

    [TestMethod]
    public void MoveRightTest10()
    {
        var t = new TextEditor
        {
            Text = sample,
            IsColorizerEnabled = false
        };
        t.SetSelection((1,4), (1,4));
        t.CursorPosition = (1, 4);
        t.MoveRight(1, true, true);
        Assert.AreEqual((1,8), t.CursorPosition);
        Assert.AreEqual((1,4), t.SelectionStart);
        Assert.AreEqual((1,8), t.SelectionEnd);
    }


    // control.MoveLeft();
    // control.MoveRight();
    // control.MoveUp();
    // control.MoveDown();
    // control.MoveTop();
    // control.MoveBottom();
    // control.MoveHome();
    // control.MoveEnd();
}