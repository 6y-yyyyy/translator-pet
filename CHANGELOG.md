# Changelog

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
