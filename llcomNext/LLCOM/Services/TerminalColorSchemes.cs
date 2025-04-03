using Avalonia.Media;
using LLCOM.Models;

namespace LLCOM.Services;

public static class TerminalColorSchemes
{
    public static TerminalColorScheme[] TerminalColorSchemesList =>
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
        Code40 = Brush.Parse("#000000"),
        Code41 = Brush.Parse("#CF494C"),
        Code42 = Brush.Parse("#60B442"),
        Code43 = Brush.Parse("#DB9C11"),
        Code44 = Brush.Parse("#0575D8"),
        Code45 = Brush.Parse("#AF5ED2"),
        Code46 = Brush.Parse("#1DB6BB"),
        Code47 = Brush.Parse("#BAB7B6"),
        Code30 = Brush.Parse("#817E7E"),
        Code31 = Brush.Parse("#FF643B"),
        Code32 = Brush.Parse("#37E57B"),
        Code33 = Brush.Parse("#FCCD1A"),
        Code34 = Brush.Parse("#688DFD"),
        Code35 = Brush.Parse("#ED6FE9"),
        Code36 = Brush.Parse("#32E0FB"),
        Code37 = Brush.Parse("#D3D8D9")
    };

    public static TerminalColorScheme Ottosson = new("Ottosson")
    {
        Foreground = Brush.Parse("#bebebe"),
        Background = Brush.Parse("#000000"),
        Cursor = Brush.Parse("#ffffff"),
        SelectionBackground = Brush.Parse("#92a4fd"),
        Code40 = Brush.Parse("#000000"),
        Code41 = Brush.Parse("#be2c21"),
        Code42 = Brush.Parse("#3fae3a"),
        Code43 = Brush.Parse("#be9a4a"),
        Code44 = Brush.Parse("#204dbe"),
        Code45 = Brush.Parse("#bb54be"),
        Code46 = Brush.Parse("#00a7b2"),
        Code47 = Brush.Parse("#bebebe"),
        Code30 = Brush.Parse("#808080"),
        Code31 = Brush.Parse("#ff3e30"),
        Code32 = Brush.Parse("#58ea51"),
        Code33 = Brush.Parse("#ffc944"),
        Code34 = Brush.Parse("#2f6aff"),
        Code35 = Brush.Parse("#fc74ff"),
        Code36 = Brush.Parse("#00e1f0"),
        Code37 = Brush.Parse("#ffffff")
    };

    public static TerminalColorScheme Campbell = new("Campbell")
    {
        Foreground = Brush.Parse("#CCCCCC"),
        Background = Brush.Parse("#0C0C0C"),
        Cursor = Brush.Parse("#FFFFFF"),
        SelectionBackground = Brush.Parse("#FFFFFF"),
        Code40 = Brush.Parse("#0C0C0C"),
        Code41 = Brush.Parse("#C50F1F"),
        Code42 = Brush.Parse("#13A10E"),
        Code43 = Brush.Parse("#C19C00"),
        Code44 = Brush.Parse("#0037DA"),
        Code45 = Brush.Parse("#881798"),
        Code46 = Brush.Parse("#3A96DD"),
        Code47 = Brush.Parse("#CCCCCC"),
        Code30 = Brush.Parse("#767676"),
        Code31 = Brush.Parse("#E74856"),
        Code32 = Brush.Parse("#16C60C"),
        Code33 = Brush.Parse("#F9F1A5"),
        Code34 = Brush.Parse("#3B78FF"),
        Code35 = Brush.Parse("#B4009E"),
        Code36 = Brush.Parse("#61D6D6"),
        Code37 = Brush.Parse("#F2F2F2")
    };

    public static TerminalColorScheme Campbell_Powershell = new("Campbell Powershell")
    {
        Foreground = Brush.Parse("#CCCCCC"),
        Background = Brush.Parse("#012456"),
        Cursor = Brush.Parse("#FFFFFF"),
        SelectionBackground = Brush.Parse("#FFFFFF"),
        Code40 = Brush.Parse("#0C0C0C"),
        Code41 = Brush.Parse("#C50F1F"),
        Code42 = Brush.Parse("#13A10E"),
        Code43 = Brush.Parse("#C19C00"),
        Code44 = Brush.Parse("#0037DA"),
        Code45 = Brush.Parse("#881798"),
        Code46 = Brush.Parse("#3A96DD"),
        Code47 = Brush.Parse("#CCCCCC"),
        Code30 = Brush.Parse("#767676"),
        Code31 = Brush.Parse("#E74856"),
        Code32 = Brush.Parse("#16C60C"),
        Code33 = Brush.Parse("#F9F1A5"),
        Code34 = Brush.Parse("#3B78FF"),
        Code35 = Brush.Parse("#B4009E"),
        Code36 = Brush.Parse("#61D6D6"),
        Code37 = Brush.Parse("#F2F2F2")
    };

    public static TerminalColorScheme Vintage = new("Vintage")
    {
        Foreground = Brush.Parse("#C0C0C0"),
        Background = Brush.Parse("#000000"),
        Cursor = Brush.Parse("#FFFFFF"),
        SelectionBackground = Brush.Parse("#FFFFFF"),
        Code40 = Brush.Parse("#000000"),
        Code41 = Brush.Parse("#800000"),
        Code42 = Brush.Parse("#008000"),
        Code43 = Brush.Parse("#808000"),
        Code44 = Brush.Parse("#000080"),
        Code45 = Brush.Parse("#800080"),
        Code46 = Brush.Parse("#008080"),
        Code47 = Brush.Parse("#C0C0C0"),
        Code30 = Brush.Parse("#808080"),
        Code31 = Brush.Parse("#FF0000"),
        Code32 = Brush.Parse("#00FF00"),
        Code33 = Brush.Parse("#FFFF00"),
        Code34 = Brush.Parse("#0000FF"),
        Code35 = Brush.Parse("#FF00FF"),
        Code36 = Brush.Parse("#00FFFF"),
        Code37 = Brush.Parse("#FFFFFF")
    };

    public static TerminalColorScheme One_Half_Dark = new("One Half Dark")
    {
        Foreground = Brush.Parse("#DCDFE4"),
        Background = Brush.Parse("#282C34"),
        Cursor = Brush.Parse("#FFFFFF"),
        SelectionBackground = Brush.Parse("#FFFFFF"),
        Code40 = Brush.Parse("#282C34"),
        Code41 = Brush.Parse("#E06C75"),
        Code42 = Brush.Parse("#98C379"),
        Code43 = Brush.Parse("#E5C07B"),
        Code44 = Brush.Parse("#61AFEF"),
        Code45 = Brush.Parse("#C678DD"),
        Code46 = Brush.Parse("#56B6C2"),
        Code47 = Brush.Parse("#DCDFE4"),
        Code30 = Brush.Parse("#5A6374"),
        Code31 = Brush.Parse("#E06C75"),
        Code32 = Brush.Parse("#98C379"),
        Code33 = Brush.Parse("#E5C07B"),
        Code34 = Brush.Parse("#61AFEF"),
        Code35 = Brush.Parse("#C678DD"),
        Code36 = Brush.Parse("#56B6C2"),
        Code37 = Brush.Parse("#DCDFE4")
    };

    public static TerminalColorScheme One_Half_Light = new("One Half Light")
    {
        Foreground = Brush.Parse("#383A42"),
        Background = Brush.Parse("#FAFAFA"),
        Cursor = Brush.Parse("#4F525D"),
        SelectionBackground = Brush.Parse("#383A42"),
        Code40 = Brush.Parse("#383A42"),
        Code41 = Brush.Parse("#E45649"),
        Code42 = Brush.Parse("#50A14F"),
        Code43 = Brush.Parse("#C18301"),
        Code44 = Brush.Parse("#0184BC"),
        Code45 = Brush.Parse("#A626A4"),
        Code46 = Brush.Parse("#0997B3"),
        Code47 = Brush.Parse("#FAFAFA"),
        Code30 = Brush.Parse("#4F525D"),
        Code31 = Brush.Parse("#DF6C75"),
        Code32 = Brush.Parse("#98C379"),
        Code33 = Brush.Parse("#E4C07A"),
        Code34 = Brush.Parse("#61AFEF"),
        Code35 = Brush.Parse("#C577DD"),
        Code36 = Brush.Parse("#56B5C1"),
        Code37 = Brush.Parse("#FFFFFF")
    };

    public static TerminalColorScheme Solarized_Dark = new("Solarized Dark")
    {
        Foreground = Brush.Parse("#839496"),
        Background = Brush.Parse("#002B36"),
        Cursor = Brush.Parse("#FFFFFF"),
        SelectionBackground = Brush.Parse("#FFFFFF"),
        Code40 = Brush.Parse("#002B36"),
        Code41 = Brush.Parse("#DC322F"),
        Code42 = Brush.Parse("#859900"),
        Code43 = Brush.Parse("#B58900"),
        Code44 = Brush.Parse("#268BD2"),
        Code45 = Brush.Parse("#D33682"),
        Code46 = Brush.Parse("#2AA198"),
        Code47 = Brush.Parse("#EEE8D5"),
        Code30 = Brush.Parse("#073642"),
        Code31 = Brush.Parse("#CB4B16"),
        Code32 = Brush.Parse("#586E75"),
        Code33 = Brush.Parse("#657B83"),
        Code34 = Brush.Parse("#839496"),
        Code35 = Brush.Parse("#6C71C4"),
        Code36 = Brush.Parse("#93A1A1"),
        Code37 = Brush.Parse("#FDF6E3")
    };

    public static TerminalColorScheme Solarized_Light = new("Solarized Light")
    {
        Foreground = Brush.Parse("#657B83"),
        Background = Brush.Parse("#FDF6E3"),
        Cursor = Brush.Parse("#002B36"),
        SelectionBackground = Brush.Parse("#2C4D57"),
        Code40 = Brush.Parse("#002B36"),
        Code41 = Brush.Parse("#DC322F"),
        Code42 = Brush.Parse("#859900"),
        Code43 = Brush.Parse("#B58900"),
        Code44 = Brush.Parse("#268BD2"),
        Code45 = Brush.Parse("#D33682"),
        Code46 = Brush.Parse("#2AA198"),
        Code47 = Brush.Parse("#EEE8D5"),
        Code30 = Brush.Parse("#073642"),
        Code31 = Brush.Parse("#CB4B16"),
        Code32 = Brush.Parse("#586E75"),
        Code33 = Brush.Parse("#657B83"),
        Code34 = Brush.Parse("#839496"),
        Code35 = Brush.Parse("#6C71C4"),
        Code36 = Brush.Parse("#93A1A1"),
        Code37 = Brush.Parse("#FDF6E3")
    };

    public static TerminalColorScheme Tango_Dark = new("Tango Dark")
    {
        Foreground = Brush.Parse("#D3D7CF"),
        Background = Brush.Parse("#000000"),
        Cursor = Brush.Parse("#FFFFFF"),
        SelectionBackground = Brush.Parse("#FFFFFF"),
        Code40 = Brush.Parse("#000000"),
        Code41 = Brush.Parse("#CC0000"),
        Code42 = Brush.Parse("#4E9A06"),
        Code43 = Brush.Parse("#C4A000"),
        Code44 = Brush.Parse("#3465A4"),
        Code45 = Brush.Parse("#75507B"),
        Code46 = Brush.Parse("#06989A"),
        Code47 = Brush.Parse("#D3D7CF"),
        Code30 = Brush.Parse("#555753"),
        Code31 = Brush.Parse("#EF2929"),
        Code32 = Brush.Parse("#8AE234"),
        Code33 = Brush.Parse("#FCE94F"),
        Code34 = Brush.Parse("#729FCF"),
        Code35 = Brush.Parse("#AD7FA8"),
        Code36 = Brush.Parse("#34E2E2"),
        Code37 = Brush.Parse("#EEEEEC")
    };

    public static TerminalColorScheme Tango_Light = new("Tango Light")
    {
        Foreground = Brush.Parse("#555753"),
        Background = Brush.Parse("#FFFFFF"),
        Cursor = Brush.Parse("#000000"),
        SelectionBackground = Brush.Parse("#141414"),
        Code40 = Brush.Parse("#000000"),
        Code41 = Brush.Parse("#CC0000"),
        Code42 = Brush.Parse("#4E9A06"),
        Code43 = Brush.Parse("#C4A000"),
        Code44 = Brush.Parse("#3465A4"),
        Code45 = Brush.Parse("#75507B"),
        Code46 = Brush.Parse("#06989A"),
        Code47 = Brush.Parse("#D3D7CF"),
        Code30 = Brush.Parse("#555753"),
        Code31 = Brush.Parse("#EF2929"),
        Code32 = Brush.Parse("#8AE234"),
        Code33 = Brush.Parse("#FCE94F"),
        Code34 = Brush.Parse("#729FCF"),
        Code35 = Brush.Parse("#AD7FA8"),
        Code36 = Brush.Parse("#34E2E2"),
        Code37 = Brush.Parse("#EEEEEC")
    };

    public static TerminalColorScheme Dark_Plus = new("Dark+")
    {
        Foreground = Brush.Parse("#cccccc"),
        Background = Brush.Parse("#1e1e1e"),
        Cursor = Brush.Parse("#808080"),
        SelectionBackground = Brush.Parse("#ffffff"),
        Code40 = Brush.Parse("#000000"),
        Code41 = Brush.Parse("#cd3131"),
        Code42 = Brush.Parse("#0dbc79"),
        Code43 = Brush.Parse("#e5e510"),
        Code44 = Brush.Parse("#2472c8"),
        Code45 = Brush.Parse("#bc3fbc"),
        Code46 = Brush.Parse("#11a8cd"),
        Code47 = Brush.Parse("#e5e5e5"),
        Code30 = Brush.Parse("#666666"),
        Code31 = Brush.Parse("#f14c4c"),
        Code32 = Brush.Parse("#23d18b"),
        Code33 = Brush.Parse("#f5f543"),
        Code34 = Brush.Parse("#3b8eea"),
        Code35 = Brush.Parse("#d670d6"),
        Code36 = Brush.Parse("#29b8db"),
        Code37 = Brush.Parse("#e5e5e5")
    };

    public static TerminalColorScheme CGA = new("CGA")
    {
        Foreground = Brush.Parse("#AAAAAA"),
        Background = Brush.Parse("#000000"),
        Cursor = Brush.Parse("#00AA00"),
        SelectionBackground = Brush.Parse("#FFFFFF"),
        Code40 = Brush.Parse("#000000"),
        Code41 = Brush.Parse("#AA0000"),
        Code42 = Brush.Parse("#00AA00"),
        Code43 = Brush.Parse("#AA5500"),
        Code44 = Brush.Parse("#0000AA"),
        Code45 = Brush.Parse("#AA00AA"),
        Code46 = Brush.Parse("#00AAAA"),
        Code47 = Brush.Parse("#AAAAAA"),
        Code30 = Brush.Parse("#555555"),
        Code31 = Brush.Parse("#FF5555"),
        Code32 = Brush.Parse("#55FF55"),
        Code33 = Brush.Parse("#FFFF55"),
        Code34 = Brush.Parse("#5555FF"),
        Code35 = Brush.Parse("#FF55FF"),
        Code36 = Brush.Parse("#55FFFF"),
        Code37 = Brush.Parse("#FFFFFF")
    };

    public static TerminalColorScheme IBM_5153 = new("IBM 5153")
    {
        Foreground = Brush.Parse("#AAAAAA"),
        Background = Brush.Parse("#000000"),
        Cursor = Brush.Parse("#00AA00"),
        SelectionBackground = Brush.Parse("#FFFFFF"),
        Code40 = Brush.Parse("#000000"),
        Code41 = Brush.Parse("#AA0000"),
        Code42 = Brush.Parse("#00AA00"),
        Code43 = Brush.Parse("#C47E00"),
        Code44 = Brush.Parse("#0000AA"),
        Code45 = Brush.Parse("#AA00AA"),
        Code46 = Brush.Parse("#00AAAA"),
        Code47 = Brush.Parse("#AAAAAA"),
        Code30 = Brush.Parse("#555555"),
        Code31 = Brush.Parse("#FF5555"),
        Code32 = Brush.Parse("#55FF55"),
        Code33 = Brush.Parse("#FFFF55"),
        Code34 = Brush.Parse("#5555FF"),
        Code35 = Brush.Parse("#FF55FF"),
        Code36 = Brush.Parse("#55FFFF"),
        Code37 = Brush.Parse("#FFFFFF")
    };
}