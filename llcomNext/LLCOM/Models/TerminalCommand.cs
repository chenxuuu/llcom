using System;
using System.Collections.Generic;

namespace LLCOM.Models;

public enum TerminalCommand
{
    None, //没匹配上任何命令
    
    Bs, //退格 0x08
    Ht, //水平制表符 0x09
    Lf, //换行 0x0A
    Cr, //回车 0x0D
    
    Hide, //隐藏光标 \x1b[?25l
    Show, //显示光标 \x1b[?25h
    
    ClearLineEnd, //清除光标到行尾 \x1b[K
    ClearLineStart, //清除光标到行首 \x1b[1K
    ClearLine, //清除当前行 \x1b[2K
    
    ClearScreenEnd, //清除光标到屏幕末尾 \x1b[J
    ClearScreenStart, //清除光标到屏幕开头 \x1b[1J
    ClearScreen, //清除屏幕 \x1b[2J
    
    MoveCursorUp, //光标上移 \x1b[{n}A
    MoveCursorDown, //光标下移 \x1b[{n}B
    MoveCursorRight, //光标右移 \x1b[{n}C
    MoveCursorLeft, //光标左移 \x1b[{n}D
    ResetCursor, //光标移动到左上角 \x1b[H
    MoveCursorTo, //光标移动到指定位置 \x1b[{n};{m}H
    SaveCursor, //保存光标位置 \x1b[s
    RestoreCursor, //恢复光标位置 \x1b[u
    
    ResetStyle, //重置样式 \x1b[m
    Bold, //加粗 \x1b[1m
    Underline, //下划线 \x1b[4m
    Reverse, //反转颜色 \x1b[7m
    ForegroundColor, //前景色 \x1b[3{n}m
    BackgroundColor, //背景色 \x1b[4{n}m
}

public class TerminalCommandCheck
{
    /// <summary>
    /// 分析给定的字符切片，判断是否为终端命令
    /// </summary>
    /// <param name="slice">切片，函数将会判断开头</param>
    /// <returns></returns>
    public static ((TerminalCommand, (int, int)), int) Do(ReadOnlySpan<char> slice)
    {
        if(slice.Length == 0)
            return ((TerminalCommand.None,(0,0)), 0);
        //先判断下是否为单字符命令
        var singleCmd = slice[0] switch
        {
            '\b' => TerminalCommand.Bs, //退格
            '\t' => TerminalCommand.Ht, //水平制表符
            '\n' => TerminalCommand.Lf, //换行
            '\r' => TerminalCommand.Cr, //回车
            _ => TerminalCommand.None
        };
        if (singleCmd != TerminalCommand.None)
        {
            return ((singleCmd, (0,0)), 1);
        }
        //判断是否为转义字符
        if(slice[0] != '\x1b' || slice[1] != '[' || slice.Length < 3)
        {
            return ((TerminalCommand.None, (0,0)), 0);
        }
        //看看是否为显示/隐藏光标
        if (slice.Length >= 6 && slice[2] == '?' && slice[3] == '2' && slice[4] == '5')
        {
            if (slice[5] == 'l')
                return ((TerminalCommand.Hide, (25,0)), 6);
            if (slice[5] == 'h')
                return ((TerminalCommand.Show, (25,0)), 6);
        }
        //其他命令就按正常格式分析
        //\x1b[{数字}{字母} 数字可能是2个字符也可能不存在
        int code = 0;
        char cmd = '\0';
        int i = 2; //从第三个字符开始分析，最多分析到第四个字符
        while (i < slice.Length && i < 5 && char.IsDigit(slice[i]))
        {
            code = code * 10 + (slice[i] - '0'); //将数字字符转换为数字
            i++;
        }
        if (i < slice.Length)
        {
            cmd = slice[i];
            i++;
        }
        if(cmd == '\0')
        {
            return ((TerminalCommand.None,(0,0)), 0); //没有命令
        }
        //根据命令字符返回对应的命令
        switch (cmd)
        {
            case 'K': //清除行
                var kr = code switch
                {
                    2 => TerminalCommand.ClearLineEnd, //清除光标到行尾
                    3 => TerminalCommand.ClearLineStart, //清除光标到行首
                    4 => TerminalCommand.ClearLine, //清除当前行
                    _ => TerminalCommand.None //不匹配
                };
                if(kr != TerminalCommand.None)
                    return ((kr, (code,0)), i);
                break;
            case 'J': //清除屏幕
                var jr = code switch
                {
                    0 => TerminalCommand.ClearScreenEnd, //清除光标到屏幕末尾
                    1 => TerminalCommand.ClearScreenStart, //清除光标到屏幕开头
                    2 => TerminalCommand.ClearScreen, //清除屏幕
                    _ => TerminalCommand.None //不匹配
                };
                if(jr != TerminalCommand.None)
                    return ((jr, (code,0)), i);
                break;
            case 'A': //光标上移
                if (code > 0)
                    return ((TerminalCommand.MoveCursorUp, (code,0)), i);
                break;
            case 'B': //光标下移
                if (code > 0)
                    return ((TerminalCommand.MoveCursorDown, (code,0)), i);
                break;
            case 'C': //光标右移
                if (code > 0)
                    return ((TerminalCommand.MoveCursorRight, (code,0)), i);
                break;
            case 'D': //光标左移
                if (code > 0)
                    return ((TerminalCommand.MoveCursorLeft, (code,0)), i);
                break;
            case 'H': //光标移动到指定位置
                return ((TerminalCommand.ResetCursor, (code,0)), i);
            case 's': //保存光标位置
                return ((TerminalCommand.SaveCursor, (0,0)), i);
            case 'u': //恢复光标位置
                return ((TerminalCommand.RestoreCursor, (0,0)), i);
            case 'm': //样式
                var mr = code switch
                {
                    0 => TerminalCommand.ResetStyle, //重置样式
                    1 => TerminalCommand.Bold, //加粗
                    4 => TerminalCommand.Underline, //下划线
                    7 => TerminalCommand.Reverse, //反转颜色
                    _ when code >= 30 && code <= 37 => TerminalCommand.ForegroundColor, //前景色
                    _ when code >= 40 && code <= 47 => TerminalCommand.BackgroundColor, //背景色
                    _ => TerminalCommand.None //不匹配
                };
                if (mr != TerminalCommand.None)
                {
                    return ((mr, (code,0)), i);
                }
                break;
        }
        //检查是不是匹配\x1b[{n};{m}H
        if(cmd != ';')
            return ((TerminalCommand.None,(0,0)), 0);
        //如果是分号，说明可能是光标移动到指定位置
        //需要检查后面的数字
        int col = code, row = 0;
        while (i < slice.Length && char.IsDigit(slice[i]))
        {
            row = row * 10 + (slice[i] - '0'); //将数字字符转换为数字
            i++;
        }
        if (i < slice.Length && slice[i] == 'H')
        {
            return ((TerminalCommand.MoveCursorTo, (col, row)), i + 1);
        }
        return ((TerminalCommand.None,(0,0)), 0);
    }

    /// <summary>
    /// 分析给定的字符数组，判断是否为终端命令
    /// </summary>
    /// <param name="arr">数组</param>
    /// <param name="offset">从哪里开始</param>
    /// <param name="length">判断的长度，留空则为剩余全长</param>
    /// <returns></returns>
    public static ((TerminalCommand, (int,int)),int) Do(char[] arr, int offset, int? length = null)
    {
        Span<char> slice = arr.AsSpan(offset, length ?? arr.Length - offset);
        return Do(slice);
    }
}