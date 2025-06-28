using ImGuiColorTextEditNet;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace TextEdit.Tests;

[TestClass]
public class MovementTests
{
    [TestMethod]
    public void MoveLeftTest1()
    {
        var t = new TextEditor
        {
            AllText = """

                one two three
                3.14
                test.com

                """,
        };

        Assert.AreEqual((0, 0), t.CursorPosition);
        Assert.AreEqual((0, 0), t.Selection.Start);
        Assert.AreEqual((0, 0), t.Selection.End);

        // Moving left at the start of the document should be a no-op
        t.Movement.MoveLeft();
        Assert.AreEqual((0, 0), t.CursorPosition);
        Assert.AreEqual((0, 0), t.Selection.Start);
        Assert.AreEqual((0, 0), t.Selection.End);
    }

    [TestMethod]
    public void MoveLeftTest2()
    {
        var t = new TextEditor
        {
            AllText = """

                one two three
                3.14
                test.com

                """,
        };

        // Moving left at the start of the document two places should also be a no-op (and not error)
        t.Movement.MoveLeft(2);
        Assert.AreEqual((0, 0), t.CursorPosition);
        Assert.AreEqual((0, 0), t.Selection.Start);
        Assert.AreEqual((0, 0), t.Selection.End);
    }

    [TestMethod]
    public void MoveLeftTest3()
    {
        var t = new TextEditor
        {
            AllText = """

                one two three
                3.14
                test.com

                """,
            CursorPosition = (1, 0),
        };

        Assert.AreEqual((1, 0), t.CursorPosition);
        Assert.AreEqual((1, 0), t.Selection.Start);
        Assert.AreEqual((1, 0), t.Selection.End);

        // Moving left at the start of a line should move to the end of the previous line
        t.Movement.MoveLeft();
        Assert.AreEqual((0, 0), t.CursorPosition);
        Assert.AreEqual((0, 0), t.Selection.Start);
        Assert.AreEqual((0, 0), t.Selection.End);
    }

    [TestMethod]
    public void MoveLeftTest4()
    {
        var t = new TextEditor
        {
            AllText = """

                one two three
                3.14
                test.com

                """,
            CursorPosition = (1, 3),
        };

        // Moving left when not at the start of the line should just move left.
        t.Movement.MoveLeft();
        Assert.AreEqual((1, 2), t.CursorPosition);
        Assert.AreEqual((1, 2), t.Selection.Start);
        Assert.AreEqual((1, 2), t.Selection.End);
    }

    [TestMethod]
    public void MoveLeftSelectTest1()
    {
        var t = new TextEditor
        {
            AllText = """

                one two three
                3.14
                test.com

                """,
        };

        // Moving left at the start of the document while selecting should be a no-op.
        t.Movement.MoveLeft(1, true);
        Assert.AreEqual((0, 0), t.CursorPosition);
        Assert.AreEqual((0, 0), t.Selection.Start);
        Assert.AreEqual((0, 0), t.Selection.End);
    }

    [TestMethod]
    public void MoveLeftSelectTest2()
    {
        var t = new TextEditor
        {
            AllText = """

                one two three
                3.14
                test.com

                """,
            CursorPosition = (1, 3),
        };

        t.Movement.MoveLeft(1, true);
        Assert.AreEqual((1, 2), t.CursorPosition);
        Assert.AreEqual((1, 2), t.Selection.Start);
        Assert.AreEqual((1, 3), t.Selection.End);
    }

    [TestMethod]
    public void MoveLeftSelectTest3()
    {
        var t = new TextEditor
        {
            AllText = """

                one two three
                3.14
                test.com

                """,
            CursorPosition = (1, 0),
        };

        // Moving left at the start of a line while selecting should extend the selection to the end of the previous line.
        t.Movement.MoveLeft(1, true);
        Assert.AreEqual((0, 0), t.CursorPosition);
        Assert.AreEqual((0, 0), t.Selection.Start);
        Assert.AreEqual((1, 0), t.Selection.End);
    }

    [TestMethod]
    public void MoveLeftByWordTest1()
    {
        var t = new TextEditor
        {
            AllText = """

                one two three
                3.14
                test.com

                """,
        };

        // Moving left by a word at the start of the document should be a no-op.
        t.Movement.MoveLeft(1, false, true);
        Assert.AreEqual((0, 0), t.CursorPosition);
        Assert.AreEqual((0, 0), t.Selection.Start);
        Assert.AreEqual((0, 0), t.Selection.End);
    }

    [TestMethod]
    public void MoveLeftSelectByWordTest1()
    {
        var t = new TextEditor
        {
            AllText = """

                one two three
                3.14
                test.com

                """,
        };

        // Moving left by a word at the start of the document while selecting should be a no-op.
        t.Movement.MoveLeft(1, true, true);
        Assert.AreEqual((0, 0), t.CursorPosition);
        Assert.AreEqual((0, 0), t.Selection.Start);
        Assert.AreEqual((0, 0), t.Selection.End);
    }

    [TestMethod]
    public void MoveLeftSelectByWordTest2()
    {
        var t = new TextEditor
        {
            AllText = """

                one two three
                3.14
                test.com

                """,
            Options = { IsColorizerEnabled = false },
            CursorPosition = (1, 3),
        };

        // Moving left by a word while selecting should select the previous word.
        t.Movement.MoveLeft(1, true, true);
        Assert.AreEqual((1, 0), t.CursorPosition);
        Assert.AreEqual((1, 0), t.Selection.Start);
        Assert.AreEqual((1, 3), t.Selection.End);
    }

