# Third-party notices

TranslatorPet application code is MIT licensed. Dependencies and model weights retain their own licenses. Preserve this file, `licenses/`, the Python license and all dependency `.dist-info` directories when distributing the application.

## Current model (v0.3.0)

- Original model: **Qwen3-4B-Instruct-2507**, by the **Qwen Team / Alibaba Cloud**.
- Source: https://huggingface.co/Qwen/Qwen3-4B-Instruct-2507
- Original model revision: `cdbee75f17c01a7cc42f958dc650907174af0554`.
- License: **Apache License 2.0**, full text in `licenses/Qwen3-Apache-2.0.txt`.
- GGUF quantization provider: **Unsloth**. Repository: https://huggingface.co/unsloth/Qwen3-4B-Instruct-2507-GGUF
- Quantization revision: `a06e946bb6b655725eafa393f4a9745d460374c9`; format: **Q4_K_M**.
- Exact artifact: `Qwen3-4B-Instruct-2507-Q4_K_M.gguf`, 2,497,281,120 bytes.
- SHA-256: `3605803b982cb64aead44f6c1b2ae36e3acdb41d8e46c8a94c6533bc4c67e597`.

The project does not train or fine-tune these weights and does not modify the downloaded GGUF. It supplies translation prompts, lossless input chunking and a narrowly scoped warning-idiom preprocessing rule. The Q4_K_M conversion was performed by the quantization provider. No endorsement by Qwen, Alibaba Cloud or Unsloth is implied.

The normal app ZIP does not contain model weights. The user explicitly requests a one-time download from the pinned Hugging Face URL through the app. A separately prepared fully offline directory or `-IncludeModel` archive may include the same verified model and must retain these notices.

## Runtime libraries

| Component | Version | License / source | License location |
| --- | --- | --- | --- |
| CPython | 3.13.16 x64 | PSF and bundled component licenses; https://www.python.org/ | `runtime/LICENSE.txt` |
| llama-cpp-python | 0.3.36 CPU Windows wheel | MIT; https://github.com/abetlen/llama-cpp-python | `licenses/llama-cpp-python-MIT.txt` and wheel `.dist-info/licenses/` |
| llama.cpp / ggml | submodule `0c1e57098bba43ac29e6e3b677cdceebdd22334f` in llama-cpp-python v0.3.36 | MIT; https://github.com/ggml-org/llama.cpp | `licenses/llama.cpp-MIT.txt` |
| NumPy | 2.5.4 | BSD-3-Clause and bundled numerical-library licenses; https://numpy.org/ | `runtime/Lib/site-packages/numpy-*.dist-info/` |
| typing_extensions | 4.15.0 | PSF-2.0; https://github.com/python/typing_extensions | wheel `.dist-info/licenses/` |
| diskcache | 5.6.3 | Apache-2.0; https://github.com/grantjenks/python-diskcache | wheel `.dist-info/LICENSE` |
| Jinja2 | 3.1.6 | BSD-3-Clause; https://github.com/pallets/jinja | wheel `.dist-info/licenses/` |
| MarkupSafe | 3.0.3 | BSD-3-Clause; https://github.com/pallets/markupsafe | wheel `.dist-info/licenses/` |

Diskcache is a dependency of the wrapper; TranslatorPet does not enable disk prompt/session caching. Translation text and results are not saved. The CPU wheel and all Python dependencies are pinned by SHA-256 in `dependencies.lock.json`.

Windows, .NET Framework and the Microsoft Visual C++ runtime are system prerequisites subject to Microsoft's terms. The Visual C++ redistributable installer is not bundled.

## Earlier versions

The v0.2.x releases used Argos-packaged OPUS-MT English ↔ Chinese 1.9 weights (CC BY 4.0), CTranslate2 and SentencePiece. Their licenses remain in the repository for historical releases. They are not the translation backend for v0.3.0. See the notices at the corresponding Git tag for those distributions.
