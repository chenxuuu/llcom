using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CommunityToolkit.Mvvm.ComponentModel;
using LLCOM.Services;
using Wcwidth;

namespace LLCOM.Models;

public class TerminalObject
{
    public TerminalObject()
    {

    }
    
    //展示画面变化时的事件
    public EventHandler? TerminalChangedEvent { get; set; }
    private void TerminalChanged()
    {
        if(TerminalChangedEvent is null)
            return;
        //触发更新事件
        TerminalChangedEvent?.Invoke(this, EventArgs.Empty);
    }
    
    /// 用于存放终端数据的缓存
    private List<List<TerminalBlock>> CacheLines { get; } = [];

    //当前光标位置
    /// X从0开始，最大可到达窗口宽度（再增加就需要换行了）
    private int PositionX { get; set; } = 0;

    /// Y从0开始，最大可到达窗口高度-1
    private int PositionY { get; set; } = 0;
    
    /// 当前的颜色、字体等信息，存到TerminalBlock中
    private TerminalBlock CurrentState { get; set; } = new(String.Empty);
    
    /// MaxCacheLines表示终端缓存的行数，超过这个行数后会删除最上面的行
    private readonly int _maxCacheLines = Utils.Setting.TerminalBufferLines;
    
    /// 可视范围内的宽
    private int _windowWidth;
    /// 可视范围内的高
    private int _windowHeight;
    
    //添加新的一行上去
    private void AddLine()
    {
        //添加行
        CacheLines.Add([]);
        //如果超过了最大行数，删除最上面的行
        if (CacheLines.Count > _maxCacheLines)
            CacheLines.RemoveAt(0);
        if (CurrentLine != 0)
        {
            CurrentLine--;
            //如果当前行数超过了最大行数，设置为最大行数
            if (CurrentLine > CacheLines.Count - _windowHeight)
                CurrentLine = CacheLines.Count - _windowHeight;
            if (CurrentLine < 0) CurrentLine = 0;
        }
    }

