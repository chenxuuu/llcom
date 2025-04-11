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
    public EventHandler<List<List<TerminalBlock>>>? TerminalChangedEvent { get; set; }
    private void TerminalChanged()
    {
        if(TerminalChangedEvent is null)
            return;
        //触发更新事件
        TerminalChangedEvent?.Invoke(this, GetShowLines());
    }
    
    //用于存放终端数据的缓存
    private List<List<TerminalBlock>> CacheLines { get; } = [];
    
    //当前光标位置
    //X从0开始，最大可到达窗口宽度（再增加就需要换行了）
    //Y从0开始，最大可到达窗口高度-1
    private int PositionX { get; set; } = 0;
    private int PositionY { get; set; } = 0;
    
    //当前的颜色、字体等信息，存到TerminalBlock中
    private TerminalBlock CurrentState { get; set; } = new(String.Empty);
    
    //MaxCacheLines表示终端缓存的行数，超过这个行数后会删除最上面的行
    private readonly int _maxCacheLines = Utils.Setting.TerminalBufferLines;
    
    //可视范围内的宽高
    private int _windowWidth;
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
    public void AddText(char[] texts)//TODO)) 保持private
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
    public void ChangePosition(int x, int y)
    {
        PositionX = x;
        PositionY = y;
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
        if (PositionY < cacheLines.Count)
        {
            //这里需要处理光标位置
            //复制一个新的行来替换掉现有的行用来展示
            var tempLine = new List<TerminalBlock>();
            foreach (var block in cacheLines[PositionY])
            {
                tempLine.Add((TerminalBlock)block.Clone());
            }
            
            var allLength = tempLine.Sum(l => l.Length);
            //光标没有重叠，说明光标位置在当前行的后面
            if (allLength <= PositionX)
            {
                //加几个空格，直到光标位置
                if(allLength < PositionX)
                    tempLine.Add(new TerminalBlock(new string(' ', allLength - PositionX)));
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
            cacheLines[PositionY] = tempLine;
        }
        
        return cacheLines;
    }
    
    //窗口大小变化
    public void ChangeWindowSize(int width, int height)
    {
        _windowWidth = width;
        _windowHeight = height;
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
}