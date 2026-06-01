# EditMultilSelectAttribute API 参考

**命名空间:** `MediaBrowser.Model.Attributes`
**程序集:** `MediaBrowser.Model.dll`

## 概述

标记一个选择项允许多选的特性。应用在 `string` 属性上时，UI 渲染为复选框列表而非下拉框。

## 声明

```csharp
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public class EditMultilSelectAttribute : Attribute
```

## 用法

与 `[SelectItemsSource(...)]` 配合使用，应用于 `string` 属性：

```csharp
[EditMultilSelect]
[SelectItemsSource(nameof(MyOptionsList))]
public string MySelections { get; set; }
```

## 继承链

```
System.Object → System.Attribute → EditMultilSelectAttribute
```

## 注意事项

- **仅适用于 `string` 属性** —— 不适用于 enum 或其他类型
- 选中的值以**逗号分隔字符串**存储（如 `"val1,val2,val3"`）
- 配合 `[EditMultiline(N)]` 可显示可滚动的多行复选框列表
- 该特性自身无配置参数 —— 仅是一个标记
- 同一属性不允许重复应用（`AllowMultiple = false`）

## SDK 文档

参见: `emby-sdk/Documentation/reference/pluginapi/MediaBrowser.Model.Attributes.EditMultilSelectAttribute.html`
