# Third-party notices

TranslatorPet application code is MIT licensed. That license does not replace the licenses of the runtime, libraries, or translation models below. Keep this file, the model READMEs, `licenses/`, the Python license, and dependency `.dist-info` folders when redistributing a portable build.

## Translation models

Bundled directions: English → Chinese 1.9 and Chinese → English 1.9, packaged by the Argos Translate project from OPUS-MT models.

Authors: **Jörg Tiedemann and Santhosh Thottingal**.

Title: **OPUS-MT — Building open translation services for the World**. Proceedings of the 22nd Annual Conference of the European Association for Machine Translation (EAMT), 2020, Lisbon, Portugal.

The model packages' original READMEs state that the original OPUS models are licensed **Creative Commons Attribution 4.0 International (CC BY 4.0)**. Their READMEs remain under `models/en_zh/README.md` and `models/zh_en/README.md` in the portable build.

- License: https://creativecommons.org/licenses/by/4.0/
- Full license text: `licenses/CC-BY-4.0.txt`
- OPUS-MT source: https://github.com/Helsinki-NLP/Opus-MT
- Argos model index: https://github.com/argosopentech/argospm-index
- Original package URLs: https://argos-net.com/v1/translate-en_zh-1_9.argosmodel and https://argos-net.com/v1/translate-zh_en-1_9.argosmodel

Distribution changes: directory names are normalized to `en_zh` / `zh_en`; the unused Stanza sentence-splitter files are omitted. Translation weight files and tokenizers are unchanged. A project-owned sentence splitter and CTranslate2 CPU int8 inference are used. No endorsement by the original authors is implied.

The setup script first tries the upstream host, then a third-party Hugging Face archive mirror pinned to specific revisions. Exact URLs and SHA-256 digests are recorded in `dependencies.lock.json`. The mirror's generic MIT tag does not supersede the model packages' CC BY 4.0 notices.

## Runtime libraries

| Component | Version | License / source | License location in portable build |
| --- | --- | --- | --- |
| CPython | 3.13.16 x64 | PSF and bundled component licenses; https://www.python.org/ | `runtime/LICENSE.txt` |
| CTranslate2 | 4.8.2 | MIT and bundled components; https://github.com/OpenNMT/CTranslate2 | `licenses/CTranslate2-MIT.txt` and component notices in `licenses/` |
| SentencePiece | 0.2.1 | Apache-2.0 and bundled components; https://github.com/google/sentencepiece | `licenses/SentencePiece-Apache-2.0.txt` and component notices in `licenses/` |
| NumPy | 2.5.4 | BSD-3-Clause and bundled numerical-library licenses; https://numpy.org/ | `runtime/Lib/site-packages/numpy-*.dist-info/` |
| PyYAML | 6.0.3 | MIT; https://pyyaml.org/ | `runtime/Lib/site-packages/pyyaml-*.dist-info/` |

Windows, .NET Framework and the Microsoft Visual C++ runtime are system prerequisites and remain subject to Microsoft's terms. This repository does not bundle the Visual C++ redistributable installer.

CTranslate2's Windows binary also contains oneDNN components and uses the Intel OpenMP runtime. Their notices are included in `licenses/`. The unused NVIDIA cuDNN DLL from the upstream wheel is omitted from this CPU-only distribution; translation weights are not changed.
