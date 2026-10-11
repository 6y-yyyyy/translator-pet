# TranslatorPet

A compact Windows desktop companion for local English ↔ Simplified Chinese translation. Click the floating orb to open the translation window. Translation is offline, with no daily service quota, account, API key, or remote fallback.

Version **0.2.1 — early usable release**. Windows 10/11 x64, .NET Framework 4.8 and Microsoft Visual C++ 2015–2022 x64 runtime are required. CPU inference; no GPU required. This patch fixes model loading from installation paths containing Chinese or other non-ASCII characters.

**[⬇ Download for Windows — portable ZIP, about 188 MB](https://github.com/6y-yyyyy/translator-pet/releases/download/v0.2.1/TranslatorPet-v0.2.1-windows-x64.zip)** · [Getting started](#getting-started) · [Release page](https://github.com/6y-yyyyy/translator-pet/releases/tag/v0.2.1)

## Why I made this

When browsing GitHub or reading English articles, I want to try understanding the original text myself, so I prefer to keep the page in English. But I often come across an unfamiliar word or a sentence I cannot understand and need a little help before I can keep reading.

I wanted a small tool that stays out of the way and is always within reach: a tiny floating orb on the desktop. When I get stuck, I can copy the text, click the orb, paste it and translate. Once I understand it, I can close the popup and return to the original text. I hope it helps others who like to read this way too.

## Getting started

For **Windows 10 / 11 x64**. This is a portable ZIP: extract it and run the app. It includes the translation models and Python runtime; no account or API key is required.

1. **Download:** click [TranslatorPet-v0.2.1-windows-x64.zip](https://github.com/6y-yyyyy/translator-pet/releases/download/v0.2.1/TranslatorPet-v0.2.1-windows-x64.zip) and wait for the roughly 188 MB download to finish. Alternatively, open the [release page](https://github.com/6y-yyyyy/translator-pet/releases/tag/v0.2.1), expand **Assets**, and select that exact filename. The source archives and **Code → Download ZIP** contain source code, not the runnable app.
2. **Extract:** find the ZIP in Downloads, right-click it, choose **Extract All…**, and select a folder such as your Desktop. The extracted files take about 300 MB. Keep the entire `TranslatorPet` folder, including `runtime`, `models` and `engine`; do not move the exe on its own or run it inside the ZIP.
3. **Launch:** open the extracted `TranslatorPet` folder and double-click **`TranslatorPet.exe`**. If extensions are hidden, its name is **TranslatorPet** and its type is **Application**. The orb appears near the bottom-right of the desktop. Drag it to move it; click it to open the translation window. You can create a desktop shortcut to this exe for later use.
4. **Translate:** copy a word or sentence, click the orb, click **粘贴** (Paste), choose the source and target languages, then click **翻译** (Translate) or press **Ctrl+Enter**. The result appears in the lower box. Click **×** or press **Esc** to hide the popup. Right-click the orb and choose **退出** (Exit) to quit.

After downloading, English ↔ Simplified Chinese translation works offline without a daily quota.

If the app reports a missing runtime, install the required component from Microsoft's official pages: [.NET Framework 4.8 Runtime](https://dotnet.microsoft.com/en-us/download/dotnet-framework/net48) or [Visual C++ 2015–2022 Redistributable, x64](https://learn.microsoft.com/en-us/cpp/windows/latest-supported-vc-redist). Then launch the app again. No separate Python installation or dedicated GPU is needed.

Right-click the orb for size and skin options. The UI currently uses Chinese labels.

## Build and test

From the repository root in Windows PowerShell:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\build.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\prepare-runtime.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\test.ps1 -WithModels
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\package.ps1
```

The build uses the .NET Framework compiler; setup downloads a portable Python runtime and model artifacts pinned by SHA-256 in `dependencies.lock.json`. No machine-specific paths or secrets are needed. Approximately 190 MB of compressed dependencies are downloaded initially. Models and runtime binaries are kept outside Git.

## Architecture and limitations

WPF is the UI layer. A short-lived local Python worker receives one JSON request over stdin and emits JSONL progress/result events over stdout. It runs Argos-packaged OPUS-MT weights through CTranslate2. There is no web server, telemetry, translation history, or background clipboard monitoring. The engine exits after completion or cancellation.

Lightweight models have lower quality than larger translation systems for complex prose and technical language. Input is capped at 10,000 characters per request for responsiveness, not a daily quota. Long input is split without silently dropping tokens. Currently only English and Simplified Chinese are supported. Skins are static, including imported GIFs. Other languages, OCR, automatic detection, animation, signed installers and updates are future work.

Portable preferences live in `data/`; do not redistribute personal profiles. Without `portable.marker`, preferences use the current user's LocalAppData directory. Tests cover real inference, Unicode, cancellation, sentence chunking and UI interactions. Integration tests block Python socket connections; this is not a complete native-library network audit. Broad clean-machine and multi-monitor compatibility testing remains to be done.

Project code: MIT. Models: CC BY 4.0. Preserve all third-party notices and licenses when redistributing. See [THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md), [CONTRIBUTING.md](CONTRIBUTING.md) and the [Chinese guide](README.md).
