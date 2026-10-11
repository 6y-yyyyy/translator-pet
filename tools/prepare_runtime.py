"""Build a relocatable Windows x64 runtime using hash-locked public artifacts."""
import argparse
import hashlib
import json
from pathlib import Path
import shutil
import urllib.request
import zipfile

REPO = Path(__file__).resolve().parents[1]


def verify(path, expected):
    with path.open("rb") as stream:
        return hashlib.file_digest(stream, "sha256").hexdigest() == expected


def download(asset, cache):
    target = cache / asset["name"]
    wheel = cache / "wheels" / asset["name"]
    if wheel.is_file() and verify(wheel, asset["sha256"]):
        return wheel
    if target.is_file() and verify(target, asset["sha256"]):
        return target
    urls = [asset["upstream"], asset["url"]] if "upstream" in asset else [asset["url"]]
    errors = []
    for url in urls:
        part = target.with_suffix(target.suffix + ".part")
        try:
            print("Downloading " + asset["name"], flush=True)
            with urllib.request.urlopen(url, timeout=60) as response, part.open("wb") as out:
                shutil.copyfileobj(response, out, 1024 * 1024)
            if not verify(part, asset["sha256"]):
                raise ValueError("SHA-256 mismatch: " + asset["name"])
            part.replace(target)
            return target
        except Exception as error:
            errors.append(str(error))
            part.unlink(missing_ok=True)
    raise RuntimeError("Cannot download " + asset["name"] + ": " + "; ".join(errors))


def extract(path, destination, model=False):
    destination.mkdir(parents=True, exist_ok=True)
    with zipfile.ZipFile(path) as archive:
        for item in archive.infolist():
            parts = Path(item.filename).parts
            if model:
                parts = parts[1:]
                # Our sentence splitter does not use Stanza or PyTorch weights.
                if not parts or parts[0] == "stanza":
                    continue
            if not parts:
                continue
            if parts[-1].lower() == "cudnn64_9.dll":
                # This product is CPU-only; omit the unused NVIDIA runtime.
                continue
            target = destination.joinpath(*parts).resolve()
            if not target.is_relative_to(destination.resolve()):
                raise ValueError("Archive path escapes destination")
            if item.is_dir():
                target.mkdir(parents=True, exist_ok=True)
            else:
                target.parent.mkdir(parents=True, exist_ok=True)
                with archive.open(item) as source, target.open("wb") as out:
                    shutil.copyfileobj(source, out)


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--output", type=Path, default=REPO / "dist" / "TranslatorPet")
    parser.add_argument("--cache", type=Path, default=REPO / ".cache")
    args = parser.parse_args()
    output, cache = args.output.resolve(), args.cache.resolve()
    output.mkdir(parents=True, exist_ok=True)
    cache.mkdir(parents=True, exist_ok=True)
    lock = json.loads((REPO / "dependencies.lock.json").read_text(encoding="utf-8"))
    for asset in lock["assets"]:
        path = download(asset, cache)
        if asset["kind"] == "python":
            extract(path, output / "runtime")
        elif asset["kind"] == "wheel":
            extract(path, output / "runtime" / "Lib" / "site-packages")
        elif asset["kind"] == "model":
            extract(path, output / "models" / asset["pair"], model=True)
        print("Ready: " + asset["name"], flush=True)
    (output / "runtime" / "python313._pth").write_text("python313.zip\n.\nLib/site-packages\nimport site\n", encoding="ascii")
    (output / "runtime" / "Lib" / "site-packages" / "ctranslate2" / "cudnn64_9.dll").unlink(missing_ok=True)
    shutil.copytree(REPO / "engine", output / "engine", dirs_exist_ok=True, ignore=shutil.ignore_patterns("__pycache__"))
    (output / "portable.marker").write_text("Portable profile lives in ./data\n", encoding="utf-8")
    for name in ("README.md", "README.en.md", "LICENSE", "THIRD_PARTY_NOTICES.md", "dependencies.lock.json"):
        if (REPO / name).exists():
            shutil.copy2(REPO / name, output / name)
    if (REPO / "licenses").exists():
        shutil.copytree(REPO / "licenses", output / "licenses", dirs_exist_ok=True)
    if (REPO / "docs").exists():
        shutil.copytree(REPO / "docs", output / "docs", dirs_exist_ok=True)
    print("Runtime prepared: " + str(output), flush=True)


if __name__ == "__main__":
    main()