    [TestMethod]
    public void MoveLeftSelectByWordTest3()
    {
        var t = new TextEditor
        {
            AllText = """

                one two three
                3.14
                test.com

                """,
            Options = { IsColorizerEnabled = false },
            CursorPosition = (1, 4),
        };

        // Moving left by a word while selecting should select the previous word.
        t.Movement.MoveLeft(1, true, true);
        Assert.AreEqual((1, 0), t.CursorPosition);
        Assert.AreEqual((1, 0), t.Selection.Start);
        Assert.AreEqual((1, 4), t.Selection.End);
    }

    [TestMethod]
    public void MoveRightTest1()
    {
        var t = new TextEditor
        {
            AllText = """

                one two three
                3.14
                test.com

                """,
        };

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
        var t = new TextEditor
        {
            AllText = """

                one two three
                3.14
                test.com

                """,
        };

        // Moving right twice on an empty line followed by a non-empty line should go to the next line and then past the first character.
        t.Movement.MoveRight(2);
        Assert.AreEqual((1, 1), t.CursorPosition);
        Assert.AreEqual((1, 1), t.Selection.Start);
        Assert.AreEqual((1, 1), t.Selection.End);
    }

    [TestMethod]
    public void MoveRightTest3()
    {
        var t = new TextEditor
        {
            AllText = """

                one two three
                3.14
                test.com

                """,
            CursorPosition = (4, 0),
        };

        // Moving right at the end of the document should be a no-op.
        t.Movement.MoveRight();
        Assert.AreEqual((4, 0), t.CursorPosition);
        Assert.AreEqual((4, 0), t.Selection.Start);
        Assert.AreEqual((4, 0), t.Selection.End);
    }

    [TestMethod]
    public void MoveRightSelectTest1()
    {
        var t = new TextEditor
        {
            AllText = """

                one two three
                3.14
                test.com

                """,
        };

        // Moving right on an empty line while selecting should extend the selection to the start of the next line.
        t.Movement.MoveRight(1, true);
        Assert.AreEqual((1, 0), t.CursorPosition);
        Assert.AreEqual((0, 0), t.Selection.Start);
        Assert.AreEqual((1, 0), t.Selection.End);

        // Doing it again should extend the selection to include the first char on the following line.
        t.Movement.MoveRight(1, true);
        Assert.AreEqual((1, 1), t.CursorPosition);
        Assert.AreEqual((0, 0), t.Selection.Start);
        Assert.AreEqual((1, 1), t.Selection.End);
    }

    [TestMethod]
    public void MoveRightByWordTest1()
    {
        var t = new TextEditor
        {
            AllText = """

                one two three
                3.14
                test.com

                """,
        };

        // Moving right on an empty line while moving by word should move the cursor to the start of the next line.
        t.Movement.MoveRight(1, false, true);
        Assert.AreEqual((1, 0), t.CursorPosition);
        Assert.AreEqual((1, 0), t.Selection.Start);
        Assert.AreEqual((1, 0), t.Selection.End);

        // Doing it again should move the cursor past the first word on the following line.
        t.Movement.MoveRight(1, false, true);
        Assert.AreEqual((1, 4), t.CursorPosition);
        Assert.AreEqual((1, 4), t.Selection.Start);
        Assert.AreEqual((1, 4), t.Selection.End);
    }

    [TestMethod]
    public void MoveRightSelectByWordTest1()
    {
        var t = new TextEditor
        {
            AllText = """

                one two three
                3.14
                test.com

                """,
            Options = { IsColorizerEnabled = false },
        };

        t.Movement.MoveRight(1, true, true);
        Assert.AreEqual((1, 0), t.CursorPosition);
        Assert.AreEqual((0, 0), t.Selection.Start);
        Assert.AreEqual((1, 0), t.Selection.End);

        t.Movement.MoveRight(1, true, true);
        Assert.AreEqual((1, 4), t.CursorPosition);
        Assert.AreEqual((0, 0), t.Selection.Start);
        Assert.AreEqual((1, 4), t.Selection.End);
    }

    [TestMethod]
    public void MoveRightTest4()
    {
        var t = new TextEditor
        {
            AllText = """

                one two three
                3.14
                test.com

                """,
            CursorPosition = (1, 3),
        };

        t.Movement.MoveRight();
        Assert.AreEqual((1, 4), t.CursorPosition);
        Assert.AreEqual((1, 4), t.Selection.Start);
        Assert.AreEqual((1, 4), t.Selection.End);
    }

    [TestMethod]
    public void MoveRightSelectTest2()
    {
        var t = new TextEditor
        {
            AllText = """

                one two three
                3.14
                test.com

                """,
            CursorPosition = (1, 3),
        };

        t.Movement.MoveRight(1, true);
        Assert.AreEqual((1, 4), t.CursorPosition);
        Assert.AreEqual((1, 3), t.Selection.Start);
        Assert.AreEqual((1, 4), t.Selection.End);
    }

    [TestMethod]
    public void MoveRightSelectByWordTest2()
    {
        var t = new TextEditor
        {
            AllText = """

                one two three
                3.14
                test.com

                """,
            Options = { IsColorizerEnabled = false },
            CursorPosition = (1, 3),
        };

        t.Movement.MoveRight(1, true, true);
        Assert.AreEqual((1, 4), t.CursorPosition);
        Assert.AreEqual((1, 0), t.Selection.Start);
        Assert.AreEqual((1, 4), t.Selection.End);
    }

    [TestMethod]
    public void MoveRightSelectByWordTest3()
    {
        var t = new TextEditor
        {
            AllText = """

                one two three
                3.14
                test.com

                """,
            Options = { IsColorizerEnabled = false },
            CursorPosition = (1, 4),
        };

        t.Movement.MoveRight(1, true, true);
        Assert.AreEqual((1, 8), t.CursorPosition);
        Assert.AreEqual((1, 4), t.Selection.Start);
        Assert.AreEqual((1, 8), t.Selection.End);
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
