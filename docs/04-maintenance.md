# LLCOM 维护指南

日常开发、构建、格式化的操作手册。

## 一、环境与构建

### 本机构建工具
| 工具 | 路径/版本 | 用途 |
|---|---|---|
| Visual Studio 2026（VS 18 Community） | `C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\amd64\MSBuild.exe` | **唯一可用构建工具**（支持旧式 csproj + 最新 C# 语法） |
| dotnet SDK 10 | `C:\Program Files\dotnet` | CSharpier 等 dotnet 工具 |
| CSharpier 1.3.0 | `~\.dotnet\tools\csharpier` | 代码格式化 |

> ⚠️ **不要**用 VS2019 msbuild（Roslyn 3.11，无法编译 C# 10 语法）；**不要**用 `dotnet msbuild`（.NET Core msbuild 不支持旧式 WPF 工程的 XAML 临时工程流程，实测会报大量 XAML 相关错误）。

### 构建命令（x64 Debug）
```bat
"C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\amd64\MSBuild.exe" llcom\llcom.csproj -p:Configuration=Debug -p:Platform=x64
```
- 平台：x64 / x86 均支持（`-p:Platform=x86`）
- 配置：Debug / Release
- 输出：`llcom\bin\x64\Debug\llcom.exe`（单文件，托管依赖被 Costura 嵌入）

### 代码格式化
```bat
csharpier format llcom\ --skip-write 2>nul  # 或直接
csharpier format <文件或目录>
```
⚠️ 只对 `.cs` 和 UI `.xaml` 执行；**不要**对 `llcom.csproj` / `packages.config` / 语言文件 / FodyWeavers.xml 执行（会被重排）。

### CI（GitHub Actions）
`.github/workflows/build.yml`：windows-latest + `nuget restore` + `msbuild`。改动构建系统前需同步检查 CI。

## 二、目录速览（改代码前必读）

```
llcom/
├─ ViewModels/     # 各功能 VM（串口/快捷发送/Lua编辑器/数据区）
├─ Services/       # 服务层（路径/配置/编码/串口/在线脚本/本地化/文件）
├─ Model/          # 数据模型（Settings 6个分部文件 + 各条目模型）
├─ Pages/          # 工具页（UI 交互 + 手写 INPC）
├─ View/           # 主窗口/设置窗口
├─ LuaEnv/         # Lua 引擎（XLua + Luat 协程框架 + 3 类 VM）
└─ Tools/Global.cs # 全局状态外观层（setting/uart/事件）
```

详细架构见 [02-architecture.md](02-architecture.md)。

## 三、依赖清单

### 重构后新增
- `CommunityToolkit.Mvvm 8.2.2`（+ `Microsoft.Bcl.AsyncInterfaces 7.0.0`、`System.ComponentModel.Annotations 5.0.0`、`System.Threading.Tasks.Extensions 4.5.4`）

### 重构后移除
- `PropertyChanged.Fody 3.4.0`（全部类已转 ObservableObject 或手写 INPC）

### 保留（勿动版本）
- `Costura.Fody`（单文件绿色版必需，FodyWeavers.xml 仅剩 Costura 配置）
- `XLua.Mini`（本地 `Lib\XLua.Mini.dll`，非 NuGet）
- 其余全部 NuGet 依赖

> packages.config 模式（非 PackageReference），装包需手动下载 nupkg 到 `packages/` 并更新 packages.config + csproj。

## 四、踩坑记录（维护时务必注意）

### 1. WPF Page 属性通知必须手写
XAML `.g.cs` 硬编码基类 `: Page`，且 MVVM Toolkit 生成器与接口/特性互斥 → **Page 类一律手写**：
```csharp
public partial class XxxPage : Page, INotifyPropertyChanged
{
    public event PropertyChangedEventHandler PropertyChanged;
    protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string name = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        return true;
    }
    private bool _isConnected;
    public bool IsConnected { get => _isConnected; set => SetProperty(ref _isConnected, value); }
}
```
新属性：`[ObservableProperty]` 生成器**不可用**于 Page，用上述模板。

### 2. AvalonEdit 文本桥接
`TextEditor.Text` 不是 DependencyProperty，不能绑定。VM 用 `SetTextBridge(() => editor.Text, t => editor.Text = t)` 接入。

### 3. Settings.quickSend 有 [JsonIgnore]
它是 quickSendList 的视图属性，去掉 [JsonIgnore] 会导致反序列化数据翻倍（历史 bug，勿回退）。

### 4. 串口 SafeHandle 崩溃规避
`SerialPortService.refreshSerialDevice()`：旧 SerialPort 对象入 `useless` 列表 + 后台线程 Dispose。**不要**改为同步 Dispose。

### 5. 接收分包逻辑
`ReadData()`：收到事件后等 `setting.timeout` ms 凑包（防中文分割）。`timeout>=0` 分包模式、`<0` 流式模式（DataShowPage 显示方式随之切换）。

### 6. USB 热插拔
- 端口列表 = WMI 结果 + 注册表权威过滤（`GetPortNames()`）
- 自动重连条件 `!_forcusClosePort && autoReconnect`，`_forcusClosePort` 初始 true（防启动误连）

### 7. 设置防抖保存
属性 setter → `Save()`（600ms 合并写盘），窗口关闭时 `Global.isMainWindowsClosed=true` 触发 `Flush()` 强制落盘。加新设置属性时保持此模式。

## 五、开发新功能流程（推荐）

1. **数据/服务** → 放 `Services/` 或 `Model/`（纯逻辑，带中文注释）
2. **页面逻辑** → 新建 `ViewModels/XxxViewModel.cs`（继承 ObservableObject 或手写 INPC）
3. **视图** → XAML 绑定 VM；控件不可绑定的（AvalonEdit）用 SetTextBridge 桥接
4. **注册** → 新 .cs 文件必须手动加入 `llcom.csproj` 的 `<Compile Include>`（旧式工程无自动 globbing）
5. **格式化** → `csharpier format <新文件>`
6. **测试** → 按 [03-test-checklist.md](03-test-checklist.md) 冒烟

## 六、常见操作

| 操作 | 方法 |
|---|---|
| 加 NuGet 包 | 下载 nupkg → 解压到 `packages/` → 更新 packages.config + csproj（Reference/Analyzer/Import） |
| 加 Lua API | `LuaEnv/LuaApis.cs` 静态方法 → `LuaEnv/LuaLoader.cs` 里 `lua.DoString("apiXxx = CS...")` 注册 → 更新 `LuaApi.md` |
| 改语言文案 | `llcom/languages/zh-CN.xaml` + `en-US.xaml`（键一致） |
| 看运行时日志 | `llcom/bin/x64/Debug/logs/log.txt`（串口）、`user_script_run/logs/log.txt`（Lua） |
