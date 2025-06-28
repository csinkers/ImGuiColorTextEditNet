using ImGuiColorTextEditNet;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace TextEdit.Tests;

[TestClass]
public class MovementTests
{
    const string Sample = @"
one two three
3.14
test.com
";

    [TestMethod]
    public void MoveLeftTest1()
    {
        var t = new TextEditor { AllText = Sample };
        Assert.AreEqual((0, 0), t.CursorPosition);
        Assert.AreEqual((0, 0), t.Selection.Start);
        Assert.AreEqual((0, 0), t.Selection.End);

        t.Movement.MoveLeft();
        Assert.AreEqual((0, 0), t.CursorPosition);
        Assert.AreEqual((0, 0), t.Selection.Start);
        Assert.AreEqual((0, 0), t.Selection.End);
    }

    [TestMethod]
    public void MoveLeftTest2()
    {
        var t = new TextEditor { AllText = Sample };
        t.Movement.MoveLeft(2);
        Assert.AreEqual((0,0), t.CursorPosition);
        Assert.AreEqual((0,0), t.Selection.Start);
        Assert.AreEqual((0,0), t.Selection.End);
    }

    [TestMethod]
    public void MoveLeftTest3()
    {
        var t = new TextEditor { AllText = Sample };
        t.Movement.MoveLeft(1, true);
        Assert.AreEqual((0,0), t.CursorPosition);
        Assert.AreEqual((0,0), t.Selection.Start);
        Assert.AreEqual((0,0), t.Selection.End);
    }

    [TestMethod]
    public void MoveLeftTest4()
    {
        var t = new TextEditor { AllText = Sample };
        t.Movement.MoveLeft(1, false, true);
        Assert.AreEqual((0,0), t.CursorPosition);
        Assert.AreEqual((0,0), t.Selection.Start);
        Assert.AreEqual((0,0), t.Selection.End);
    }

    [TestMethod]
    public void MoveLeftTest5()
    {
        var t = new TextEditor { AllText = Sample };
        t.Movement.MoveLeft(1, true, true);
        Assert.AreEqual((0,0), t.CursorPosition);
        Assert.AreEqual((0,0), t.Selection.Start);
        Assert.AreEqual((0,0), t.Selection.End);
    }

    [TestMethod]
    public void MoveLeftTest6()
    {
        var t = new TextEditor { AllText = Sample };
        t.Selection.Select((1,0), (1,0));
        t.CursorPosition = (1, 0);
        Assert.AreEqual((1,0), t.CursorPosition);
        Assert.AreEqual((1,0), t.Selection.Start);
        Assert.AreEqual((1,0), t.Selection.End);

        t.Movement.MoveLeft();
        Assert.AreEqual((0,0), t.CursorPosition);
        Assert.AreEqual((0,0), t.Selection.Start);
        Assert.AreEqual((0,0), t.Selection.End);
    }

    [TestMethod]
    public void MoveLeftTest7()
    {
        var t = new TextEditor { AllText = Sample };
        t.Selection.Select((1,3), (1,3));
        t.CursorPosition = (1, 3);
        t.Movement.MoveLeft();
        Assert.AreEqual((1,2), t.CursorPosition);
        Assert.AreEqual((1,2), t.Selection.Start);
        Assert.AreEqual((1,2), t.Selection.End);
    }

    [TestMethod]
    public void MoveLeftTest8()
    {
        var t = new TextEditor { AllText = Sample };
        t.Selection.Select((1,3), (1,3));
        t.CursorPosition = (1, 3);
        t.Movement.MoveLeft(1, true);
        Assert.AreEqual((1,2), t.CursorPosition);
        Assert.AreEqual((1,2), t.Selection.Start);
        Assert.AreEqual((1,3), t.Selection.End);
    }

    [TestMethod]
    public void MoveLeftTest9()
    {
        var t = new TextEditor { AllText = Sample };
        t.Options.IsColorizerEnabled = false;

        t.Selection.Select((1,3), (1,3));
        t.CursorPosition = (1, 3);
        t.Movement.MoveLeft(1, true, true);
        Assert.AreEqual((1,0), t.CursorPosition);
        Assert.AreEqual((1,0), t.Selection.Start);
        Assert.AreEqual((1,3), t.Selection.End);
    }

    [TestMethod]
    public void MoveLeftTest10()
    {
        var t = new TextEditor { AllText = Sample };
        t.Options.IsColorizerEnabled = false;
        t.Selection.Select((1,4), (1,4));
        t.CursorPosition = (1, 4);
        t.Movement.MoveLeft(1, true, true);
        Assert.AreEqual((1,0), t.CursorPosition);
        Assert.AreEqual((1,0), t.Selection.Start);
        Assert.AreEqual((1,4), t.Selection.End);
    }

    [TestMethod]
    public void MoveRightTest1()
    {
        var t = new TextEditor { AllText = Sample };
        Assert.AreEqual((0, 0), t.CursorPosition);
        Assert.AreEqual((0, 0), t.Selection.Start);
        Assert.AreEqual((0, 0), t.Selection.End);

        // Moving right on an empty line should move to the start of the line below
        t.Movement.MoveRight();
        Assert.AreEqual((1, 0), t.CursorPosition);
        Assert.AreEqual((1, 0), t.Selection.Start);
        Assert.AreEqual((1, 0), t.Selection.End);
    }

