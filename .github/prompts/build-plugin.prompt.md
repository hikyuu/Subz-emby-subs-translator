---
description: "构建 SubZ.Plugin 产物，有错误则分析并修复"
argument-hint: "构建插件..."
agent: "agent"
---

执行 `scripts/build-plugin.ps1` 构建 SubZ.Plugin 的 Release 产物。构建流程如下：

1. 在 PowerShell 中运行：
   ```powershell
   ./scripts/build-plugin.ps1 -Configuration Release
   ```

2. 如果构建成功：
   - 确认 `artifacts/plugin/SubZ.Plugin.dll` 已生成
   - 确认 `artifacts/SubZ.Plugin-Release.zip` 已生成
   - 确认根目录 `SubZ.Plugin.dll` 已复制

3. 如果构建失败，分析错误信息并解决：
   - `.NET SDK` 缺失：提示安装 .NET SDK
   - 编译错误：定位到 `src/SubZ.Plugin/` 中的源代码，分析并修复错误
   - 依赖缺失：检查 `SubZ.Plugin.csproj` 中的 NuGet 包引用
   - DLL 未找到：检查 `TargetFramework` 是否与脚本中的路径匹配（当前：`netstandard2.0`）

修复后重新执行构建，直到成功。
