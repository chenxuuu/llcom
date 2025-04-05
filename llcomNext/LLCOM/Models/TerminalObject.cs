using System;
using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;
using LLCOM.Services;

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
        
        return cacheLines;
    }
    
    //MaxCacheLines表示终端缓存的行数，超过这个行数后会删除最上面的行
    private readonly int _maxCacheLines = Utils.Setting.TerminalBufferLines;
    
    //可视范围内的宽高
    private int _windowWidth;
    private int _windowHeight;
    //窗口大小变化
    public void ChangeWindowSize(int width, int height)
    {
        _windowWidth = width;
        _windowHeight = height;
        TerminalChanged();
    }
    
    //用于存放终端数据的缓存
    private readonly List<List<TerminalBlock>> _cacheLines = new();
    
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
    
    
    //TODO)) 仅供测试使用
    public void AddLine(List<TerminalBlock> line)
    {
        //添加行
        _cacheLines.Add(line);
        //如果超过了最大行数，删除最上面的行
        if (_cacheLines.Count > _maxCacheLines)
            _cacheLines.RemoveAt(0);
        if (_currentLine != 0)
        {
            _currentLine--;
            //如果当前行数超过了最大行数，设置为最大行数
            if (_currentLine > _cacheLines.Count - _windowHeight)
                _currentLine = _cacheLines.Count - _windowHeight;
        }
        
        //更新数据
        TerminalChanged();
    }
}