    /// <summary>
    /// 基于当前光标，往后追加文本
    /// 文本不得包含不可见字符
    /// </summary>
    /// <param name="texts">待添加的文本</param>
    private void AddText(char[] texts)//TODO)) 保持private
    {
        //防止没有设置窗口大小的时候就添加数据
        if(_windowWidth == 0 || _windowHeight == 0)
            return;
        
        var chars = texts[..];
        while (chars.Length > 0)
        {
            //当前光标位置后还有多少个字符的空间
            var space = _windowWidth - PositionX;
            //剩余空间不足，添加新行
            if (space <= 0)
            {
                PositionX = 0;
                PositionY++;//超过高度后面再管
                space = _windowWidth;
            }
            //记录一下修改前的X光标位置
            var oldX = PositionX;
            
            //放置文本
            var sb = new StringBuilder();
            while (space > 0 && chars.Length > 0)
            {
                //获取实际的字符宽度
                var length = UnicodeCalculator.GetWidth(chars[0]);
                if (length < 0)
                    length = 0;
                //挖去可用空间
                space -= length;
                if(space < 0)//剩余空间不足，别添加了
                    break;
                //添加字符
                sb.Append(chars[0]);
                //去除掉已经添加的字符
                chars = chars[1..];
                //光标位置往后挪动
                PositionX += length;
            }
            if(space < 0)
            {
                //如果剩余空间不足，说明最后一格放不下这个宽字符
                //直接把X位置打到头，交够下一轮来处理
                PositionX = _windowWidth;
            }
            //添加文本
            var text = sb.ToString();
            var line = CurrentState.MakeNew(text);

            //这一行数据要修改
            List<TerminalBlock> needChangeLine;
            
            //超过了最大高度，说明要新开一行
            if (PositionY >= _windowHeight)
            {
                PositionY = _windowHeight - 1;
                AddLine();
                needChangeLine = CacheLines[^1];
            }
            //当前缓存的行数还没有达到显示行高，也开新行
            else if (CacheLines.Count - 1 < PositionY)
            {
                var needLineCount = PositionY - CacheLines.Count + 1;
                for (int i = 0; i < needLineCount; i++)
                    AddLine();
                needChangeLine = CacheLines[^1];
            }
            //不是新行，需要更改当前行的数据
            else
            {
                //计算开始行下标的偏移量
                var lineStartOffset = CacheLines.Count - _windowHeight;
                if(lineStartOffset < 0)
                    lineStartOffset = 0;
                //使用当前行
                needChangeLine = CacheLines[PositionY + lineStartOffset];
            }
            
            var allLength = needChangeLine.Sum(l => l.Length);
            //光标没有重叠，说明可以直接添加到当前行
            if (oldX >= allLength)
            {
                //看有没有缺的空间，有的话用空格补齐
                if (oldX > allLength)
                    needChangeLine.Add(new(new(' ', oldX - allLength)));
                needChangeLine.Add(line);
            }
            else
            {
                //把这一行按字符拆碎
                var tempLine = new List<TerminalBlock>();
                foreach (var block in needChangeLine)
                {
                    //拆碎
                    var tempChars = block.Text.ToCharArray();
                    foreach (var c in tempChars)
                    {
                        var length = UnicodeCalculator.GetWidth(c);
                        //塞到临时行中
                        tempLine.Add(block.MakeNew(c.ToString()));
                        //如果字符宽度超过1，加入空白填位
                        length--;
                        while (length > 0)
                        {
                            tempLine.Add(block.MakeNew(string.Empty));
                            length--;
                        }
                    }
                }
                
                //待添加的字符列表
                //null表示前一个字符占据了这个位置
                List<char?> charsForInsert = [];
                foreach (var c in line.Text.ToCharArray())
                {
                    charsForInsert.Add(c);
                    var length = UnicodeCalculator.GetWidth(c);
                    length--;
                    while (length > 0)
                    {
                        charsForInsert.Add(null);
                        length--;
                    }
                }
                //一个个替换或者添加，从oldX开始
                var currentX = oldX;
                //先判断第一个位置是否是空字符
                if (string.IsNullOrEmpty(tempLine[currentX].Text))
                {
                    //如果是空字符，则表示往前找可以找到一个占位符
                    var charIndex = currentX;
                    while (charIndex > 0)
                    {
                        charIndex--;
                        //如果找到的不是空字符，说明可以替换
                        if (!string.IsNullOrEmpty(tempLine[charIndex].Text))
                            break;
                    }
                    //将找到的字符全部替换成空格
                    for (int i = charIndex; i < currentX; i++)
                    {
                        //替换成空格
                        tempLine[i].Text = " ";
                    }
                }
                //开始替换
                while (charsForInsert.Count > 0)
                {
                    var s = string.Empty;
                    if(charsForInsert[0] != null)
                        s = charsForInsert[0].ToString();
                    charsForInsert.RemoveAt(0);
                    
                    //看看当前位置有没有字符，没有的话就新建一个
                    if (currentX >= tempLine.Count)
                    {
                        //添加一个新的块
                        tempLine.Add(line.MakeNew(s));
                    }
                    else
                    {
                        //有东西的话就替换掉
                        tempLine[currentX] = line.MakeNew(s);
                    }
                    currentX++;
                }
                //检查下currentX后面有没有空字符
                //如果有空字符，需要替换成空格
                while (currentX < tempLine.Count)
                {
                    //替换成空格
                    if (string.IsNullOrEmpty(tempLine[currentX].Text))
                        tempLine[currentX].Text = " ";
                    else
                        break;
                    currentX++;
                }
                
                //处理完了，把needChangeLine替换掉
                needChangeLine.Clear();
                //添加数据
                foreach (var block in tempLine)
                    needChangeLine.Add(block);
            }
            
            //优化当前这一行数据块
            TerminalBlock.OptimizeBlocks(needChangeLine);
        }
    }
    
    //TODO)) 仅用于测试
    private void ChangePosition(int x, int y)
    {
        PositionX = x;
        PositionY = y;
    }

    //TODO)) 仅用于测试
    private void ChangeStyle(
        int? foreground = null, 
        int? background = null, 
        bool? isBold = null, 
        bool? isItalic = null, 
        bool? isUnderLine = null)
    {
        if (foreground != null)
            CurrentState.Foreground = foreground.Value;
        if (background != null)
            CurrentState.Background = background.Value;
        if (isBold != null)
            CurrentState.IsBold = isBold.Value;
        if (isItalic != null)
            CurrentState.IsItalic = isItalic.Value;
        if (isUnderLine != null)
            CurrentState.IsUnderLine = isUnderLine.Value;
    }
    
