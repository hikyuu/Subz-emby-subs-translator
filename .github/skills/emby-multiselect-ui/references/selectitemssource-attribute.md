# SelectItemsSourceAttribute API 参考

**命名空间:** `MediaBrowser.Model.Attributes`
**程序集:** `MediaBrowser.Model.dll`

## 概述

指示可用选项列表由另一个属性提供的特性。被引用的属性类型必须为 `IEnumerable<EditorSelectOption>` 或 `IList<EditorSelectOption>`。

## 声明

```csharp
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public class SelectItemsSourceAttribute : Attribute
```

## 构造函数

```csharp
public SelectItemsSourceAttribute(string propertyName)
```

| 参数           | 说明                                 |
| -------------- | ------------------------------------ |
| `propertyName` | 提供选项列表的属性名称（同一个类中） |

## 用法

```csharp
[Browsable(false)]
public IEnumerable<EditorSelectOption> MyOptionsList { get; set; }

[SelectItemsSource(nameof(MyOptionsList))]
[EditMultilSelect]
public string MySelection { get; set; }
```

## 继承链

```
System.Object → System.Attribute → SelectItemsSourceAttribute
```

## 注意事项

- 源属性**必须**标记 `[Browsable(false)]` 以在 UI 中隐藏
- 源属性返回 `IEnumerable<EditorSelectOption>` 或 `IList<EditorSelectOption>`
- 推荐使用 `nameof()` 而非硬编码字符串，编译时更安全
- 可与 `[EditMultilSelect]`、`[SelectShowRadioGroup]` 组合，也可单独用于单选下拉
- 也适用于枚举类型做值限定：`[SelectItemsSource(nameof(RestrictedList))] public MyEnum Value { get; set; }`

## SDK 文档

参见: `emby-sdk/Documentation/reference/pluginapi/MediaBrowser.Model.Attributes.SelectItemsSourceAttribute.html`
