using Avalonia.Media;
using LLCOM.Models;

namespace LLCOM.Services;

public static class TerminalColorSchemes
{
    /// <summary>
    /// 预置的几个终端主题
    /// 来自https://github.com/microsoft/terminal/blob/22c509f426a7d2cdf616bc18143f5bc24f238c4f/src/cascadia/TerminalSettingsModel/defaults.json#L77-L399
    /// </summary>
    public static TerminalColorScheme[] List =>
    [
        Dimidium,
        Ottosson,
        Campbell,
        Campbell_Powershell,
        Vintage,
        One_Half_Dark,
        One_Half_Light,
        Solarized_Dark,
        Solarized_Light,
        Tango_Dark,
        Tango_Light,
        Dark_Plus,
        CGA,
        IBM_5153
    ];
    
    public static TerminalColorScheme Dimidium = new("Dimidium")
    {
        Foreground = Brush.Parse("#BAB7B6"),
        Background = Brush.Parse("#141414"),
        Cursor = Brush.Parse("#37E57B"),
        SelectionBackground = Brush.Parse("#FFFFFF"),
        Code30 = Brush.Parse("#000000"),
        Code31 = Brush.Parse("#CF494C"),
        Code32 = Brush.Parse("#60B442"),
        Code33 = Brush.Parse("#DB9C11"),
        Code34 = Brush.Parse("#0575D8"),
        Code35 = Brush.Parse("#AF5ED2"),
        Code36 = Brush.Parse("#1DB6BB"),
        Code37 = Brush.Parse("#BAB7B6"),
        Code90 = Brush.Parse("#817E7E"),
        Code91 = Brush.Parse("#FF643B"),
        Code92 = Brush.Parse("#37E57B"),
        Code93 = Brush.Parse("#FCCD1A"),
        Code94 = Brush.Parse("#688DFD"),
        Code95 = Brush.Parse("#ED6FE9"),
        Code96 = Brush.Parse("#32E0FB"),
        Code97 = Brush.Parse("#D3D8D9")
    };

    public static TerminalColorScheme Ottosson = new("Ottosson")
    {
        Foreground = Brush.Parse("#bebebe"),
        Background = Brush.Parse("#000000"),
        Cursor = Brush.Parse("#ffffff"),
        SelectionBackground = Brush.Parse("#92a4fd"),
        Code30 = Brush.Parse("#000000"),
        Code31 = Brush.Parse("#be2c21"),
        Code32 = Brush.Parse("#3fae3a"),
        Code33 = Brush.Parse("#be9a4a"),
        Code34 = Brush.Parse("#204dbe"),
        Code35 = Brush.Parse("#bb54be"),
        Code36 = Brush.Parse("#00a7b2"),
        Code37 = Brush.Parse("#bebebe"),
        Code90 = Brush.Parse("#808080"),
        Code91 = Brush.Parse("#ff3e30"),
        Code92 = Brush.Parse("#58ea51"),
        Code93 = Brush.Parse("#ffc944"),
        Code94 = Brush.Parse("#2f6aff"),
        Code95 = Brush.Parse("#fc74ff"),
        Code96 = Brush.Parse("#00e1f0"),
        Code97 = Brush.Parse("#ffffff")
    };

    public static TerminalColorScheme Campbell = new("Campbell")
    {
        Foreground = Brush.Parse("#CCCCCC"),
        Background = Brush.Parse("#0C0C0C"),
        Cursor = Brush.Parse("#FFFFFF"),
        SelectionBackground = Brush.Parse("#FFFFFF"),
        Code30 = Brush.Parse("#0C0C0C"),
        Code31 = Brush.Parse("#C50F1F"),
        Code32 = Brush.Parse("#13A10E"),
        Code33 = Brush.Parse("#C19C00"),
        Code34 = Brush.Parse("#0037DA"),
        Code35 = Brush.Parse("#881798"),
        Code36 = Brush.Parse("#3A96DD"),
        Code37 = Brush.Parse("#CCCCCC"),
        Code90 = Brush.Parse("#767676"),
        Code91 = Brush.Parse("#E74856"),
        Code92 = Brush.Parse("#16C60C"),
        Code93 = Brush.Parse("#F9F1A5"),
        Code94 = Brush.Parse("#3B78FF"),
        Code95 = Brush.Parse("#B4009E"),
        Code96 = Brush.Parse("#61D6D6"),
        Code97 = Brush.Parse("#F2F2F2")
    };