    [TestMethod]
    public void MoveRightTest2()
    {
        var t = new TextEditor { AllText = Sample };

        // Moving right twice on an empty line followed by a non-empty line should go to the next line and then past the first character.
        t.Movement.MoveRight(2);
        Assert.AreEqual((1,1), t.CursorPosition);
        Assert.AreEqual((1,1), t.Selection.Start);
        Assert.AreEqual((1,1), t.Selection.End);
    }

    [TestMethod]
    public void MoveRightTest3()
    {
        var t = new TextEditor { AllText = Sample };

        // Moving right on an empty line while selecting should extend the selection to the start of the next line.
        t.Movement.MoveRight(1, true);
        Assert.AreEqual((1,0), t.CursorPosition);
        Assert.AreEqual((0,0), t.Selection.Start);
        Assert.AreEqual((1,0), t.Selection.End);

        // Doing it again should extend the selection to include the first char on the following line.
        t.Movement.MoveRight(1, true);
        Assert.AreEqual((1,1), t.CursorPosition);
        Assert.AreEqual((0,0), t.Selection.Start);
        Assert.AreEqual((1,1), t.Selection.End);
    }

    [TestMethod]
    public void MoveRightTest4()
    {
        var t = new TextEditor { AllText = Sample };

        // Moving right on an empty line while moving by word should move the cursor to the start of the next line.
        t.Movement.MoveRight(1, false, true);
        Assert.AreEqual((1,0), t.CursorPosition);
        Assert.AreEqual((1,0), t.Selection.Start);
        Assert.AreEqual((1,0), t.Selection.End);

        // Doing it again should move the cursor past the first word on the following line.
        t.Movement.MoveRight(1, false, true);
        Assert.AreEqual((1,4), t.CursorPosition);
        Assert.AreEqual((1,4), t.Selection.Start);
        Assert.AreEqual((1,4), t.Selection.End);
    }

    [TestMethod]
    public void MoveRightTest5()
    {
        var t = new TextEditor { AllText = Sample, Options = { IsColorizerEnabled = false } };
        t.Movement.MoveRight(1, true, true);
        Assert.AreEqual((1,0), t.CursorPosition);
        Assert.AreEqual((0,0), t.Selection.Start);
        Assert.AreEqual((1,0), t.Selection.End);

        t.Movement.MoveRight(1, true, true);
        Assert.AreEqual((1,4), t.CursorPosition);
        Assert.AreEqual((0,0), t.Selection.Start);
        Assert.AreEqual((1,4), t.Selection.End);
    }

    [TestMethod]
    public void MoveRightTest6()
    {
        var t = new TextEditor { AllText = Sample };
        t.Selection.Select((4,0), (4,0));
        t.CursorPosition = (4, 0);
        Assert.AreEqual((4,0), t.CursorPosition);
        Assert.AreEqual((4,0), t.Selection.Start);
        Assert.AreEqual((4,0), t.Selection.End);
    }

    [TestMethod]
    public void MoveRightTest7()
    {
        var t = new TextEditor { AllText = Sample };
        t.Selection.Select((1,3), (1,3));
        t.CursorPosition = (1, 3);
        t.Movement.MoveRight();
        Assert.AreEqual((1,4), t.CursorPosition);
        Assert.AreEqual((1,4), t.Selection.Start);
        Assert.AreEqual((1,4), t.Selection.End);
    }

    [TestMethod]
    public void MoveRightTest8()
    {
        var t = new TextEditor { AllText = Sample };
        t.Selection.Select((1,3), (1,3));
        t.CursorPosition = (1, 3);
        t.Movement.MoveRight(1, true);
        Assert.AreEqual((1,4), t.CursorPosition);
        Assert.AreEqual((1,3), t.Selection.Start);
        Assert.AreEqual((1,4), t.Selection.End);
    }

    [TestMethod]
    public void MoveRightTest9()
    {
        var t = new TextEditor { AllText = Sample };
        t.Options.IsColorizerEnabled = false;

        t.Selection.Select((1,3), (1,3));
        t.CursorPosition = (1, 3);
        t.Movement.MoveRight(1, true, true);
        Assert.AreEqual((1,8), t.CursorPosition);
        Assert.AreEqual((1,3), t.Selection.Start);
        Assert.AreEqual((1,8), t.Selection.End);
    }

    [TestMethod]
    public void MoveRightTest10()
    {
        var t = new TextEditor { AllText = Sample };
        t.Options.IsColorizerEnabled = false;
        t.Selection.Select((1,4), (1,4));
        t.CursorPosition = (1, 4);
        t.Movement.MoveRight(1, true, true);
        Assert.AreEqual((1,8), t.CursorPosition);
        Assert.AreEqual((1,4), t.Selection.Start);
        Assert.AreEqual((1,8), t.Selection.End);
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