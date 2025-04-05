using System;
using System.Collections.Generic;
using LLCOM.Services;

namespace LLCOM.Models;

public class TerminalCache(int MaxCacheLines)
{
    //展示画面变化时的事件
    public EventHandler? TerminalChangedEvent { get; set; }
    
    
    //MaxCacheLines表示终端缓存的行数，超过这个行数后会删除最上面的行
    private readonly int MaxCacheLines = Utils.Setting.TerminalBufferLines;
    
    //可视范围内的宽高
    public int WindowWidth { get; set; }
    public int WindowHeight { get; set; }
    
    //用于存放终端数据的缓存
    private List<List<TerminalBlock>> CacheLines { get; set; } = new();
    
    //当前所在的行数相比较于终端最底部的行数，0表示在最底部，其余数字表示向上挪动的行数
    private int _currentLine = 0;

    private int CurrentLine
    {
        get => _currentLine;
        set
        {
            _currentLine = value;
            TerminalChangedEvent?.Invoke(this, EventArgs.Empty);
        }
    }
}