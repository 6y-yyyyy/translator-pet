# Changelog

## 0.3.1 — 2026-10-12

- Fix custom skin replacements showing the first imported image because of WPF URI caching.
- Load images from an eagerly decoded file stream and release the file immediately.
- Add UI regression checks for three consecutive replacements of PNG, JPG, BMP and GIF images.
- The offline model is unchanged; existing users do not need to download it again.

## 0.3.0 — 2026-10-11

- Upgrade local inference to Qwen3-4B-Instruct-2507 Q4_K_M with llama.cpp CPU runtime.
- Preserve multi-line context and stream draft translations into the window.
- Clarify the specific warning idiom “here be dragons” without replacing literal stories/maps or inline code.
- Add explicit, resumable 2.5 GB model download and SHA-256 validation. Translation stays offline.
- Add real regression coverage for warnings, identifiers, negation and code comments.
- Publish a smaller app ZIP; the model is downloaded once from the UI. Higher memory use and slower inference than v0.2.x are expected.

## 0.2.1 — 2026-10-11

- Fix offline model loading when the installation path contains Chinese or other non-ASCII characters on Windows.
- Read tokenizer and non-ASCII-path model files through Python's Unicode-aware file handling.
- Add real bidirectional inference regression coverage for paths containing Chinese, spaces and emoji.

## 0.2.0 — 2026-10-11

- Replace online translation with local English / Chinese OPUS-MT models.
- Package a portable Python runtime and CPU inference libraries.
- Separate UI, model worker, dependency preparation and tests.
- Add hash-locked dependencies, third-party notices, MIT project license and CC BY model attribution.
- Add offline, Unicode, cancellation and UI regression tests.
- Add reproducible build scripts, CI and portable packaging without user profiles.

This is an early release with lightweight models and static skins. It has not yet undergone broad hardware and clean-machine testing.
