# LLCOM 重构总结

> 时间：2026-08 · 分支：`refactor/main`（22 个提交，基于 master）
> 原则：**不升级 .NET Framework 与依赖版本、不增删功能、每步独立可测可回退**

## 一、为什么重构

原项目（master）存在的问题：
1. **`Tools/Global.cs` 上帝类**（682 行）：路径/初始化/转换/在线脚本/语言全混在一起
2. **`Settings` 上帝类**（626 行）：串口+MQTT+TCP+快捷发送+窗口全塞一个类，65 个 setter 每次全量写盘
3. **MainWindow code-behind 1527 行**：串口/快捷发送/Lua/导航/日志全在事件处理器里
4. **PropertyChanged.Fody 织入黑魔法**：属性通知靠构建期 IL 注入，不可读不可调试
5. 混用 WinForms 控件、`new Thread` 满天飞、重复代码（Lua 编辑器在 MainWindow 与 SettingWindow 各一份）

## 二、做了什么（按步骤）

| 步骤 | 内容 | 成果 |
|---|---|---|
| Step 0 | 基线构建、冒烟测试清单、refactor/main 分支 | 可回退起点 |
| Step 1 | 引入 CommunityToolkit.Mvvm 8.2.2 + ViewModels/Services 骨架 | MVVM 基础 |
| Step 2 | `Global.cs` 拆分为 6 个内聚服务（AppPaths/ProfileInitializer/EncodingHelper/LocalizationService/OnlineScriptService/FileUtils），Global 保留为外观层 | 上帝类消除 |
| Step 3 | `Settings` 继承 ObservableObject、拆 6 个分部文件、**600ms 防抖保存** + 退出 Flush | 上帝类消除 + IO 优化 |
| Step 4 | `Uart` → `ISerialPortService`/`SerialPortService`（接口化、清理调试日志、Task.Run） | 串口底层稳化 |
| Step 5 | 主窗口串口控制区 MVVM（端口/波特率/发送/热插拔/自动重连），code-behind 减约 400 行 | 主窗口瘦身 |
| Step 6 | 快捷发送区 VM（10 页/增删/导入导出/切页），菜单动态化 | 数据逻辑入 VM |
| Step 7 | `LuaEditorViewModel` 通用编辑器，主窗口+设置窗口 3 实例复用，删约 400 行重复 | 重复消除 |
| Step 8 | 收发数据区 VM（分包/流式双模式） | 显示逻辑入 VM |
| Step 9 | 全库 file-scoped namespace（44 文件）+ CSharpier 全库格式化 | 现代语法+统一格式 |
| Step 10b | 移除 PropertyChanged.Fody，剩余类转 ObservableObject/手写 INPC | 织入黑魔法消除 |

## 三、过程中发现并修复的原代码遗留 bug

| # | Bug | 根因 | 修复 |
|---|---|---|---|
| 1 | **快捷发送数据每次启动翻倍** | `Settings.quickSend` 视图属性未 `[JsonIgnore]`，与 `quickSendList` 字段重复序列化，反序列化时 Newtonsoft Auto 模式向同一 List 追加两次 | `quickSend` 加 `[JsonIgnore]` |
| 2 | **窗口位置副屏不恢复** | 恢复条件用主屏宽 `FullPrimaryScreenWidth` 判断 | 改用 `VirtualScreen`（多显示器并集） |
| 3 | **USB 拔插后已拔设备残留列表** | WMI（Win32_PnPEntity）有缓存/延迟 | 用注册表权威 `SerialPort.GetPortNames()` 过滤 |
| 4 | 启动时误自动连接串口（Step5 迁移引入） | `new SerialPort()` 默认 PortName="COM1" + `_forcusClosePort` 初始值错 | 初始 true（与原版一致） |
| 5 | USB 拔插自动重连失效（Step5 迁移引入） | `_lastPortName` 仅手动关闭时更新 | 恢复用 `uart.GetName()` |
| 6 | Lua 编辑器切文件/新建卡死（Step7 引入） | `RefreshList` 中 SelectedFile 赋值触发递归 LoadFile | `_loading` 标志时序修正 |
| 7 | DataShowPage 复选框全部失效（Step8 引入） | `DataShowViewModel.Setting` 为 internal，WPF 绑定要求 public | Settings 类改 public |
| 8 | 7 个工具页绑定不更新（Step10b 引入） | 详见下方"重大踩坑" | 手写 INPC |

## 四、重大踩坑（本次重构最有价值的经验）

### 坑 1：MVVM Toolkit 生成器无法用于 WPF Page（第 8 号 bug 的完整链路）
1. `[ObservableProperty]` 要求继承 `ObservableObject` 或 `[ObservableObject]/[INotifyPropertyChanged]` 特性（MVVMTK0019）
2. 用 `[INotifyPropertyChanged]` 特性 + 手动实现接口 → **冲突**（MVVMTK0001/0002）
3. XAML 生成的 `.g.cs` **硬编码基类 `: Page`** → partial 基类必须一致，无法用自定义基类
4. **结论**：WPF Page 类只能**手写 INotifyPropertyChanged**（事件 + SetProperty 方法），见 `02-architecture.md` 第四节

### 坑 2：AvalonEdit `TextEditor.Text` 不可绑定
不是 DependencyProperty，XAML `{Binding Text=...}` 会直接 XamlParseException 崩溃。方案：VM 提供 `SetTextBridge(get, set)` 回调（见 LuaEditorViewModel/DataShowViewModel）。

### 坑 3：CSharpier 会格式化不该动的文件
`csharpier format .` 会把 `llcom.csproj`、`packages.config`、语言文件、FodyWeavers.xml 等 XML/配置也格式化。**应只对 .cs 和 UI .xaml 执行**，或格式化后还原配置文件。

### 坑 4：脚本批量改代码的行尾/转义问题
用 python 等脚本批量改 C# 文件时：① `open()` 默认会改 CRLF→LF（git diff 全文件爆红），需 `newline=''`；② heredoc 里 `\\n` 会被转成真实换行导致 C# 字符串编译错误。**优先用 edit 工具或 write 工具写脚本文件再执行**。

## 五、成果数字

- **提交**：22 个（每步独立，可回退）
- **代码量**：约 +3500 / -4000 行（净减少）
- **依赖**：+CommunityToolkit.Mvvm 8.2.2（+3 个传递包）、-PropertyChanged.Fody；**其余依赖与 .NET Framework 4.6.2 均未变动**
- **架构**：从"上帝类 + 巨型 code-behind"变为 ViewModels/Services/Models 分层
