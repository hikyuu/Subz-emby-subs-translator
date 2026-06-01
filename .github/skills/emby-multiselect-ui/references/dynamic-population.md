# 动态选项填充

如何在运行时从 Emby 服务填充多选选项。

## 核心模式

`EditableOptionsBase` 类是通过反序列化创建的（而非 DI 实例化），因此无法直接注入服务。替代方案是在 `Plugin` 类中，在选项页面展示前进行列表填充。

## 方案一：在 Plugin 中覆写（Simple UI）

适用于继承 `BasePluginSimpleUI<TOptionType>` 的场景：

```csharp
public class MyPlugin : BasePluginSimpleUI<PluginOptions>
{
    private ILibraryManager _libraryManager;

    public MyPlugin(/* ...其他注入服务... */, ILibraryManager libraryManager)
    {
        _libraryManager = libraryManager;
    }

    // UI 渲染前填充
    protected override PluginOptions OnBeforeShowUI(PluginOptions options)
    {
        PopulateMyDynamicList(options);
        return options;
    }

    private void PopulateMyDynamicList(PluginOptions options)
    {
        try
        {
            var virtualFolders = _libraryManager.GetVirtualFolders();
            options.MySelectItemsList = virtualFolders
                .Where(f => !string.IsNullOrWhiteSpace(f.Name) && !string.IsNullOrWhiteSpace(f.ItemId))
                .Select(f => new EditorSelectOption
                {
                    Name = f.Name,
                    Value = f.ItemId
                })
                .OrderBy(o => o.Name)
                .ToList();
        }
        catch (Exception ex)
        {
            // 记录日志，留空列表 —— UI 显示"无可用选项"
            _logger.LogError(ex, "填充选项列表失败");
        }
    }
}
```

## 方案二：覆写 GetOptions（Complex UI）

适用于完整的 `IHasOptionsController` 模式：

在 Plugin 中覆写 `GetOptions()`，在返回前进行填充。

## 重要考量

### 空值安全

始终确保源列表不为 null：

```csharp
public IEnumerable<EditorSelectOption> MySelectItemsList { get; set; }
    = new List<EditorSelectOption>(); // 默认为空列表，而非 null
```

### 异常处理

如果服务调用失败（如 `ILibraryManager` 不可用），捕获异常并留空列表。UI 会显示空的多选控件而不会崩溃。

### 填充时机

- **页面加载时**：`OnBeforeShowUI()` 或 `GetOptions()`
- **保存时**：如果源数据可能已变化（极少需要）
- 避免在 options 类构造函数中填充 —— 此时服务尚不可用

### 持久化

选中的值（逗号分隔的 `Value` 字符串）持久化在插件的 XML 配置文件中。源列表（`MySelectItemsList`）**不会**被持久化 —— 每次都需要重新填充。
