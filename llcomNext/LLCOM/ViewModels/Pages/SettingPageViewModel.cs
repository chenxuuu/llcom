﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RestSharp;

namespace LLCOM.ViewModels;

public partial class SettingPageViewModel : ViewModelBase
{
    private readonly Func<Type, ViewModelBase> _getService;
    
    //用于设计时预览，正式代码中无效
    public SettingPageViewModel() {}
    
    public SettingPageViewModel(Func<Type, ViewModelBase> getService)
    {
        _getService = getService;


        
        //初始化系统信息
        Task.Run(async () =>
        {
            //用skia接口获取系统字体列表
            var fontMgr = SkiaSharp.SKFontManager.Default;
            var fonts = new List<string>();
            foreach (var f in fontMgr.FontFamilies)
            {
                fonts.Add(f);
            }
            //把列表内容按字母排序
            fonts.Sort();
            //添加到系统字体列表
            foreach (var f in fonts)
            {
                SystemFontList.Add(f);
            }
            //找找看当前设置的是什么字体，对应上
            PacketFontIndex = SystemFontList.IndexOf(Services.Utils.Setting.PacketFont);
            PacketHexFontIndex = SystemFontList.IndexOf(Services.Utils.Setting.PacketHexFont);
            PacketHeaderFontIndex = SystemFontList.IndexOf(Services.Utils.Setting.PacketHeaderFont);
            PacketExtraFontIndex = SystemFontList.IndexOf(Services.Utils.Setting.PacketExtraFont);
            
            //是否已经检查过更新？
            if (Services.Utils.HasUpdate())
            {
                _updateUrl = await Services.Utils.CheckUpdate();
                HasCheckedUpdate = true;
            }
            
            var packagesInfo = new List<string>();
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
        
            foreach (var assembly in assemblies)
            {
                var name = assembly.GetName();
                packagesInfo.Add($"{name.Name}: {name.Version}");
            }
            
            SystemInfo =
                $"LLCOM: {Services.Utils.Version}\n" +
                $"OS: {Environment.OSVersion.VersionString}\n" +
                $".Net CLR: {Environment.Version}\n" +
#if debug
                $"Debug mode: true\n" +
#else
                $"Debug mode: false\n" +
#endif
                $"Location: {Environment.CurrentDirectory}\n" +
                $"AppData: {Services.Utils.AppPath}\n" +
                $"Packages \n{string.Join("\n", packagesInfo)}\n" +
                $"Environment: {Environment.GetEnvironmentVariable("PATH")}";
        });

    }

    #region FontSettings

    [ObservableProperty]
    private ObservableCollection<FontFamily> _SystemFontList = new();
    [ObservableProperty]
    private int _packetFontIndex;
    [ObservableProperty]
    private int _packetHexFontIndex;
    [ObservableProperty]
    private int _packetHeaderFontIndex;
    [ObservableProperty]
    private int _packetExtraFontIndex;

    #endregion
    
    #region About
    [ObservableProperty]
    private string _systemInfo = "Loading...";

    [RelayCommand]
    private async Task CopySystemInfo()
    {
        if(SystemInfo.Length < 20)//还没加载完
            return;
        await Services.Utils.CopyString(SystemInfo);
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasNewVersion), nameof(NoNewVersion))]
    private bool _hasCheckedUpdate = false;
    
    public bool HasNewVersion => HasCheckedUpdate && _updateUrl != null;
    public bool NoNewVersion => HasCheckedUpdate && _updateUrl == null;
    
    private string? _updateUrl = null;
    
    [RelayCommand]
    private async Task CheckUpdate()
    {
        if (HasCheckedUpdate && _updateUrl != null)
        {
            Services.Utils.OpenWebLink(_updateUrl);
            return;
        }

        var url = await Services.Utils.CheckUpdate();

        //更新信息
        if(url != null)
            _updateUrl = url;
        
        HasCheckedUpdate = true;
    }
    #endregion

}