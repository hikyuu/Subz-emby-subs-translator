# EditorSelectOption API 参考

**命名空间:** `Emby.Web.GenericEdit.Common`

## 概述

表示多选或单选 UI 元素中的一个可选项。每个实例对应一个复选框（多选模式）或一个下拉项（单选模式）。

## 属性

| 属性          | 类型     | 默认值 | 说明                                                 |
| ------------- | -------- | ------ | ---------------------------------------------------- |
| `Name`        | `string` | `null` | **必填。** UI 中显示的标签文本                       |
| `Value`       | `string` | `null` | **必填。** 选中时存储的值                            |
| `ShortName`   | `string` | `null` | 缩写标签（如短日期格式）                             |
| `IsEnabled`   | `bool`   | `true` | 是否可选；`false` = 置灰禁用                         |
| `ToolTip`     | `string` | `null` | 鼠标悬停提示文本                                     |
| `Color`       | `string` | `null` | 应用于选项文本的 CSS 颜色（如 `"red"`、`"#ff6600"`） |
| `DisplayHint` | `string` | `null` | 额外显示提示                                         |
| `FilterValue` | `string` | `null` | 用于筛选/搜索的值                                    |

## 构造函数

该类有多个构造函数重载。最常用的是：

```csharp
// 无参构造（通过对象初始化器设置属性）
new EditorSelectOption
{
    Name = "中文（简体）",
    Value = "chi"
}

// 全参构造
new EditorSelectOption(
    value: string,
    name: string,
    isEnabled: bool,
    shortName: string,
    toolTip: string,
    color: string,
    displayHint: string,
    filterValue: string
)
```

## 创建选项

### 静态列表

```csharp
[Browsable(false)]
public IEnumerable<EditorSelectOption> LanguageOptions { get; set; } = new List<EditorSelectOption>
{
    new() { Name = "English",    Value = "eng" },
    new() { Name = "中文",       Value = "chi" },
    new() { Name = "日本語",     Value = "jpn" },
};
```

### 从数据库/服务动态生成

```csharp
options.MyList = someCollection
    .Select(item => new EditorSelectOption
    {
        Name = item.DisplayName,
        Value = item.Id,
        IsEnabled = item.IsAvailable
    })
    .ToList();
```

### 按条件禁用

```csharp
new EditorSelectOption
{
    Name = "周末（已禁用）",
    Value = "saturday",
    IsEnabled = false,
    Color = "gray"
}
```

## 重要提示

- **`Name` 与 `Value` 的区别：** `Name` 是用户看到的文本；`Value` 是存入逗号分隔字符串的值
- 如果 `Name` 为 null/空，复选框无标签（应避免）
- 如果 `Value` 为 null/空，选中时不会存入有价值的内容
- 确保 `Value` 唯一 —— 重复值在逗号分隔格式下会导致异常行为