    /// <summary>
    /// 获取可以显示的行数据
    /// </summary>
    /// <returns>一行行的数据</returns>
    public List<List<TerminalBlock>> GetShowLines()
    {
        List<List<TerminalBlock>> cacheLines = new();
        
        //计算出要显示的行数范围
        int allLines = CacheLines.Count;
        //起始行和结束行，闭区间，从0开始，代表CacheLines的项目下标
        int startLine = allLines - _windowHeight - CurrentLine;
        if (startLine < 0)
            startLine = 0;
        int endLine = startLine + _windowHeight - 1;
        if (endLine >= allLines)
            endLine = allLines - 1;
        
        //添加行
        for (int i = startLine; i <= endLine; i++)
        {
            //添加行
            var line = CacheLines[i];
            cacheLines.Add(line);
        }
        
        //如果行数不够，补齐空行
        for (int i = cacheLines.Count; i < _windowHeight; i++)
        {
            //添加空行
            cacheLines.Add([]);
        }
        
        //把当前光标位置背景和前景色反色处理
        
        //实际光标在哪一行，会跟随currentLine变化
        var lineIndex = PositionY + CurrentLine;
        
        if (PositionY < cacheLines.Count && lineIndex < cacheLines.Count)
        {
            //这里需要处理光标位置
            //复制一个新的行来替换掉现有的行用来展示
            var tempLine = new List<TerminalBlock>();
            foreach (var block in cacheLines[lineIndex])
            {
                tempLine.Add((TerminalBlock)block.Clone());
            }
            
            var allLength = tempLine.Sum(l => l.Length);
            //光标没有重叠，说明光标位置在当前行的后面
            if (allLength <= PositionX)
            {
                //加几个空格，直到光标位置
                if(allLength < PositionX)
                    tempLine.Add(new TerminalBlock(new string(' ', PositionX - allLength)));
                tempLine.Add(new TerminalBlock(new string(' ', 1),-1,-1));
            }
            else
            {
                var charLine = new List<TerminalBlock>();
                var count = 0;
                //把这一行按字符拆碎
                foreach (var block in tempLine)
                {
                    //拆碎
                    var tempChars = block.Text.ToCharArray();
                    foreach (var c in tempChars)
                    {
                        var start = count;
                        var length = UnicodeCalculator.GetWidth(c);
                        var end = start + length - 1;
                        //如果posx在当前字符范围内，说明光标在这个字符上
                        if (PositionX >= start && PositionX <= end)
                            charLine.Add(new TerminalBlock(c.ToString(), -1, -1));
                        else
                            charLine.Add(block.MakeNew(c.ToString()));
                        count += length;
                        //如果字符宽度超过1，加入空白填位
                        length--;
                        while (length > 0)
                        {
                            charLine.Add(block.MakeNew(string.Empty));
                            length--;
                        }
                    }
                }
                tempLine = charLine;//替换掉当前行为拆字后的行
            }
            //优化当前这一行数据块
            TerminalBlock.OptimizeBlocks(tempLine);
            //替换掉当前行
            cacheLines[lineIndex] = tempLine;
        }
        
        return cacheLines;
    }
    
    //窗口大小变化
    public void ChangeWindowSize(int width, int height)
    {
        _windowWidth = width;
        var oldHeight = _windowHeight;
        _windowHeight = height;
        
        //高度变化后，光标位置需要重新计算
        if (oldHeight != _windowHeight)
        {
            PositionY += _windowHeight - oldHeight;
            //如果光标位置超过了最大高度，设置为最大高度
            if (PositionY >= _windowHeight)
                PositionY = _windowHeight - 1;
            if (PositionY < 0)
                PositionY = 0;
        }
        TerminalChanged();
    }
    
    //当前所在的行数相比较于终端最底部的行数，0表示在最底部，其余数字表示向上挪动的行数
    private int CurrentLine { get; set; } = 0;