    public static TerminalColorScheme Campbell_Powershell = new("Campbell Powershell")
    {
        Foreground = Brush.Parse("#CCCCCC"),
        Background = Brush.Parse("#012456"),
        Cursor = Brush.Parse("#FFFFFF"),
        SelectionBackground = Brush.Parse("#FFFFFF"),
        Code30 = Brush.Parse("#0C0C0C"),
        Code31 = Brush.Parse("#C50F1F"),
        Code32 = Brush.Parse("#13A10E"),
        Code33 = Brush.Parse("#C19C00"),
        Code34 = Brush.Parse("#0037DA"),
        Code35 = Brush.Parse("#881798"),
        Code36 = Brush.Parse("#3A96DD"),
        Code37 = Brush.Parse("#CCCCCC"),
        Code90 = Brush.Parse("#767676"),
        Code91 = Brush.Parse("#E74856"),
        Code92 = Brush.Parse("#16C60C"),
        Code93 = Brush.Parse("#F9F1A5"),
        Code94 = Brush.Parse("#3B78FF"),
        Code95 = Brush.Parse("#B4009E"),
        Code96 = Brush.Parse("#61D6D6"),
        Code97 = Brush.Parse("#F2F2F2")
    };

    public static TerminalColorScheme Vintage = new("Vintage")
    {
        Foreground = Brush.Parse("#C0C0C0"),
        Background = Brush.Parse("#000000"),
        Cursor = Brush.Parse("#FFFFFF"),
        SelectionBackground = Brush.Parse("#FFFFFF"),
        Code30 = Brush.Parse("#000000"),
        Code31 = Brush.Parse("#800000"),
        Code32 = Brush.Parse("#008000"),
        Code33 = Brush.Parse("#808000"),
        Code34 = Brush.Parse("#000080"),
        Code35 = Brush.Parse("#800080"),
        Code36 = Brush.Parse("#008080"),
        Code37 = Brush.Parse("#C0C0C0"),
        Code90 = Brush.Parse("#808080"),
        Code91 = Brush.Parse("#FF0000"),
        Code92 = Brush.Parse("#00FF00"),
        Code93 = Brush.Parse("#FFFF00"),
        Code94 = Brush.Parse("#0000FF"),
        Code95 = Brush.Parse("#FF00FF"),
        Code96 = Brush.Parse("#00FFFF"),
        Code97 = Brush.Parse("#FFFFFF")
    };

    public static TerminalColorScheme One_Half_Dark = new("One Half Dark")
    {
        Foreground = Brush.Parse("#DCDFE4"),
        Background = Brush.Parse("#282C34"),
        Cursor = Brush.Parse("#FFFFFF"),
        SelectionBackground = Brush.Parse("#FFFFFF"),
        Code30 = Brush.Parse("#282C34"),
        Code31 = Brush.Parse("#E06C75"),
        Code32 = Brush.Parse("#98C379"),
        Code33 = Brush.Parse("#E5C07B"),
        Code34 = Brush.Parse("#61AFEF"),
        Code35 = Brush.Parse("#C678DD"),
        Code36 = Brush.Parse("#56B6C2"),
        Code37 = Brush.Parse("#DCDFE4"),
        Code90 = Brush.Parse("#5A6374"),
        Code91 = Brush.Parse("#E06C75"),
        Code92 = Brush.Parse("#98C379"),
        Code93 = Brush.Parse("#E5C07B"),
        Code94 = Brush.Parse("#61AFEF"),
        Code95 = Brush.Parse("#C678DD"),
        Code96 = Brush.Parse("#56B6C2"),
        Code97 = Brush.Parse("#DCDFE4")
    };

    public static TerminalColorScheme One_Half_Light = new("One Half Light")
    {
        Foreground = Brush.Parse("#383A42"),
        Background = Brush.Parse("#FAFAFA"),
        Cursor = Brush.Parse("#4F525D"),
        SelectionBackground = Brush.Parse("#383A42"),
        Code30 = Brush.Parse("#383A42"),
        Code31 = Brush.Parse("#E45649"),
        Code32 = Brush.Parse("#50A14F"),
        Code33 = Brush.Parse("#C18301"),
        Code34 = Brush.Parse("#0184BC"),
        Code35 = Brush.Parse("#A626A4"),
        Code36 = Brush.Parse("#0997B3"),
        Code37 = Brush.Parse("#FAFAFA"),
        Code90 = Brush.Parse("#4F525D"),
        Code91 = Brush.Parse("#DF6C75"),
        Code92 = Brush.Parse("#98C379"),
        Code93 = Brush.Parse("#E4C07A"),
        Code94 = Brush.Parse("#61AFEF"),
        Code95 = Brush.Parse("#C577DD"),
        Code96 = Brush.Parse("#56B5C1"),
        Code97 = Brush.Parse("#FFFFFF")
    };

