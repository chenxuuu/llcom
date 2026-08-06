# LLCOM 架构文档

> 本文档在重构过程中逐步建立，用于说明重构后的项目结构与关键设计决策，方便后续维护。
> 重构分支：`refactor/main`（基于 master，逐步骤提交，每步可回退）。

## 一、项目概览

LLCOM 是一个可运行 Lua 脚本的高自由度串口调试工具（.NET Framework 4.6.2 / WPF），同时集成了 TCP/UDP/MQTT/WinUSB/串口监听/绘图/在线脚本等功能。

## 二、目录结构（重构后）

```
llcom/
├─ App.xaml(.cs)             # 启动 + 崩溃上报
├─ ViewModels/               # MVVM ViewModel（CommunityToolkit.Mvvm）
│   ├─ MainViewModel.cs      # 主窗口串口控制区（端口/波特率/发送/热插拔）
│   ├─ QuickSendViewModel.cs # 快捷发送 10 页列表管理
│   ├─ LuaEditorViewModel.cs # Lua 编辑器（主窗口 + 设置窗口 3 个实例共用）
│   ├─ DataShowViewModel.cs  # 收发数据区（分包/流式两种显示模式）
│   └─ ... 
├─ Services/                 # 可复用服务层（原 Tools/Global.cs 拆分而来）
│   ├─ AppPaths.cs           # 路径/文件名/IsMSIX
│   ├─ ProfileInitializer.cs # 配置加载/初始化/文件结构生成
│   ├─ EncodingHelper.cs     # Hex/String/Byte 互转
│   ├─ LocalizationService.cs# 语言切换
│   ├─ OnlineScriptService.cs# GitHub 在线脚本
│   ├─ FileUtils.cs          # 资源释放/SSCOM 导入
│   ├─ ISerialPortService.cs # 串口服务接口
│   └─ SerialPortService.cs  # 串口实现（原 Model/Uart.cs）
├─ Model/                    # 数据模型
│   ├─ Settings.cs (+5 分部) # 全局设置（ObservableObject + 防抖保存）
│   ├─ ToSendData.cs         # 快捷发送条目
│   ├─ DataShowItem.cs       # 数据区显示条目
│   └─ ...
├─ Pages/                    # 工具页（代码后置保留 UI 交互，数据逻辑已 VM 化）
├─ View/                     # 主窗口/设置窗口
├─ LuaEnv/                   # Lua 引擎（XLua + Luat 协程框架）
└─ Tools/
    └─ Global.cs             # 全局状态外观层（组合根：setting/uart/事件）
```

## 三、模块职责与数据流

### 1. 串口数据流
```
SerialPortService（后台接收线程，信号量分包）
  → UartDataRecived 事件
  → ProfileInitializer 挂接：写日志文件
  → Logger.ShowData → DataShowTask 事件
  → DataShowViewModel（UI 线程分发）
    → 分包模式：ObservableCollection<DataShowItem>（列表绑定）
    → 流式模式：StringBuilder + SetTextBridge 增量追加
  → 同时 LuaApis.SendChannelsReceived("uart", data) 推送给 Lua
```

### 2. Lua 双向通道
- Lua `apiSend("uart", data)` → `LuaApis.Send` → `SendChannels["uart"]` → `SerialPortService.SendData`
- 串口收到数据 → `SendChannelsReceived` → Lua 侧 `apiSetCb("uart", cb)` 回调
- 支持通道：`uart` / `mqtt` / `socket-client` / `tcp-server` / `netlab` / `winusb`

### 3. 三个 Lua 虚拟机（互不干扰）
| VM | 用途 | 生命周期 |
|---|---|---|
| `LuaRunEnv` | 用户脚本（user_script_run） | 每次运行新建，可停止 |
| `LuaLoader` | 发送/接收转换脚本 | 常驻，缓存编译结果，脚本变更后 ClearRun 重载 |
| `LuaEnv`（实例化） | 热更脚本等一次性任务 | 即用即弃 |

