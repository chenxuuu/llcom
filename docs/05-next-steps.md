# 下一步行动指南

重构已完成（`refactor/main` 分支，22 个提交）。本文档按优先级排列后续可做的事，每项附评估与操作步骤。

## 一、合并到 master（建议先做）

```bat
git checkout master
git merge refactor/main
```
合并前建议：
1. 在 `refactor/main` 上按 [03-test-checklist.md](03-test-checklist.md) 完整跑一遍（重点：串口收发、快捷发送 10 页、Lua 运行、各工具页、设置持久化）
2. 合并后构建 x64 Debug/Release + x86，并跑一次启动冒烟
3. 推送后 CI 会自动构建验证

> 若想保留"每步可回退"，也可以不合并，直接基于 `refactor/main` 继续开发。

## 二、可选任务评估

### 2.1 SDK-style csproj 迁移（原 Step 10c）— 风险高，谨慎评估
**收益**：csproj 从 ~530 行降到 ~50 行、PackageReference、隐式文件 globbing、dotnet CLI 支持
**风险点**（迁移前必须逐项确认）：
1. **CI 破坏**：`nuget restore`（packages.config）不支持 PackageReference → 需改 `dotnet restore` + workflow
2. **Costura 嵌入**：`costura32/64` 的 xlua.dll/serial_monitor.dll 嵌入需重验证（单文件绿色版可能失效）
3. **Walterlv.Environment.Source 包**：T4 生成源在 SDK-style 下行为需验证
4. **双平台**：x86/x64 baml 生成、`<PlatformTarget>` 配置
5. **绑定重定向**：App.config 的手写 redirect 与 SDK 自动生成可能冲突

**如决定做**：`.pi/skills/msbuild-modernization` 已安装，按其中 7 步清单执行（TFM 映射 net462、删除显式 Include、packages.config→PackageReference、移除样板、WPF 用 `UseWPF=true`）。**建议先在副本分支试迁移，验证 CI + 单文件产出后再合并。**

### 2.2 单元测试 — 中风险，推荐
项目目前**零单元测试**，重构后 VM/Service 已可测（构造函数注入设计）。可测对象：
- `EncodingHelper`（Hex/字符串互转，纯函数）
- `Settings` 持久化（防抖/Flush/JSON 兼容）
- `LuaEditorViewModel`（文件管理逻辑）
- `QuickSendViewModel`（列表/序号/导入解析）

**注意**：旧式 csproj + net462，测试工程需要 mstest/xunit + `Microsoft.NET.Test.Sdk`。可新建独立测试 csproj（SDK-style + net462）引用主程序集。dotnet-test skills 已了解但未安装（如决定做可安装）。

### 2.3 性能优化 — 按需
- 串口大数据量收发时 DataShowPage 流式模式性能（当前用 AppendText 增量，OK）
- 快捷发送列表大时 ListBox 虚拟化（已开启）
- 如需排查性能：安装 dotnet-diag skills（dotnet-trace-collect 等），注意它们主要面向现代 .NET 运行时

### 2.4 不推荐做的事
- **升级 .NET Framework**（用户明确约束；且 xlua/serial_monitor 等原生依赖需重编译）
- **迁移到 Avalonia**（仓库有 `origin/copilot/avalonia` 分支曾尝试，风险高、收益未验证）
- **移除 Costura**（会破坏单文件绿色版发布形态）

## 三、新功能开发流程（已在本仓库实践验证）

以"新增一个工具页"为例：
1. `Pages/XxxPage.xaml` + `.xaml.cs`（继承 `Page`，手写 INPC，DataContext=this）
2. 逻辑放 `ViewModels/XxxViewModel.cs`（需要通知时继承 ObservableObject）
3. 注册到 MainWindow.xaml 的"工具"TabControl（Frame Navigate）
4. csproj 加 `<Compile Include>` + `<Page Include>`
5. 需要 Lua 通道时：`LuaApis.SendChannelsRegister("xxx", ...)`
6. csharpier 格式化 + 冒烟测试

## 四、发布流程（沿用现有）

1. 合并到 master
2. 打 tag → CI 自动构建 Release + 打 zip（见 .github/workflows/build.yml）
3. 发布到 GitHub Releases / 更新 changlog/autoUpdate.xml（AutoUpdater.NET 检查更新用）

## 五、当前已知限制（后续可关注）

- `Global.cs` 仍是静态外观层（setting/uart 全局单例）——功能上够用，若想彻底 DI 化可后续重构
- `MainWindow.xaml.cs` 仍约 900 行（导航/Lua 运行/日志打印等 UI 密集逻辑保留 code-behind，属于合理边界）
- 工具页的弹窗（接收脚本/参数选择）保留 code-behind（依赖 Popup 控件，VM 化收益低）
