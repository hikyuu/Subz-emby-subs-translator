---
name: emby-multiselect-ui
description: "在 Emby Server 插件中创建多选（复选框列表）UI 控件。适用于：为 Emby 插件添加多选配置项、使用 [EditMultilSelect] 搭配自定义 EditorSelectOption 列表、从 ILibraryManager 等服务动态填充选项。"
argument-hint: "[任务描述] 关于 Emby 多选 UI"
---

# Emby 多选 UI

在 Emby Server 插件中创建多选复选框列表配置控件。

## 适用场景

- 为 Emby 插件设置页添加**多选字段**
- 从 `ILibraryManager` 等运行时服务填充选项
- 读取/写入多选控件存储的逗号分隔字符串值

## 快速决策树

```
Emby 插件需要多选配置？
├── 固定选项列表？                → [静态选项](#静态固定选项)
├── 选项来自 Emby 服务？          → [动态选项](./references/dynamic-population.md)
├── 需要多行滚动布局？            → 添加 [EditMultiline(N)]
└── 只需单选下拉？                → 去掉 [EditMultilSelect]，仅用 [SelectItemsSource]
```

## 三个核心要素

每个多选必须包含三样东西：

| #   | 要素                                                                  | 位置                   |
| --- | --------------------------------------------------------------------- | ---------------------- |
| 1   | `EditorSelectOption` **源列表** → `[Browsable(false)]`                | 同一个类中，对 UI 隐藏 |
| 2   | **目标 `string`** 属性 + `[EditMultilSelect]` + `[SelectItemsSource]` | 同一个类中，对 UI 可见 |
| 3   | **填充逻辑**（动态列表需要）                                          | Plugin 类或工厂方法    |

## 静态（固定）选项

最简单的场景 —— 选项不变。`EditorSelectOption` 完整属性说明 → [./references/editorselectoption.md](./references/editorselectoption.md)。

```csharp
// 第 1 步：源列表（对 UI 隐藏）
[Browsable(false)]
public IEnumerable<EditorSelectOption> LanguageList { get; set; } = new List<EditorSelectOption>
{
    new() { Name = "中文（简体）", Value = "chi" },
    new() { Name = "English",        Value = "eng" },
    new() { Name = "日本語",          Value = "jpn" },
};

// 第 2 步：目标属性（UI 可见）
[DisplayName("跳过语言 / Skip Languages")]
[EditMultilSelect]
[SelectItemsSource(nameof(LanguageList))]
public string SkipLanguages { get; set; } = string.Empty;
```

## 动态选项

选项依赖运行时数据（媒体库、用户等）。含异常处理的完整模式 → [./references/dynamic-population.md](./references/dynamic-population.md)。

一句话：在 Plugin 类中通过 `OnBeforeShowUI()` 填充，切勿在 options 类的构造函数中填充。

## 读取选中值

`string` 属性以逗号分隔存储选中的 `Value`：

```csharp
var selected = (options.SkipLanguages ?? "")
    .Split(',', StringSplitOptions.RemoveEmptyEntries)
    .Select(s => s.Trim())
    .ToList();
// 空列表 = 未选中任何项
```

## 关键类与特性

| 类/特性                      | 作用                               | 详情                                                |
| ---------------------------- | ---------------------------------- | --------------------------------------------------- |
| `EditMultilSelectAttribute`  | 将 `string` 变为复选框列表         | [参考](./references/editmultilselect-attribute.md)  |
| `SelectItemsSourceAttribute` | 指向提供选项列表的属性             | [参考](./references/selectitemssource-attribute.md) |
| `EditorSelectOption`         | 单个复选框项（`Name`、`Value` 等） | [参考](./references/editorselectoption.md)          |

## 实际案例

- **本项目**: `src/SubZ.Plugin/Configuration/PluginOptions.cs` —— 两个多选字段（跳过语言 + 媒体库筛选）
- **语言工厂**: `src/SubZ.Plugin/Configuration/LanguageSelectOptionProvider.cs`
- **SDK 示例**: `emby-sdk/SampleCode/Examples/EmbyPluginUiDemo/UI/Selection/SelectionUI.cs`

## 常见坑点

1. **忘记 `[Browsable(false)]`** → 源列表会作为独立的 UI 字段显示出来
2. **源列表为 null** → 初始化为 `new List<EditorSelectOption>()`，永远不要为 null
3. **使用枚举类型** → 多选需要 `string` 类型，不是 enum
4. **`Value` 为空或重复** → 逗号分隔格式下会导致异常行为
5. **在构造函数中填充** → 服务不可用；应使用 `OnBeforeShowUI()` 替代