### 4. 设置持久化（防抖保存）
- `Settings` 继承 ObservableObject，属性 setter 调 `Save()`
- `Save()` 为 600ms 防抖合并写盘；窗口关闭时 `Global.isMainWindowsClosed=true` 调 `Flush()` 强制落盘
- `SentCount/ReceivedCount/DisableLog` 仅 UI 通知，不落盘

## 四、关键设计决策（维护者必读）

### 1. 为什么 7 个工具 Page 手写 INotifyPropertyChanged
MVVM Toolkit 8.2.2 的 `[ObservableProperty]` 生成器要求类继承 `ObservableObject` 或使用
`[ObservableObject]/[INotifyPropertyChanged]` 特性，而这两个特性与"手动实现接口"冲突
（MVVMTK0001/0002）。且 **XAML 生成的 .g.cs 硬编码基类 `: Page`**，partial 基类必须一致，
无法用自定义基类。因此这些 Page 采用手写模式：
```csharp
public partial class XxxPage : Page, INotifyPropertyChanged
{
    public event PropertyChangedEventHandler PropertyChanged;
    protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string name = null) {...}
}
```
⚠️ 添加新属性时务必用 `SetProperty` 并保持属性名 PascalCase 与 XAML 绑定一致。

### 2. AvalonEdit TextEditor 不可绑定
`TextEditor.Text` 不是 DependencyProperty，不能 XAML 绑定。VM 通过 `SetTextBridge(get, set)`
回调访问文本（VM 不持 UI 引用）。参考 `LuaEditorViewModel.SetTextBridge` / `DataShowViewModel.SetTextBridge`。

### 3. quickSend 的 [JsonIgnore]
`Settings.quickSend` 是 `quickSendList` 的视图属性（getter 返回当前页引用），必须 `[JsonIgnore]`——
否则序列化时与 `quickSendList` 字段重复写入 JSON，反序列化时 Newtonsoft 的
`ObjectCreationHandling.Auto` 会向同一 List 追加两次，**导致数据翻倍**（曾发生线上 bug）。

### 4. 串口 SafeHandle 崩溃规避
`SerialPortService.refreshSerialDevice()`：微软 SerialPort 释放时有 SafeHandle 崩溃问题
（System.ObjectDisposedException），旧对象放入 `useless` 列表 + 后台线程 Dispose。
**不要**改成同步 Dispose。

### 5. 接收分包逻辑
`SerialPortService.ReadData()`：收到事件后等待 `setting.timeout` 毫秒让数据包凑齐，
避免中文等多字节字符被分割。`timeout >= 0` 为分包模式，`< 0` 为流式模式。

### 6. USB 热插拔
- `MainWindow.WndProc` 拦截 `WM_DEVICECHANGE(0x219)`（仅串口未打开时），延迟 1 秒后调 `_vm.OnUsbDeviceChanged()`
- 端口列表以 `SerialPort.GetPortNames()`（注册表权威）过滤 WMI 结果，避免已拔设备残留
- 自动重连：`_forcusClosePort` 初始 true（禁止启动误连），用户手动打开串口后置 false

## 五、构建与测试

```bat
"C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\amd64\MSBuild.exe" llcom\llcom.csproj -p:Configuration=Debug -p:Platform=x64
```

- 代码格式化：`dotnet tool run csharpier`（CSharpier，跳过生成文件与配置文件）
- 冒烟测试清单：`docs/refactor-test-checklist.md`
- CI：GitHub Actions（windows-latest + msbuild + nuget restore）

## 六、依赖清单（重构后）

- 新增：CommunityToolkit.Mvvm 8.2.2（+ Microsoft.Bcl.AsyncInterfaces / System.ComponentModel.Annotations / System.Threading.Tasks.Extensions）
- 移除：PropertyChanged.Fody（全部类已转 ObservableObject / 手写 INPC）
- 保留：Costura.Fody（单文件绿色版必需，FodyWeavers.xml 仅剩 Costura 配置）
- 其余依赖版本均未变动