    private double CurrentLine2ScrollValue =>
        (CurrentLine == 0 || CacheLines.Count < _windowHeight)
            ? 100
            : 100.0 - (double)CurrentLine / (CacheLines.Count - _windowHeight) * 100.0;
    //向上移动的行数
    public double CurrentLineMoveUp(int delta)
    {
        var lastCurrentLine = CurrentLine;
        CurrentLine += delta;
        if(CurrentLine > CacheLines.Count - _windowHeight)
            CurrentLine = CacheLines.Count - _windowHeight;
        if(CurrentLine < 0)
            CurrentLine = 0;
        
        if(lastCurrentLine != CurrentLine)
            TerminalChanged();
        
        return CurrentLine2ScrollValue;
    }
    //滚轮事件
    public void ScrollBarChanged(double value)
    {
        var lastCurrentLine = CurrentLine;
        if(Math.Abs(value - 100.0) < 0.001 || CacheLines.Count < _windowHeight)
            CurrentLine = 0;
        else
            CurrentLine = (int)(CacheLines.Count - _windowHeight - value * (CacheLines.Count - _windowHeight) / 100.0);
        
        if(lastCurrentLine != CurrentLine)
            TerminalChanged();
    }
    
    /// <summary>
    /// 保存的光标位置
    /// </summary>
    private int _saveCursorX = 0;
    /// <summary>
    /// 保存的光标位置
    /// </summary>
    private int _saveCursorY = 0;
    
