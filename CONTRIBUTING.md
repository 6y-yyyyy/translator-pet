# Contributing

Build and test with the commands in README. Keep source changes independent of local absolute paths, profiles, downloaded models and credentials.

For a fix, describe the concrete problem and the behavior after the change. Add a regression test when the change affects text processing, process lifetime, cancellation, model preparation or the UI interaction contract. UI changes should include a screenshot from `--preview` or the live app.

For new language packs, include upstream authors, license, exact download URLs, SHA-256 hashes and a real translation test in both supported directions. A mirror's license tag is not a substitute for a model's original license. Do not add unreviewed automatic downloads at translation time.

The worker protocol accepts one JSON object (`text`, `source`, `target`) on stdin and emits JSONL records of type `progress`, `result` or `error`. Keep input out of command-line arguments and logs. New translation backends should preserve cancellation and explicit privacy behavior.

Bug reports should include Windows version, display scaling, app version, reproduction steps and a non-sensitive example. Never include private source text or user profiles without redaction.

Run `tools/test.ps1 -WithModels` before a release, then `tools/package.ps1`. Review `THIRD_PARTY_NOTICES.md`, dependency pins, screenshots and test results. A public release additionally needs testing on a clean Windows x64 machine; CI success alone does not establish broad compatibility.
