import importlib.util
import json
import os
from pathlib import Path
import subprocess
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

    def test_paragraph_round_trip(self):
        text = " first\r\n\r\n第二行\n😀\r last "
        self.assertEqual("".join(worker.paragraphs(text)), text)

    def test_long_sentence_is_not_truncated(self):
        class Tokenizer:
            def encode(self, text, out_type):
                return list(text)
        text = "abcd中文😀" * 200
        chunks = list(worker.token_chunks(Tokenizer(), text))
        self.assertEqual("".join("".join(c) for c in chunks), text)
        self.assertTrue(all(len(c) <= worker.MAX_TOKENS for c in chunks))

    def test_same_language_is_exact(self):
        text = "  Hello\n\n世界 😀  "
        self.assertEqual(worker.translate({"text": text, "source": "en", "target": "en"}), text)

    def test_missing_model_is_explicit(self):
        with patch.object(worker, "ROOT", REPO / "nonexistent-test-installation"):
            with self.assertRaisesRegex(ValueError, "缺少离线语言包"):
                worker.translate({"text": "Hello", "source": "en", "target": "zh"})


@unittest.skipUnless(os.environ.get("TRANSLATORPET_TEST_RELEASE"), "Set TRANSLATORPET_TEST_RELEASE to test installed models")
class OfflineIntegrationTests(unittest.TestCase):
    def invoke(self, text, source, target):
        release = Path(os.environ["TRANSLATORPET_TEST_RELEASE"])
        env = dict(os.environ, TRANSLATORPET_DISABLE_NETWORK="1")
        result = subprocess.run([str(release / "runtime" / "python.exe"), "-I", "-X", "utf8", str(release / "engine" / "worker.py")], input=json.dumps({"text": text, "source": source, "target": target}), encoding="utf-8", capture_output=True, timeout=90, env=env)
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


if __name__ == "__main__":
    unittest.main()
