import hashlib
import io
import json
from pathlib import Path
import sys
import tempfile
import unittest

REPO = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(REPO / "engine"))
import model_store


class Response(io.BytesIO):
    def __init__(self, content, status=200, headers=None):
        super().__init__(content)
        self.status, self.headers = status, headers or {}


class ModelStoreTests(unittest.TestCase):
    def setUp(self):
        scratch = REPO / "artifacts" / "download-tests"
        scratch.mkdir(parents=True, exist_ok=True)
        self.temp = tempfile.TemporaryDirectory(dir=scratch)
        self.addCleanup(self.temp.cleanup)
        self.root = Path(self.temp.name)
        self.content = b"test model bytes" * 64
        self.asset = dict(name="test.gguf", url="https://huggingface.co/example/test.gguf", size=len(self.content), sha256=hashlib.sha256(self.content).hexdigest())
        (self.root / "model.json").write_text(json.dumps(self.asset), encoding="utf-8")
        (self.root / "models").mkdir()

    def test_hash_checked_before_activation(self):
        result = model_store.prepare_model(self.root, lambda s: None, lambda *a, **kw: Response(self.content))
        self.assertEqual(result.read_bytes(), self.content)

    def test_bad_hash_never_installed(self):
        with self.assertRaisesRegex(ValueError, "校验失败"):
            model_store.prepare_model(self.root, lambda s: None, lambda *a, **kw: Response(b"x" * len(self.content)))
        self.assertFalse(model_store.model_path(self.root).exists())

    def test_resume_partial_file(self):
        part = self.root / "models/test.gguf.part"
        part.write_bytes(self.content[:30])
        def opener(request, **kwargs):
            self.assertEqual(request.headers["Range"], "bytes=30-")
            return Response(self.content[30:], 206, {"Content-Range": f"bytes 30-{len(self.content)-1}/{len(self.content)}"})
        self.assertEqual(model_store.prepare_model(self.root, lambda s: None, opener).read_bytes(), self.content)

    def test_full_response_to_resume_restarts_safely(self):
        (self.root / "models/test.gguf.part").write_bytes(self.content[:30])
        self.assertEqual(model_store.prepare_model(self.root, lambda s: None, lambda *a, **kw: Response(self.content)).read_bytes(), self.content)

    def test_wrong_resume_range_rejected(self):
        (self.root / "models/test.gguf.part").write_bytes(self.content[:30])
        with self.assertRaisesRegex(ValueError, "下载范围"):
            model_store.prepare_model(self.root, lambda s: None, lambda *a, **kw: Response(self.content, 206, {"Content-Range": "bytes 0-99/100"}))

    def test_valid_model_never_connects(self):
        model_store.model_path(self.root).write_bytes(self.content)
        def denied(*args, **kwargs):
            self.fail("Already-installed model made a network request")
        model_store.prepare_model(self.root, lambda s: None, denied)

    def test_manifest_path_escape_rejected(self):
        self.asset["name"] = "../outside.gguf"
        (self.root / "model.json").write_text(json.dumps(self.asset), encoding="utf-8")
        with self.assertRaises(ValueError):
            model_store.model_path(self.root)


if __name__ == "__main__":
    unittest.main()
