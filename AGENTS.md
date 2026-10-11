# Development conventions

- Keep translation local-only unless a user explicitly requests a different mode. Never introduce a hidden network fallback.
- Do not save input text in logs, command arguments, environment variables, fixtures or user-profile snapshots.
- Keep `src/TranslatorPet` (Windows UI), `engine` (inference), `tools` (build and packaging), and `tests` separate.
- Only list languages whose model directions are installed and covered by a real test.
- Preserve third-party author, license and model metadata when updating dependencies. Pin URLs and SHA-256 in `dependencies.lock.json`.
- Run `tools/build.ps1` and tests appropriate to the change. Run `tools/test.ps1 -WithModels` before packaging.
- Exclude user data, credentials, dependency caches, models and runtime binaries from Git. Portable archives must exclude `data/`.
- Use repository-local Git identity and authentication settings where possible; do not alter global Git settings for this project.
- Report actual local checks separately from CI and clean-machine checks. Do not call an untested binary broadly compatible.
