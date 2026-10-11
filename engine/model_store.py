"""Download only the explicitly requested, hash-pinned model; never user text."""
import hashlib
from http.client import HTTPException
import json
from pathlib import Path
import re
import time
import urllib.request
from urllib.error import URLError


def manifest(root):
    value = json.loads((Path(root) / "model.json").read_text(encoding="utf-8"))
    if (not re.fullmatch(r"[A-Za-z0-9_.-]+\.gguf", value["name"])
            or not re.fullmatch(r"[a-f0-9]{64}", value["sha256"])
            or not isinstance(value["size"], int) or value["size"] <= 0
            or not value["url"].startswith("https://huggingface.co/")):
        raise ValueError("模型配置不完整，请重新解压发布包。")
    return value


def model_path(root):
    return Path(root) / "models" / manifest(root)["name"]


def verify(path, expected):
    with path.open("rb") as f:
        return hashlib.file_digest(f, "sha256").hexdigest() == expected


def prepare_model(root, progress, opener=None):
    asset = manifest(root)
    path = model_path(root)
    path.parent.mkdir(parents=True, exist_ok=True)
    if path.is_file() and path.stat().st_size == asset["size"]:
        progress("正在校验已有模型 …")
        if verify(path, asset["sha256"]):
            return path
    part = path.with_suffix(".gguf.part")
    offset = part.stat().st_size if part.exists() else 0
    if offset > asset["size"]:
        part.unlink()
        offset = 0
    if offset < asset["size"]:
        headers = {"User-Agent": "TranslatorPet/0.3.0"}
        if offset:
            headers["Range"] = f"bytes={offset}-"
        request = urllib.request.Request(asset["url"], headers=headers)
        progress(f"正在连接模型下载源 · 已下载 {offset / 1e9:.2f} GB")
        try:
            with (opener or urllib.request.urlopen)(request, timeout=60) as response:
                if response.status == 206:
                    if not response.headers.get("Content-Range", "").startswith(f"bytes {offset}-"):
                        raise ValueError("服务器返回了错误的下载范围，请稍后重试。")
                elif response.status == 200:
                    offset = 0
                else:
                    raise ValueError("模型下载未成功，请稍后重试。")
                with part.open("ab" if offset else "wb") as out:
                    done, last = offset, 0
                    while data := response.read(1024 * 1024):
                        done += len(data)
                        if done > asset["size"]:
                            raise ValueError("模型文件大小异常，请重新下载。")
                        out.write(data)
                        if time.monotonic() - last >= 0.3:
                            progress(f"下载模型 {done * 100 // asset['size']}% · {done / 1e9:.2f}/{asset['size'] / 1e9:.2f} GB")
                            last = time.monotonic()
        except (URLError, TimeoutError, ConnectionError, OSError, HTTPException):
            raise ValueError("模型下载中断。请检查网络与磁盘空间后重试，已下载的部分会保留。") from None
    progress("正在校验模型完整性 …")
    if part.stat().st_size != asset["size"]:
        raise ValueError("模型尚未下载完整，请重试以继续下载。")
    if not verify(part, asset["sha256"]):
        part.unlink()
        raise ValueError("模型校验失败，未安装该文件，请重新下载。")
    part.replace(path)
    progress("增强模型已就绪 · 之后可离线翻译")
    return path
