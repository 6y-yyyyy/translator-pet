# TranslatorPet

A compact Windows desktop companion for local English ↔ Simplified Chinese translation. Click the floating orb to open the translation window. Translation is offline, with no daily service quota, account, API key, or remote fallback.

Version **0.2.0 — early usable release**. Windows 10/11 x64, .NET Framework 4.8 and Microsoft Visual C++ 2015–2022 x64 runtime are required. CPU inference; no GPU required.

## Run

Extract the complete portable release and launch `TranslatorPet.exe`. Keep its `runtime`, `models` and `engine` folders together. No Python installation is required. Copy text, click the orb, paste, choose source / target languages, and translate. Ctrl+Enter translates; Esc hides the popup. Drag the orb to move it. Right-click for size, static skins and exit.

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
