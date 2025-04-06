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
    private readonly List<List<TerminalBlock>> _cacheLines = [];
    
    //当前光标位置
    //X从0开始，最大可到达窗口宽度（再增加就需要换行了）
    //Y从0开始，最大可到达窗口高度-1
    private int _positionX = 0;
    private int _positionY = 0;
    
    //当前的颜色、字体等信息，存到TerminalBlock中
    private TerminalBlock _currentState = new(String.Empty);
    
    //MaxCacheLines表示终端缓存的行数，超过这个行数后会删除最上面的行
    private readonly int _maxCacheLines = Utils.Setting.TerminalBufferLines;
    
    //可视范围内的宽高
    private int _windowWidth;
    private int _windowHeight;
    
    //添加新的一行上去
    private void AddLine()
    {
        //添加行
        _cacheLines.Add([]);
        //如果超过了最大行数，删除最上面的行
        if (_cacheLines.Count > _maxCacheLines)
            _cacheLines.RemoveAt(0);
        if (_currentLine != 0)
        {
            _currentLine--;
            //如果当前行数超过了最大行数，设置为最大行数
            if (_currentLine > _cacheLines.Count - _windowHeight)
                _currentLine = _cacheLines.Count - _windowHeight;
            if (_currentLine < 0) _currentLine = 0;
        }
    }

    /// <summary>
    /// 基于当前光标，往后追加文本
    /// 文本不得包含不可见字符
    /// </summary>
    /// <param name="text">待添加的文本</param>
    public void AddText(char[] texts)//TODO)) 保持private
    {
        //防止没有设置窗口大小的时候就添加数据
        if(_windowWidth == 0 || _windowHeight == 0)
            return;
        
        var chars = texts[..];
        while (chars.Length > 0)
        {
            //当前光标位置后还有多少个字符的空间
            var space = _windowWidth - _positionX;
            //剩余空间不足，添加新行
            if (space <= 0)
            {
                _positionX = 0;
                _positionY++;//超过高度后面再管
                space = _windowWidth;
            }
            //记录一下修改前的X光标位置
            var oldX = _positionX;
            
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
                _positionX += length;
            }
            if(space < 0)
            {
                //如果剩余空间不足，说明最后一格放不下这个宽字符
                //直接把X位置打到头，交够下一轮来处理
                _positionX = _windowWidth;
            }
            //添加文本
            var text = sb.ToString();
            var line = _currentState.MakeNew(text);

            //这一行数据要修改
            List<TerminalBlock> needChangeLine;
            
            //超过了最大高度，说明要新开一行
            if (_positionY >= _windowHeight)
            {
                _positionY = _windowHeight - 1;
                AddLine();
                needChangeLine = _cacheLines[^1];
            }
            //当前缓存的行数还没有达到显示行高，也开新行
            else if (_cacheLines.Count - 1 < _positionY)
            {
                var needLineCount = _positionY - _cacheLines.Count + 1;
                for (int i = 0; i < needLineCount; i++)
                    AddLine();
                needChangeLine = _cacheLines[^1];
            }
            //不是新行，需要更改当前行的数据
            else
            {
                //计算开始行下标的偏移量
                var lineStartOffset = _cacheLines.Count - _windowHeight;
                if(lineStartOffset < 0)
                    lineStartOffset = 0;
                //使用当前行
                needChangeLine = _cacheLines[_positionY + lineStartOffset];
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
                //TODO)) 这里需要处理光标重叠的情况
                // //查找一下看开始的位置是重叠到哪个元素了
                // var hitIndex = 0;
                // var hitLength = 0;
                // foreach (var block in oldLine)
                // {
                //     if (hitLength + block.Length > oldX)
                //         break;
                //     hitLength += block.Length;
                //     hitIndex++;
                // }
                // //查找看看结束的位置是重叠到哪个元素了
                // var endIndex = hitIndex;
                // var endLength = hitLength;
                // for(int i = hitIndex; i < oldLine.Count; i++)
                // {
                //     if (endLength + oldLine[i].Length > _positionX)
                //         break;
                //     endLength += oldLine[i].Length;
                //     endIndex++;
                // }
                    
            }
            
            //优化当前这一行数据块
            TerminalBlock.OptimizeBlocks(needChangeLine);
        }
    }
    
    //TODO)) 仅用于测试
    public void ChangePosition(int x, int y)
    {
        _positionX = x;
        _positionY = y;
    }
    
    /// <summary>
    /// 获取可以显示的行数据
    /// </summary>
    /// <returns>一行行的数据</returns>
    public List<List<TerminalBlock>> GetShowLines()
    {
        List<List<TerminalBlock>> cacheLines = new();
        
        //计算出要显示的行数范围
        int allLines = _cacheLines.Count;
        //起始行和结束行，闭区间，从0开始，代表CacheLines的项目下标
        int startLine = allLines - _windowHeight - _currentLine;
        if (startLine < 0)
            startLine = 0;
        int endLine = startLine + _windowHeight - 1;
        if (endLine >= allLines)
            endLine = allLines - 1;
        
        //添加行
        for (int i = startLine; i <= endLine; i++)
        {
            //添加行
            var line = _cacheLines[i];
            cacheLines.Add(line);
        }
        
        //如果行数不够，补齐空行
        for (int i = cacheLines.Count; i < _windowHeight; i++)
        {
            //添加空行
            cacheLines.Add([]);
        }
        //把当前光标位置背景和前景色反色处理
        var posY = _positionY;
        if (posY < _cacheLines.Count)
        {
            //TODO)) 这里需要处理光标位置
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
    private int _currentLine = 0;

    private double CurrentLine2ScrollValue =>
        (_currentLine == 0 || _cacheLines.Count < _windowHeight)
            ? 100
            : 100.0 - (double)_currentLine / (_cacheLines.Count - _windowHeight) * 100.0;
    //向上移动的行数
    public double CurrentLineMoveUp(int delta)
    {
        var lastCurrentLine = _currentLine;
        _currentLine += delta;
        if(_currentLine > _cacheLines.Count - _windowHeight)
            _currentLine = _cacheLines.Count - _windowHeight;
        if(_currentLine < 0)
            _currentLine = 0;
        
        if(lastCurrentLine != _currentLine)
            TerminalChanged();
        
        return CurrentLine2ScrollValue;
    }
    //滚轮事件
    public void ScrollBarChanged(double value)
    {
        var lastCurrentLine = _currentLine;
        if(Math.Abs(value - 100.0) < 0.001 || _cacheLines.Count < _windowHeight)
            _currentLine = 0;
        else
            _currentLine = (int)(_cacheLines.Count - _windowHeight - value * (_cacheLines.Count - _windowHeight) / 100.0);
        
        if(lastCurrentLine != _currentLine)
            TerminalChanged();
    }
}