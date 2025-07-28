namespace llcomTest;
using LLCOM.Models;

[TestClass]
public class TerminalCommandTest
{
    [TestMethod]
    public void TestSingle()
    {
        var testCases = new (char, TerminalCommand)[]
        {
            ('\b', TerminalCommand.Bs),
            ('\t', TerminalCommand.Ht),
            ('\n', TerminalCommand.Lf),
            ('\r', TerminalCommand.Cr)
        };

        foreach (var (input, expected) in testCases)
        {
            var result = TerminalCommandCheck.Do([input,'=','=','=','=','=','=']);
            Assert.AreEqual(expected, result.Item1.Item1);
        }
    }
    
    [TestMethod]
    public void TestMultiple()
    {
        var testCases = new (string, TerminalCommand, (int, int), int)[]
        {
            ("\x1b[?25l", TerminalCommand.Hide, (25, 0), 6),
            ("\x1b[?25h", TerminalCommand.Show, (25, 0), 6),
            ("\x1b[2J", TerminalCommand.ClearScreen, (2, 0), 4),
            ("\x1b[H", TerminalCommand.ResetCursor, (0, 0), 3),
            ("\x1b[10;20H", TerminalCommand.MoveCursorTo, (10, 20), 8),
            ("\x1b[2A", TerminalCommand.MoveCursorUp, (2, 0), 4),
            ("\x1b[10A", TerminalCommand.MoveCursorUp, (10, 0), 5),
            ("\x1b[m", TerminalCommand.ResetStyle, (0, 0), 3),
            ("\x1b[1;31m", TerminalCommand.MultipleStyle, (1, 31), 7),
            ("\x1b[0m", TerminalCommand.ResetStyle, (0, 0), 4),
            ("\x1b[7m", TerminalCommand.Reverse, (7, 0), 4),
            ("\x1b[31m", TerminalCommand.ForegroundColor, (31, 0), 5),
            ("\x1b[43m", TerminalCommand.BackgroundColor, (43, 0), 5),
            ("\x1b[2004l", TerminalCommand.Unknown, (0, 0), 7),
            ("\x1b[2004h", TerminalCommand.Unknown, (0, 0), 7),
            ("\x1b[A", TerminalCommand.Unknown, (0, 0), 3),
            ("\x1bpsafsdf", TerminalCommand.None, (0, 0), 0),
            ("\x1b666", TerminalCommand.None, (0, 0), 0),
            ("\x111erwe", TerminalCommand.None, (0, 0), 0),
        };

        foreach (var (input, expectedCmd, expectedPos, expectedLength) in testCases)
        {
            var result = TerminalCommandCheck.Do((input+"==========").AsSpan());
            Assert.AreEqual(expectedCmd, result.Item1.Item1);
            Assert.AreEqual(expectedPos, result.Item1.Item2);
            Assert.AreEqual(expectedLength, result.Item2);
        }
    }
}