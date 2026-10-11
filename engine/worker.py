"""Local-only translation worker. Reads one JSON request; writes JSONL events.

Uses Argos-packaged OPUS-MT weights through CTranslate2, without a web server.
Input text is never stored in a file. Model and runtime loading are local only.
"""
import json
import os
from pathlib import Path
import re
import sys

ROOT = Path(__file__).resolve().parent.parent
ALIASES = {"zh-CN": "zh"}
MAX_TEXT = 10000
MAX_TOKENS = 240


def emit(kind, **payload):
    print(json.dumps({"type": kind, **payload}, ensure_ascii=False), flush=True)


def block_network():
    """Used by integration tests to fail if a Python dependency attempts a connection."""
    import socket
    def denied(*args, **kwargs):
        raise RuntimeError("Network disabled in offline verification")
    socket.create_connection = denied
    socket.socket.connect = denied
    socket.socket.connect_ex = denied


def validate(request):
    text = request.get("text")
    if not isinstance(text, str) or not text.strip():
        raise ValueError("先输入或粘贴需要翻译的文字。")
    if len(text) > MAX_TEXT:
        raise ValueError("单次最多 10,000 字，请分段翻译；没有每日次数限制。")
    source = ALIASES.get(request.get("source"), request.get("source"))
    target = ALIASES.get(request.get("target"), request.get("target"))
    if source not in {"en", "zh"} or target not in {"en", "zh"}:
        raise ValueError("此版本已安装英语和简体中文模型，请选择这两种语言。")
    return text, source, target


def paragraphs(text):
    return re.split(r"(\r\n|\r|\n)", text)


def token_chunks(tokenizer, text):
    # Preserve sentence boundaries where possible. Never silently truncate long input.
    for sentence in re.split(r"(?<=[。！？!?])\s*|(?<=\.)\s+(?=[A-Z])", text):
        if not sentence.strip():
            continue
        tokens = tokenizer.encode(sentence, out_type=str)
        for start in range(0, len(tokens), MAX_TOKENS):
            yield tokens[start:start + MAX_TOKENS]


def translate(request):
    text, source, target = validate(request)
    if source == target:
        return text
    model_dir = ROOT / "models" / (source + "_" + target)
    if not (model_dir / "model" / "model.bin").is_file():
        raise ValueError("缺少离线语言包，请使用完整离线发布包，或按 README 运行模型准备脚本。")
    import ctranslate2
    import sentencepiece
    tokenizer = sentencepiece.SentencePieceProcessor(model_file=str(model_dir / "sentencepiece.model"))
    translator = ctranslate2.Translator(str(model_dir / "model"), device="cpu", compute_type="int8", inter_threads=1, intra_threads=min(4, os.cpu_count() or 1))
    metadata = json.loads((model_dir / "metadata.json").read_text(encoding="utf-8"))
    prefix = metadata.get("target_prefix")
    plan = []
    total = 0
    for paragraph in paragraphs(text):
        chunks = list(token_chunks(tokenizer, paragraph)) if paragraph.strip() else []
        total += len(chunks)
        plan.append((paragraph, chunks))
    output = []
    done = 0
    for paragraph, chunks in plan:
        if not chunks:
            output.append(paragraph)
            continue
        translated = []
        for tokens in chunks:
            emit("progress", message=f"正在本地翻译 {done + 1} / {total} …")
            results = translator.translate_batch([tokens], target_prefix=[[prefix]] if prefix else None, beam_size=4, length_penalty=0.2, replace_unknowns=True, max_decoding_length=1024)
            hypothesis = results[0].hypotheses[0]
            if len(hypothesis) >= 1024:
                raise ValueError("该句生成过长，请缩短原文后重试。")
            # OPUS-MT can emit SentencePiece boundary markers as literal pieces.
            value = tokenizer.decode(hypothesis).replace("\u2581", " ")
            if prefix and value.startswith(prefix):
                value = value[len(prefix):]
            if not value.strip():
                raise ValueError("此段没有生成有效译文，请缩短句子后重试。")
            translated.append(value.strip())
            done += 1
        leading = paragraph[:len(paragraph) - len(paragraph.lstrip())]
        trailing = paragraph[len(paragraph.rstrip()):]
        output.append(leading + ("" if target == "zh" else " ").join(translated) + trailing)
    return "".join(output)


def main():
    if os.environ.get("TRANSLATORPET_DISABLE_NETWORK") == "1":
        block_network()
    try:
        request = json.loads(sys.stdin.readline(MAX_TEXT * 12 + 4096))
        if not isinstance(request, dict):
            raise ValueError("请求格式不正确。")
        emit("result", text=translate(request))
    except (ValueError, KeyError, TypeError):
        error = sys.exc_info()[1]
        emit("error", message=str(error) if isinstance(error, ValueError) and not isinstance(error, json.JSONDecodeError) else "请求格式不正确。")
        return 1
    except Exception:
        # Avoid printing source text or file-system details to users or log files.
        emit("error", message="本地模型无法加载或运行。请检查运行环境和语言包，详见 README。")
        return 1
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