    public void AddChars(ReadOnlySpan<char> chars)
    {
        //如果没有设置窗口大小，直接返回
        if(_windowWidth == 0 || _windowHeight == 0)
            return;

        var ptr = 0;
        var lastPos = 0;
        while (ptr < chars.Length)
        {
            //分析当前文本块有没有匹配到命令
            var ((cmd, (p1,p2)) ,len) = TerminalCommandCheck.Do(chars[ptr..]);
            //没匹配到就继续下一个
            if (cmd == TerminalCommand.None)
            {
                ptr++;
                continue;
            }
            else//匹配到了，动作之前先打印没输出的文本
            {
                var slice = chars[lastPos..ptr];
                if (slice.Length > 0)
                {
                    //添加文本
                    AddText(slice.ToArray());
                }
                lastPos = ptr + len;
                ptr += len;
            }

            //当前的样式备份一下
            var lastState = CurrentState;
            //匹配对应命令
            switch (cmd)
            {
                case TerminalCommand.Unknown://未知命令，直接跳过
                    break;
                case TerminalCommand.Bs:
                    //退格，光标往前挪一个字符，并且删除当前字符
                    //先把光标往前挪一个字符
                    if (PositionX > 0)
                        PositionX--;
                    else if (PositionY > 0)
                    {
                        //如果光标在行首，往上一行挪
                        PositionY--;
                        PositionX = _windowWidth - 1; //挪到行尾
                    }
                    //样式恢复默认
                    CurrentState = new TerminalBlock("");
                    //加一个空格来覆盖掉当前字符
                    AddText(new[] { ' ' });
                    //样式恢复到上一个状态
                    CurrentState = lastState;
                    //先把光标往前挪一个字符
                    if (PositionX > 0)
                        PositionX--;
                    else if (PositionY > 0)
                    {
                        //如果光标在行首，往上一行挪
                        PositionY--;
                        PositionX = _windowWidth - 1; //挪到行尾
                    }
                    break;
                case TerminalCommand.Ht:
                    //水平制表符，光标往后挪到下一个制表符位置，制表符位置是8的倍数
                    //计算一下需要几个空格
                    var nextTabStop = (PositionX / 8 + 1) * 8;
                    //如果下一个制表符位置超过了窗口宽度，就直接挪到窗口宽度
                    if (nextTabStop >= _windowWidth)
                        nextTabStop = _windowWidth - 1;
                    //添加空格
                    var spaces = nextTabStop - PositionX;
                    if (spaces > 0)
                    {
                        //添加空格
                        AddText(new string(' ', spaces).ToCharArray());
                        //光标位置往后挪
                        PositionX += spaces;
                    }
                    break;
                case TerminalCommand.Lf:
                    //换行，光标往下一行挪
                    PositionY++;
                    if(PositionY >= _windowHeight)
                    {
                        //如果超过了最大高度，添加新行
                        AddLine();
                        PositionY = _windowHeight - 1; //光标位置挪到最后一行
                    }
                    //如果没开启严格换行模式，顺便就处理一下光标位置
                    if (!Utils.Setting.IsTerminalStrictLineBreak)
                    {
                        //光标位置挪到行首
                        PositionX = 0;
                    }
                    break;
                case TerminalCommand.Cr:
                    //回车，光标位置挪到行首
                    PositionX = 0;
                    break;
                case TerminalCommand.Hide:
                    //隐藏光标，TODO
                    break;
                case TerminalCommand.Show:
                    //显示光标，TODO
                    break;
                case TerminalCommand.ClearLineEnd:
                    //清除光标到行尾
                    //样式恢复默认
                    CurrentState = new TerminalBlock("");
                    //添加空格到行尾
                    var spacesToEnd = _windowWidth - PositionX;
                    if (spacesToEnd > 0)
                    {
                        AddText(new string(' ', spacesToEnd).ToCharArray());
                    }
                    //样式恢复到上一个状态
                    CurrentState = lastState;
                    break;
                case TerminalCommand.ClearLineStart:
                    //清除光标到行首
                    //样式恢复默认
                    CurrentState = new TerminalBlock("");
                    //存一下上次的光标位置
                    var oldXLineStartCmd = PositionX;
                    //光标位置挪到行首
                    PositionX = 0;
                    //添加空格到行首
                    if (oldXLineStartCmd > 0)
                    {
                        AddText(new string(' ', oldXLineStartCmd).ToCharArray());
                    }
                    //样式恢复到上一个状态
                    CurrentState = lastState;
                    break;
                case TerminalCommand.ClearLine:
                    //清除当前行
                    //样式恢复默认
                    CurrentState = new TerminalBlock("");
                    //移动光标到行首
                    PositionX = 0;
                    //添加一行空格
                    if (_windowWidth > 0)
                    {
                        AddText(new string(' ', _windowWidth).ToCharArray());
                    }
                    //样式恢复到上一个状态
                    CurrentState = lastState;
                    break;
                case TerminalCommand.ClearScreenEnd:
                    //清除光标到行尾
                    //样式恢复默认
                    CurrentState = new TerminalBlock("");
                    //添加空格到行尾
                    var spacesToScreenEndLine = _windowWidth - PositionX;
                    if (spacesToScreenEndLine > 0)
                    {
                        AddText(new string(' ', spacesToScreenEndLine).ToCharArray());
                    }
                    //把后面的行全部清空
                    for (int i = PositionY + 1; i < _windowHeight; i++)
                    {
                        PositionX = 0; //光标位置挪到行首
                        PositionY = i; //光标位置挪到这一行
                        //添加一行空格
                        AddText(new string(' ', _windowWidth).ToCharArray());
                    }
                    //样式恢复到上一个状态
                    CurrentState = lastState;
                    break;
                case TerminalCommand.ClearScreenStart:
                    //清除光标到行首
                    //样式恢复默认
                    CurrentState = new TerminalBlock("");
                    //存一下上次的光标位置
                    var oldXLineScreenStartCmd = PositionX;
                    //光标位置挪到行首
                    PositionX = 0;
                    //添加空格到行首
                    if (oldXLineScreenStartCmd > 0)
                    {
                        AddText(new string(' ', oldXLineScreenStartCmd).ToCharArray());
                    }
                    //把前面的行全部清空
                    for (int i = 0; i < PositionY; i++)
                    {
                        PositionX = 0; //光标位置挪到行首
                        PositionY = i; //光标位置挪到这一行
                        //添加一行空格
                        AddText(new string(' ', _windowWidth).ToCharArray());
                    }
                    //样式恢复到上一个状态
                    CurrentState = lastState;
                    break;
                case TerminalCommand.ClearScreen:
                    //清除屏幕
                    //样式恢复默认
                    CurrentState = new TerminalBlock("");
                    //直接加数行，盖住之前的内容
                    for (int i = 0; i < _windowHeight; i++)
                        AddLine();
                    //样式恢复到上一个状态
                    CurrentState = lastState;
                    break;
                case TerminalCommand.MoveCursorUp:
                    //光标上移
                    if (p1 > 0)
                    {
                        PositionY -= p1;
                        if (PositionY < 0)
                            PositionY = 0; //不能小于0
                    }
                    break;
                case TerminalCommand.MoveCursorDown:
                    //光标下移
                    if (p1 > 0)
                    {
                        PositionY += p1;
                        if (PositionY >= _windowHeight) //如果超过了窗口高度，挪到最后一行
                            PositionY = _windowHeight - 1;
                    }
                    break;
                case TerminalCommand.MoveCursorRight:
                    //光标右移
                    if (p1 > 0)
                    {
                        PositionX += p1;
                        if (PositionX >= _windowWidth) //如果超过了窗口宽度，挪到行尾
                            PositionX = _windowWidth - 1;
                    }
                    break;
                case TerminalCommand.MoveCursorLeft:
                    //光标左移
                    if (p1 > 0)
                    {
                        PositionX -= p1;
                        if (PositionX < 0) //不能小于0
                            PositionX = 0;
                    }
                    break;
                case TerminalCommand.ResetCursor:
                    //光标移动到左上角
                    PositionX = 0;
                    PositionY = 0;
                    break;
                case TerminalCommand.MoveCursorTo:
                    //光标移动到指定位置
                    //p1表示列数，p2表示行数
                    PositionY = p1 - 1;
                    PositionX = p2 - 1;
                    if(PositionX < 0)
                        PositionX = 0; //不能小于0
                    if(PositionX >= _windowWidth)
                        PositionX = _windowWidth - 1; //不能超过窗口宽度
                    if(PositionY < 0)
                        PositionY = 0; //不能小于0
                    if(PositionY >= _windowHeight)
                        PositionY = _windowHeight - 1; //不能超过窗口高度
                    break;
                case TerminalCommand.SaveCursor:
                    //保存光标位置
                    _saveCursorX = PositionX;
                    _saveCursorY = PositionY;
                    break;
                case TerminalCommand.RestoreCursor:
                    //恢复光标位置
                    PositionX = _saveCursorX;
                    PositionY = _saveCursorY;
                    break;
                case TerminalCommand.ResetStyle:
                    //重置样式
                    CurrentState = new TerminalBlock("");
                    break;
                case TerminalCommand.Bold:
                    //加粗
                    CurrentState.IsBold = true;
                    break;
                case TerminalCommand.Underline:
                    //下划线
                    CurrentState.IsUnderLine = true;
                    break;
                case TerminalCommand.Reverse:
                    //反转颜色
                    CurrentState.Background = -1;
                    CurrentState.Foreground = -1;
                    break;
                case TerminalCommand.ForegroundColor:
                    //前景色
                    //p1表示颜色代码
                    if (p1 == 39) //重置前景色
                        CurrentState.Foreground = -1;
                    else//设置前景色
                        CurrentState.Foreground = p1;
                    break;
                case TerminalCommand.BackgroundColor:
                    //背景色
                    //p1表示颜色代码
                    if (p1 == 49) //重置背景色
                        CurrentState.Background = -1;
                    else//设置背景色
                        CurrentState.Background = p1;
                    break;
                case TerminalCommand.MultipleStyle:
                    //多重样式
                    switch (p1)
                    {
                        case 0://重置样式
                            CurrentState = new TerminalBlock("");
                            break;
                        case 1://加粗
                            CurrentState.IsBold = true;
                            break;
                        case 4://下划线
                            CurrentState.IsUnderLine = true;
                            break;
                        case 7://反转颜色
                            CurrentState.Background = -1;
                            CurrentState.Foreground = -1;
                            break;
                    }
                    if(p2 is >= 30 and <= 37) CurrentState.Foreground = p2; //前景色
                    if(p2 is >= 90 and <= 97) CurrentState.Foreground = p2; //前景色
                    if(p2 is >= 40 and <= 47 )CurrentState.Background = p2; //背景色
                    if(p2 is >= 100 and <= 107)CurrentState.Background = p2; //背景色
                    if(p2 == 1) CurrentState.IsBold = true; //加粗
                    if(p2 == 4) CurrentState.IsUnderLine = true; //下划
                    if (p2 == 7)    //反转颜色
                    {
                        CurrentState.Background = -1;
                        CurrentState.Foreground = -1;
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
        
        //如果最后还有未处理的文本
        if (lastPos < chars.Length)
        {
            //添加文本
            AddText(chars[lastPos..].ToArray());
        }
        
        //触发更新事件
        TerminalChanged();
    }
}