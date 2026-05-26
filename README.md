# SubZ

[English](#english) | [中文](#中文)

<a id="english"></a>

SubZ is an Emby server plugin for **library-ingest/manual batch subtitle translation and bilingual subtitle generation** via LLM APIs.

![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg) ![Platform: Emby Plugin](https://img.shields.io/badge/Platform-Emby%20Plugin-2563eb) ![Runtime: .NET Standard 2.0](https://img.shields.io/badge/Runtime-.NET%20Standard%202.0-7c3aed) ![Language: C# Core](https://img.shields.io/badge/Language-C%23%20(Core)-16a34a) ![Scripts: PowerShell + HTML](https://img.shields.io/badge/Scripts-PowerShell%20%2B%20HTML-0ea5e9)

> API-first, no local model runtime required.

## Table of Contents

- [Compliance Notice](#compliance-notice)
- [Features](#features)
- [Workflow](#workflow)
- [Requirements](#requirements)
- [Supported Video File Types](#supported-video-file-types)
- [Install](#install)
- [Configuration](#configuration)
- [Manual Run API](#manual-run-api)
- [Credits](#credits)
- [License](#license)

## Compliance Notice

- SubZ does not provide or bundle media sources, scraping modules, or piracy-related functionality.
- SubZ only processes subtitles from your own library files and provider APIs you configure.
- You are responsible for ensuring your usage complies with local laws, copyright requirements, and third-party API terms.
- Before publishing/distributing this plugin, verify all bundled assets and dependencies are properly licensed.

## Features

- Embedded + external subtitle source detection
  - External subtitles first (`.srt/.ass/.ssa/.vtt`)
  - Fallback to embedded text subtitle tracks from media containers (e.g., MKV)
  - `PreferredSourceLanguage` is applied to both external subtitle matching and embedded track scoring
  - Limitation: image-based embedded subtitle tracks (e.g., `SUP/PGS/VobSub/dvd_subtitle`) are currently not translatable
- Skip logic
  - Automatically skips when target-language subtitles already exist
- LLM translation pipeline
  - LLM `/chat/completions` translation
  - Batch translation + self-healing retry strategy
  - Automatic `thinking` capability detection
  - If supported, send `thinking=disabled` (non-thinking mode); if not supported, auto-fallback without `thinking`
- Runtime task control
  - Pause / Resume / Stop queue execution
  - Real-time status and runtime logs via API
- Bilingual subtitle output
  - Original + translated lines merged per cue
  - Reflow to max 2 lines per subtitle cue
- Output formats
  - `.ass` (with configurable font name/size)
  - `.srt`
  - ASS output sanitizes HTML-style tags (e.g., `<font ...>`) to avoid literal tag leakage on screen
- Execution modes
  - Manual target mode: run by specific **folder** or **file**
  - Library-ingest auto mode: enabled when `Enable plugin=true` and `Manual target only mode=false`
- Emby-native plugin UI and plugin card thumbnail support
- Embedded runtime status dashboard in Emby UI (sidebar menu: Server → SubZ Status)

## Workflow

![SubZ Workflow](docs/images/subz-workflow.en.png)

## Requirements

- Emby Server 4.9+
- `ffprobe` and `ffmpeg` available in server runtime
- An LLM API key

## Supported Video File Types

SubZ currently scans and processes these video file extensions:

- `.mkv`
- `.mp4`
- `.m4v`
- `.avi`
- `.ts`
- `.m2ts`
- `.mov`

Notes:
- In manual **folder** mode, video files are scanned recursively in all subfolders.
- In manual **single-file** mode, the selected file must be one of the extensions above.

## Install

1. Copy the `SubZ.Plugin.dll` to Emby plugins directory.

2. Restart Emby server.

## Configuration

### Configuration Panel Reference

| Module | Option | Description |
|---|---|---|
| Core | `EnablePlugin` | Global on/off switch. Default: `true`. |
| Core | `ManualTargetOnlyMode` | Skip library-ingest auto trigger, run only manual targets. Default: `false`. |
| Core | `TargetLanguage` | Output language code. Default: `zh-CN`. Existing target-language subtitles will be skipped. |
| Manual Run | `ManualTargetFolder` | Folder target for a manual job. Default: empty. Scans recursively. |
| Manual Run | `ManualTargetFile` | Single file target for a manual job. Default: empty. Use folder or file, not both. |
| Manual Run | `RunOnceNow` | On save, queue one manual task with current target. Default: `false`. Auto-resets after accepted. |
| Output | `OutputFormat` | `srt` or `ass`. Default: `srt`. |
| Output | `AssFontName` | ASS font family. Default: `Microsoft YaHei`. Requires font availability in rendering environment. |
| Output | `AssFontSize` | ASS font size. Default: `60`. |
| Output | `AssFontColor` | ASS primary color. Default: `&H00FFFFFF`. Supports `&H00RRGGBB` or `#RRGGBB`. |
| LLM | `ApiProvider` | Provider label for profile behavior. Default: `deepseek`. |
| LLM | `ApiBaseUrl` | LLM base URL (SubZ calls `/chat/completions`). Default: `https://api.deepseek.com`. |
| LLM | `ApiKey` | LLM API credential. Default: empty. |
| LLM | `Model` | Model ID used in request payload. Default: `deepseek-v4-flash`. |
| LLM | `BatchSize` | Subtitle cues per request. Default: `120`. Higher values improve speed but raise timeout/misalignment risk. |
| LLM | `PreferredSourceLanguage` | Dropdown option for preferred source language. Default: `en`. Used for external subtitle matching and embedded track preference; falls back to any available subtitle. |
| Media Tools | `FfmpegPath` | Subtitle extraction executable path. Leave empty to prefer Emby's built-in path, then fallback to `/bin/ffmpeg`. |
| Media Tools | `FfprobePath` | Subtitle probing executable path. Leave empty to prefer Emby's built-in path, then fallback to `/bin/ffprobe`. |
| Logging & Status | `LogFileMaxSizeMb` | Runtime log rolling size limit. Default: `10`. |
| Logging & Status | `LogRetentionDays` | Runtime log retention. Default: `7`. |
| Logging & Status | `DebugLogMode` | Verbose diagnostics (full request/response body and ffprobe/ffmpeg details). Default: `false`. |
| Logging & Status | `StatusPageUrl` | Stored external status page link. Default: `http://localhost:18123/subz-status.html`. |
| Reliability | `PreserveSubtitleTags` | Protect and restore tags/placeholders around translation. Default: `true`. |
| Reliability | `EnableTailRetry` | Self-healing retries for failed/unchanged segments. Default: `true`. |
| Reliability | `TailRetryAttempts` | Max tail/healing retry rounds. Default: `2`. |
| Reliability | `PreferNonForcedTrack` | Prefer non-forced subtitle tracks. Default: `true`. |
| Reliability | `PreferNonHiTrack` | Prefer non-hearing-impaired subtitle tracks. Default: `true`. |
| Reliability | `PreferTextSubtitleTrack` | Prefer text subtitle tracks over image-based tracks. Default: `true`. |

Default API profile:

- Provider: `deepseek`
- Base URL: `https://api.deepseek.com`
- Model: `deepseek-v4-flash`
- BatchSize: `120`
- ParallelRequests: `2`
- RetryCount: `2`

Thinking behavior:
- SubZ auto-detects whether the current provider/model supports the `thinking` field.
- If supported, SubZ enables non-thinking mode by sending `thinking=disabled`.
- If not supported, SubZ retries automatically without the `thinking` field and caches this capability per `baseUrl + model`.

## Manual Run API

- `POST /SubZ/Translate/Run`

Body (folder target):

```json
{
  "targetFolderPath": "/path/to/folder",
  "targetFilePath": null
}
```

Body (single file target):

```json
{
  "targetFolderPath": null,
  "targetFilePath": "/path/to/video.mkv"
}
```

Rules:
- Provide exactly one of `targetFolderPath` or `targetFilePath`
- Target path must exist

Status APIs:
- `GET /SubZ/Translate/Queue`
- `GET /SubZ/Translate/Status`
- `GET /SubZ/Translate/Status?LogLimit=120&LogSource=file` (read last lines from runtime log file)
- `GET /SubZ/Translate/Status?LogSource=file&TokenDays=7&TokenLimit=200` (token usage range/filter)

Control via status endpoint:
- `GET /SubZ/Translate/Status?Cmd=pause`
- `GET /SubZ/Translate/Status?Cmd=resume`
- `GET /SubZ/Translate/Status?Cmd=stop`

Notes:
- If task state stays `Queued`, check `IsPaused` in status response.
- When `IsPaused=true`, queued jobs will not start until resumed.

## Credits

- Inspired by workflow ideas from [Translate_Subs](https://github.com/dexusno/Translate_Subs)

## License

This project is licensed under the [MIT License](LICENSE).

---

## 中文

SubZ 是一个 Emby 服务端插件，通过LLM API实现自动入库/手动批量字幕翻译与双语字幕生成。

![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg) ![Platform: Emby Plugin](https://img.shields.io/badge/Platform-Emby%20Plugin-2563eb) ![Runtime: .NET Standard 2.0](https://img.shields.io/badge/Runtime-.NET%20Standard%202.0-7c3aed) ![Language: C# Core](https://img.shields.io/badge/Language-C%23%20(Core)-16a34a) ![Scripts: PowerShell + HTML](https://img.shields.io/badge/Scripts-PowerShell%20%2B%20HTML-0ea5e9)

> 仅 API 模式，不依赖本地模型推理。

## 目录

- [合规声明](#合规声明)
- [功能特性](#功能特性)
- [流程图](#流程图)
- [运行要求](#运行要求)
- [支持的视频文件类型](#支持的视频文件类型)
- [安装方式](#安装方式)
- [配置说明](#配置说明)
- [手动执行 API](#手动执行-api)
- [致谢](#致谢)
- [许可证](#许可证)

## 合规声明

- SubZ 不提供或内置媒体资源来源、爬虫模块或任何盗版相关功能。
- SubZ 仅处理你自有媒体库中的字幕文件，以及你自行配置的模型 API。
- 你需要自行确保使用行为符合当地法律法规、版权要求及第三方 API 服务条款。
- 在发布或分发本插件前，请确认所包含资源与依赖均具备合法许可证。

## 功能特性

- 字幕来源检测（外挂 + 封装内）
  - 优先外挂字幕（`.srt/.ass/.ssa/.vtt`）
  - 回退到视频封装内文本字幕轨（如 MKV）
  - `源语言偏好` 同时用于外挂字幕匹配和内嵌字幕轨优先选择
  - 限制：若内嵌字幕为图形字幕轨（如 `SUP/PGS/VobSub/dvd_subtitle`），当前版本无法直接翻译
- 跳过逻辑
  - 若已存在目标语言字幕，自动跳过
- LLM 翻译链路
  - 通过 LLM `/chat/completions` 进行翻译
  - 批量翻译 + 失败自愈重试
  - 自动检测 `thinking` 能力
  - 若支持则发送 `thinking=disabled`（non-thinking 模式）；若不支持则自动回退为不发送 `thinking`
- 运行控制
  - 支持暂停 / 继续 / 停止队列
  - 支持 API 实时状态与运行日志
- 双语字幕输出
  - 每条 cue 合并“原文 + 译文”
  - 自动重排为最多 2 行
- 输出格式
  - `.ass`（支持字体名/字号配置）
  - `.srt`
  - ASS 输出会清洗 HTML 风格标签（如 `<font ...>`），避免标签文本直接显示在画面中
- 执行模式
  - 手动模式：按指定**文件夹**或**文件**执行
  - 自动入库模式：当 `启用插件=true` 且 `仅手动目标模式=false` 时启用
- Emby 原生插件设置页 + 插件卡片缩略图支持
- 内置运行状态仪表盘（Emby 侧边栏：服务器 → SubZ 状态）

## 流程图

![SubZ 流程图](docs/images/subz-workflow.zh-CN.png)

## 运行要求

- Emby Server 4.9+
- 服务器运行环境可调用 `ffprobe` 与 `ffmpeg`
- 可用的 LLM API Key

## 支持的视频文件类型

SubZ 当前会扫描并处理以下视频扩展名：

- `.mkv`
- `.mp4`
- `.m4v`
- `.avi`
- `.ts`
- `.m2ts`
- `.mov`

说明：
- 手动**文件夹**模式下，会递归扫描该文件夹及其所有子目录中的视频文件。
- 手动**单文件**模式下，所选文件必须是以上扩展名之一。

## 安装方式

1. 将 `SubZ.Plugin.dll` 复制到 Emby `plugins` 目录。

2. 重启 Emby 服务。

## 配置说明

### 配置面板详细说明

| 模块 | 配置项 | 说明 |
|---|---|---|
| 核心 | `启用插件` | 全局启用或禁用 SubZ。默认：`true`。 |
| 核心 | `仅手动目标模式` | 跳过自动入库触发，仅允许手动目标执行。默认：`false`。 |
| 核心 | `目标语言` | 翻译输出目标语言代码。默认：`zh-CN`。若已存在该语言字幕会自动跳过。 |
| 手动执行 | `手动执行目标文件夹` | 手动任务的文件夹路径。默认：空。会递归扫描子目录。 |
| 手动执行 | `手动执行目标文件` | 手动任务的单文件路径。默认：空。与文件夹二选一。 |
| 手动执行 | `立即执行一次` | 保存时受理并入队一次手动任务。默认：`false`。受理后会自动复位。 |
| 输出 | `输出格式` | `srt` 或 `ass`。默认：`srt`。 |
| 输出 | `ASS 字体名称` | ASS 字体族名称。默认：`微软雅黑`。需在渲染环境可用。 |
| 输出 | `ASS 字体大小` | ASS 字号。默认：`60`。 |
| 输出 | `ASS 字体颜色` | ASS 主文字颜色。默认：`&H00FFFFFF`。支持 `&H00RRGGBB` 或 `#RRGGBB`。 |
| 模型接口 | `API 提供方` | 提供方标识。默认：`deepseek`。 |
| 模型接口 | `API 地址` | LLM 接口基础地址（调用 `/chat/completions`）。默认：`https://api.deepseek.com`。 |
| 模型接口 | `API 密钥` | LLM 鉴权密钥。默认：空。 |
| 模型接口 | `模型` | 请求中的模型标识。默认：`deepseek-v4-flash`。 |
| 模型接口 | `每批字幕条数` | 单次请求包含字幕条数。默认：`120`。值越大吞吐越高，但超时/错位风险也更高。 |
| 模型接口 | `源语言偏好` | 外挂字幕优先匹配语言代码。默认：`en`。未命中时回退到任意可用字幕。 |
| 媒体工具 | `FFmpeg 路径` | 抽取封装字幕用的可执行路径。留空时优先使用 Emby 内置路径，最后兜底 `/bin/ffmpeg`。 |
| 媒体工具 | `FFprobe 路径` | 探测字幕轨信息用的可执行路径。留空时优先使用 Emby 内置路径，最后兜底 `/bin/ffprobe`。 |
| 日志与状态 | `日志文件最大大小(MB)` | 单个运行日志滚动上限。默认：`10`。 |
| 日志与状态 | `日志保留天数` | 运行日志保留天数。默认：`7`。 |
| 日志与状态 | `详细日志模式(Debug)` | 记录完整请求/响应正文及 ffprobe/ffmpeg 细节。默认：`false`。 |
| 日志与状态 | `状态页访问路径` | 外部状态页链接保存字段。默认：`http://localhost:18123/subz-status.html`。 |
| 稳定性 | `保留字幕标签` | 翻译前后保护并恢复标签/占位符。默认：`true`。 |
| 稳定性 | `启用尾段重试` | 对失败或未变化片段执行自愈重试。默认：`true`。 |
| 稳定性 | `尾段重试次数` | 自愈重试最大轮数。默认：`2`。 |
| 稳定性 | `优先非强制字幕轨` | 选源时优先非强制字幕轨。默认：`true`。 |
| 稳定性 | `优先非听障字幕轨` | 选源时优先非听障字幕轨。默认：`true`。 |
| 稳定性 | `优先文本字幕轨` | 选源时优先文本字幕轨。默认：`true`。 |

默认 API 配置：

- Provider：`deepseek`
- Base URL：`https://api.deepseek.com`
- Model：`deepseek-v4-flash`
- BatchSize：`120`
- ParallelRequests：`2`
- RetryCount：`2`

Thinking 行为说明：
- SubZ 会自动检测当前 provider/model 是否支持 `thinking` 字段。
- 若支持，SubZ 会启用 non-thinking 模式并发送 `thinking=disabled`。
- 若不支持，SubZ 会自动重试为不带 `thinking` 参数，并按 `baseUrl + model` 缓存能力结果。

## 手动执行 API

- `POST /SubZ/Translate/Run`

按文件夹执行：

```json
{
  "targetFolderPath": "/path/to/folder",
  "targetFilePath": null
}
```

按单文件执行：

```json
{
  "targetFolderPath": null,
  "targetFilePath": "/path/to/video.mkv"
}
```

规则：
- `targetFolderPath` 与 `targetFilePath` 二选一
- 路径必须存在

状态接口：
- `GET /SubZ/Translate/Queue`
- `GET /SubZ/Translate/Status`
- `GET /SubZ/Translate/Status?LogLimit=120&LogSource=file`（从运行日志文件读取最近日志）

控制接口（通过状态接口参数）：
- `GET /SubZ/Translate/Status?Cmd=pause`
- `GET /SubZ/Translate/Status?Cmd=resume`
- `GET /SubZ/Translate/Status?Cmd=stop`

说明：
- 如果任务一直是 `Queued`，优先检查状态响应里的 `IsPaused`。
- 当 `IsPaused=true` 时，队列不会启动，需先继续运行。

## 致谢

- 流程设计参考：[Translate_Subs](https://github.com/dexusno/Translate_Subs)

## 许可证

本项目采用 [MIT License](LICENSE)。
