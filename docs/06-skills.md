# pi Skills 使用说明

项目已安装 2 个 dotnet 官方 skills（来自 https://github.com/dotnet/skills，MIT），供 agent 在相关任务中自动/手动加载。

## 已安装

### 1. msbuild-modernization（`.pi/skills/msbuild-modernization/`）
**用途**：旧式 csproj → SDK-style 迁移的完整指南（TFM 映射、packages.config→PackageReference、隐式 globbing、WPF 项目、常见坑、工具链）。

**触发场景**：
- 评估/执行 SDK-style 迁移（见 docs/05-next-steps.md 2.1 节）
- 修改构建系统、csproj、packages.config
- 手工触发：`/skill:msbuild-modernization`

**注意**：该 skill 自身标注 "DO NOT USE FOR: .NET Framework projects that cannot move to SDK-style"——本项目（net462 + Costura + 双平台）能否迁移需按清单逐项验证，**不要盲目套用**。

### 2. dotnet-pinvoke（`.pi/skills/dotnet-pinvoke/`，含 references/type-mapping.md、references/diagnostics.md）
**用途**：正确编写/审查 P/Invoke（DllImport）与原生互操作：类型映射、字符串编码、内存所有权、SafeHandle、x86 调用约定、崩溃诊断。

**触发场景**：
- 新增/修改 `[DllImport]`（本项目有 Tools/Win32.cs、SerialMonitorPage 的 serial_monitor.dll 调用）
- 与原生库（serial_monitor_rs 的 Rust DLL、libusb）交互时
- 排查 AccessViolationException / DllNotFoundException / 内存损坏
- 手工触发：`/skill:dotnet-pinvoke`

**本项目相关注意**：
- 项目是 **x86/x64 双平台**，skill 强调 x86 目标必须显式 `CallingConvention`
- .NET Framework 4.6.2 只能用 `DllImport`（`LibraryImport` 是 .NET 7+）

## 未安装的 skills 及原因

| 类别 | 不装原因 |
|---|---|
| dotnet-test（code-testing-agent 等） | 项目无测试工程；如未来加测试再按需安装 |
| dotnet-diag（trace/性能） | 主要面向现代 .NET 运行时，net462 场景不匹配 |
| dotnet-msbuild 其余（binlog/perf 等） | 本项目构建简单，暂不需要 |
| Blazor/MAUI/EF/ASP.NET Core/Upgrade | 技术栈完全不适用 |

## 触发机制（pi 的 skill 加载规则）

- pi 启动时扫描 `.pi/skills/`，只把 **name + description** 放进系统提示（渐进式披露）
- 任务匹配 description 时，agent 用 `read` 加载完整 SKILL.md 执行
- 也可用 `/skill:名称` 手动强制加载
- 新增 skill：目录下放 `SKILL.md`（frontmatter 必须含 name + description，name 小写字母数字连字符）

## 更新方式

```bash
git clone --depth 1 https://github.com/dotnet/skills /tmp/dotnet-skills
cp -r /tmp/dotnet-skills/plugins/<插件>/skills/<skill名> .pi/skills/
```
（注意同时复制 skill 的 `references/` 子目录）
