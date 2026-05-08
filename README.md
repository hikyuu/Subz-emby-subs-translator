# SubZ

[English](README.md) | [中文](README.zh-CN.md)


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
- [External Status Page (Optional)](#external-status-page-optional)
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
- Execution modes
  - Manual target mode: run by specific **folder** or **file**
  - Library-ingest auto mode: enabled when `Enable plugin=true` and `Manual target only mode=false`
- Emby-native plugin UI and plugin card thumbnail support
- External lightweight status page support (optional)

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

1. Use the packaged plugin DLL (`artifacts/plugin/SubZ.Plugin.dll`) or `SubZ.Plugin.dll`.

2. Copy the DLL to Emby plugins directory.

3. (Optional) Copy `subz-status.html` to your Emby config folder.

4. Restart Emby server.

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
| LLM | `PreferredSourceLanguage` | Preferred source subtitle language code. Default: `en`. Falls back to any available subtitle. |
| Media Tools | `FfmpegPath` | Subtitle extraction executable path. Default: `/bin/ffmpeg`. |
| Media Tools | `FfprobePath` | Subtitle probing executable path. Default: `/bin/ffprobe`. |
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

Control via status endpoint:
- `GET /SubZ/Translate/Status?Cmd=pause`
- `GET /SubZ/Translate/Status?Cmd=resume`
- `GET /SubZ/Translate/Status?Cmd=stop`

Notes:
- If task state stays `Queued`, check `IsPaused` in status response.
- When `IsPaused=true`, queued jobs will not start until resumed.

## External Status Page (Optional)

SubZ keeps the Emby native plugin settings page unchanged.

For richer runtime monitoring, you can host an external HTML page that reads:
- `GET /SubZ/Translate/Status`
- `GET /SubZ/Translate/Queue`

Typical deployment:
- Place `subz-status.html` under your Emby config storage path.
- Serve it with any static web server (nginx/caddy/apache).
- Configure Emby base URL + Emby API Token in the page.

## Credits

- Inspired by workflow ideas from [Translate_Subs](https://github.com/dexusno/Translate_Subs)

## License

This project is licensed under the [MIT License](LICENSE).