    public static TerminalColorScheme Solarized_Dark = new("Solarized Dark")
    {
        Foreground = Brush.Parse("#839496"),
        Background = Brush.Parse("#002B36"),
        Cursor = Brush.Parse("#FFFFFF"),
        SelectionBackground = Brush.Parse("#FFFFFF"),
        Code30 = Brush.Parse("#002B36"),
        Code31 = Brush.Parse("#DC322F"),
        Code32 = Brush.Parse("#859900"),
        Code33 = Brush.Parse("#B58900"),
        Code34 = Brush.Parse("#268BD2"),
        Code35 = Brush.Parse("#D33682"),
        Code36 = Brush.Parse("#2AA198"),
        Code37 = Brush.Parse("#EEE8D5"),
        Code90 = Brush.Parse("#073642"),
        Code91 = Brush.Parse("#CB4B16"),
        Code92 = Brush.Parse("#586E75"),
        Code93 = Brush.Parse("#657B83"),
        Code94 = Brush.Parse("#839496"),
        Code95 = Brush.Parse("#6C71C4"),
        Code96 = Brush.Parse("#93A1A1"),
        Code97 = Brush.Parse("#FDF6E3")
    };

    public static TerminalColorScheme Solarized_Light = new("Solarized Light")
    {
        Foreground = Brush.Parse("#657B83"),
        Background = Brush.Parse("#FDF6E3"),
        Cursor = Brush.Parse("#002B36"),
        SelectionBackground = Brush.Parse("#2C4D57"),
        Code30 = Brush.Parse("#002B36"),
        Code31 = Brush.Parse("#DC322F"),
        Code32 = Brush.Parse("#859900"),
        Code33 = Brush.Parse("#B58900"),
        Code34 = Brush.Parse("#268BD2"),
        Code35 = Brush.Parse("#D33682"),
        Code36 = Brush.Parse("#2AA198"),
        Code37 = Brush.Parse("#EEE8D5"),
        Code90 = Brush.Parse("#073642"),
        Code91 = Brush.Parse("#CB4B16"),
        Code92 = Brush.Parse("#586E75"),
        Code93 = Brush.Parse("#657B83"),
        Code94 = Brush.Parse("#839496"),
        Code95 = Brush.Parse("#6C71C4"),
        Code96 = Brush.Parse("#93A1A1"),
        Code97 = Brush.Parse("#FDF6E3")
    };

    public static TerminalColorScheme Tango_Dark = new("Tango Dark")
    {
        Foreground = Brush.Parse("#D3D7CF"),
        Background = Brush.Parse("#000000"),
        Cursor = Brush.Parse("#FFFFFF"),
        SelectionBackground = Brush.Parse("#FFFFFF"),
        Code30 = Brush.Parse("#000000"),
        Code31 = Brush.Parse("#CC0000"),
        Code32 = Brush.Parse("#4E9A06"),
        Code33 = Brush.Parse("#C4A000"),
        Code34 = Brush.Parse("#3465A4"),
        Code35 = Brush.Parse("#75507B"),
        Code36 = Brush.Parse("#06989A"),
        Code37 = Brush.Parse("#D3D7CF"),
        Code90 = Brush.Parse("#555753"),
        Code91 = Brush.Parse("#EF2929"),
        Code92 = Brush.Parse("#8AE234"),
        Code93 = Brush.Parse("#FCE94F"),
        Code94 = Brush.Parse("#729FCF"),
        Code95 = Brush.Parse("#AD7FA8"),
        Code96 = Brush.Parse("#34E2E2"),
        Code97 = Brush.Parse("#EEEEEC")
    };

