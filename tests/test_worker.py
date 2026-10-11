import importlib.util
import json
import os
from pathlib import Path
import subprocess
import shutil
import tempfile
import unittest
from unittest.mock import patch

REPO = Path(__file__).resolve().parents[1]
spec = importlib.util.spec_from_file_location("worker", REPO / "engine" / "worker.py")
worker = importlib.util.module_from_spec(spec)
spec.loader.exec_module(worker)


class InputTests(unittest.TestCase):
    def test_empty_rejected(self):
        for value in (None, "", "  ", 123):
            with self.assertRaises(ValueError):
                worker.validate({"text": value, "source": "en", "target": "zh"})

    def test_alias_and_unsupported_language(self):
        self.assertEqual(worker.validate({"text": "Hello", "source": "en", "target": "zh-CN"}), ("Hello", "en", "zh"))
        with self.assertRaises(ValueError):
            worker.validate({"text": "Hello", "source": "../en", "target": "zh"})

    def test_length_bound(self):
        with self.assertRaises(ValueError):
            worker.validate({"text": "a" * 10001, "source": "en", "target": "zh"})

    def test_chunk_round_trip(self):
        text = " first\r\n\r\n第二行\n😀\r last "
        self.assertEqual("".join(worker.text_chunks(text, len, 12)), text)

    def test_long_sentence_is_not_truncated(self):
        text = "abcd中文😀" * 200
        chunks = list(worker.text_chunks(text, len, 240))
        self.assertEqual("".join(chunks), text)
        self.assertTrue(all(len(c) <= 240 for c in chunks))

    def test_same_language_is_exact(self):
        text = "  Hello\n\n世界 😀  "
        self.assertEqual(worker.translate({"text": text, "source": "en", "target": "en"}), text)

    def test_missing_model_is_explicit(self):
        with patch.object(worker, "model_path", return_value=REPO / "nonexistent-test-model.gguf"):
            with self.assertRaisesRegex(ValueError, "缺少增强离线模型"):
                worker.translate({"text": "Hello", "source": "en", "target": "zh"})

    def test_warning_context_and_literal_exclusions(self):
        self.assertIn("hidden risks", worker.clarify_warning("Proceed with caution: here be dragons!"))
        self.assertIn("hidden risks", worker.clarify_warning("// Here be dragons."))
        for literal in ("The map says HERE BE DRAGONS.", "The dragon in this story is dangerous.", "// The map says here be dragons.", "Careful with `here be dragons` as a string.", 'const warning = "here be dragons";'):
            self.assertEqual(worker.clarify_warning(literal), literal)

    def test_manifest_matches_lock(self):
        self.assertEqual(json.loads((REPO / "model.json").read_text(encoding="utf-8")), json.loads((REPO / "dependencies.lock.json").read_text(encoding="utf-8"))["model"])


@unittest.skipUnless(os.environ.get("TRANSLATORPET_TEST_RELEASE"), "Set TRANSLATORPET_TEST_RELEASE to test installed models")
class OfflineIntegrationTests(unittest.TestCase):
    def invoke(self, text, source, target, bom=False, worker_path=None):
        release = Path(os.environ["TRANSLATORPET_TEST_RELEASE"])
        env = dict(os.environ, TRANSLATORPET_DISABLE_NETWORK="1")
        request = ("\ufeff" if bom else "") + json.dumps({"text": text, "source": source, "target": target})
        result = subprocess.run([str(release / "runtime" / "python.exe"), "-I", "-X", "utf8", str(worker_path or release / "engine" / "worker.py")], input=request, encoding="utf-8", capture_output=True, timeout=180, env=env)
        events = [json.loads(line) for line in result.stdout.splitlines()]
        self.assertEqual(result.returncode, 0, result.stderr + result.stdout)
        return next(e["text"] for e in events if e["type"] == "result")

    def test_english_to_chinese_without_network(self):
        result = self.invoke("This project is open source.", "en", "zh-CN")
        print("EN -> ZH:", result)
        self.assertIn("开源", result)

    def test_chinese_to_english_without_network(self):
        result = self.invoke("请阅读安装说明。", "zh-CN", "en")
        print("ZH -> EN:", result)
        self.assertIn("install", result.lower())
        self.assertNotIn("\u2581", result)

    def test_preserves_blank_lines(self):
        result = self.invoke("Hello world.\n\nThis is a test.", "en", "zh-CN")
        self.assertIn("\n\n", result)
        self.assertTrue(all(piece.strip() for piece in result.split("\n\n")))

    def test_utf8_bom_protocol(self):
        self.assertEqual(self.invoke("Unicode 世界", "en", "en", bom=True), "Unicode 世界")

    def test_warning_and_technical_terms(self):
        result = self.invoke("PROCEED WITH CAUTION: HERE BE DRAGONS!", "en", "zh-CN")
        self.assertRegex(result, "谨慎|小心|注意")
        self.assertRegex(result, "风险|危险|隐患|陷阱|坑")
        self.assertNotRegex(result.lower(), "dragon|caution|龙")

    def test_identifiers_and_negation(self):
        result = self.invoke("Do not retry unless the status is HTTP 503. Keep `MAX_RETRIES` at 3.", "en", "zh-CN")
        for value in ("HTTP 503", "`MAX_RETRIES`", "3"):
            self.assertIn(value, result)
        self.assertRegex(result, "除非|只有|仅当")

    def test_literal_dragon_is_preserved(self):
        result = self.invoke("The dragon in the story is not dangerous.", "en", "zh-CN")
        self.assertIn("龙", result)
        self.assertRegex(result, "不|没有|无")

    def test_contextual_code_comment(self):
        result = self.invoke("// Nested callbacks may trigger more events.\n// Here be dragons: proceed with care.", "en", "zh-CN")
        self.assertIn("回调", result)
        self.assertEqual(sum(line.lstrip().startswith("//") for line in result.splitlines()), 2)
        self.assertNotIn("龙", result)

    def test_models_in_unicode_directory(self):
        release = Path(os.environ["TRANSLATORPET_TEST_RELEASE"])
        scratch = REPO / "artifacts" / "path-tests"
        scratch.mkdir(parents=True, exist_ok=True)
        with tempfile.TemporaryDirectory(prefix="TranslatorPet-", dir=scratch) as folder:
            relocated = Path(folder) / "中文 路径 😀"
            shutil.copytree(release / "engine", relocated / "engine")
            shutil.copytree(release / "models", relocated / "models")
            shutil.copy2(release / "model.json", relocated / "model.json")
            worker_path = relocated / "engine" / "worker.py"
            self.assertIn("开源", self.invoke("This project is open source.", "en", "zh-CN", worker_path=worker_path))
            self.assertIn("install", self.invoke("请阅读安装说明。", "zh-CN", "en", worker_path=worker_path).lower())


if __name__ == "__main__":
    unittest.main()