    public static TerminalColorScheme Tango_Light = new("Tango Light")
    {
        Foreground = Brush.Parse("#555753"),
        Background = Brush.Parse("#FFFFFF"),
        Cursor = Brush.Parse("#000000"),
        SelectionBackground = Brush.Parse("#141414"),
        Code30 = Brush.Parse("#000000"),
        Code31 = Brush.Parse("#CC0000"),
        Code32 = Brush.Parse("#4E9A06"),
        Code33 = Brush.Parse("#C4A000"),
        Code34 = Brush.Parse("#3465A4"),
        Code35 = Brush.Parse("#75507B"),
        Code36 = Brush.Parse("#06989A"),
        Code37 = Brush.Parse("#D3D7CF"),
        Code90 = Brush.Parse("#555753"),
        Code91 = Brush.Parse("#EF2929"),
        Code92 = Brush.Parse("#8AE234"),
        Code93 = Brush.Parse("#FCE94F"),
        Code94 = Brush.Parse("#729FCF"),
        Code95 = Brush.Parse("#AD7FA8"),
        Code96 = Brush.Parse("#34E2E2"),
        Code97 = Brush.Parse("#EEEEEC")
    };

    public static TerminalColorScheme Dark_Plus = new("Dark+")
    {
        Foreground = Brush.Parse("#cccccc"),
        Background = Brush.Parse("#1e1e1e"),
        Cursor = Brush.Parse("#808080"),
        SelectionBackground = Brush.Parse("#ffffff"),
        Code30 = Brush.Parse("#000000"),
        Code31 = Brush.Parse("#cd3131"),
        Code32 = Brush.Parse("#0dbc79"),
        Code33 = Brush.Parse("#e5e510"),
        Code34 = Brush.Parse("#2472c8"),
        Code35 = Brush.Parse("#bc3fbc"),
        Code36 = Brush.Parse("#11a8cd"),
        Code37 = Brush.Parse("#e5e5e5"),
        Code90 = Brush.Parse("#666666"),
        Code91 = Brush.Parse("#f14c4c"),
        Code92 = Brush.Parse("#23d18b"),
        Code93 = Brush.Parse("#f5f543"),
        Code94 = Brush.Parse("#3b8eea"),
        Code95 = Brush.Parse("#d670d6"),
        Code96 = Brush.Parse("#29b8db"),
        Code97 = Brush.Parse("#e5e5e5")
    };

    public static TerminalColorScheme CGA = new("CGA")
    {
        Foreground = Brush.Parse("#AAAAAA"),
        Background = Brush.Parse("#000000"),
        Cursor = Brush.Parse("#00AA00"),
        SelectionBackground = Brush.Parse("#FFFFFF"),
        Code30 = Brush.Parse("#000000"),
        Code31 = Brush.Parse("#AA0000"),
        Code32 = Brush.Parse("#00AA00"),
        Code33 = Brush.Parse("#AA5500"),
        Code34 = Brush.Parse("#0000AA"),
        Code35 = Brush.Parse("#AA00AA"),
        Code36 = Brush.Parse("#00AAAA"),
        Code37 = Brush.Parse("#AAAAAA"),
        Code90 = Brush.Parse("#555555"),
        Code91 = Brush.Parse("#FF5555"),
        Code92 = Brush.Parse("#55FF55"),
        Code93 = Brush.Parse("#FFFF55"),
        Code94 = Brush.Parse("#5555FF"),
        Code95 = Brush.Parse("#FF55FF"),
        Code96 = Brush.Parse("#55FFFF"),
        Code97 = Brush.Parse("#FFFFFF")
    };

    public static TerminalColorScheme IBM_5153 = new("IBM 5153")
    {
        Foreground = Brush.Parse("#AAAAAA"),
        Background = Brush.Parse("#000000"),
        Cursor = Brush.Parse("#00AA00"),
        SelectionBackground = Brush.Parse("#FFFFFF"),
        Code30 = Brush.Parse("#000000"),
        Code31 = Brush.Parse("#AA0000"),
        Code32 = Brush.Parse("#00AA00"),
        Code33 = Brush.Parse("#C47E00"),
        Code34 = Brush.Parse("#0000AA"),
        Code35 = Brush.Parse("#AA00AA"),
        Code36 = Brush.Parse("#00AAAA"),
        Code37 = Brush.Parse("#AAAAAA"),
        Code90 = Brush.Parse("#555555"),
        Code91 = Brush.Parse("#FF5555"),
        Code92 = Brush.Parse("#55FF55"),
        Code93 = Brush.Parse("#FFFF55"),
        Code94 = Brush.Parse("#5555FF"),
        Code95 = Brush.Parse("#FF55FF"),
        Code96 = Brush.Parse("#55FFFF"),
        Code97 = Brush.Parse("#FFFFFF")
    };
}