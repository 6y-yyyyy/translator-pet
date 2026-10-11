# 随身翻译 · TranslatorPet

一个小巧的 Windows 悬浮翻译助手。平时只显示圆球，点击后展开翻译窗口。**中英翻译在本机运行，没有在线服务的每日额度，也不上传原文。**

[English](README.en.md) · [贡献指南](CONTRIBUTING.md) · [第三方许可](THIRD_PARTY_NOTICES.md)

当前版本：**v0.2.0，早期可用版本**。面向 Windows 10/11 x64，先支持英语和简体中文双向翻译。

![翻译窗口](docs/translation-window.png)

源码：[6y-yyyyy/translator-pet](https://github.com/6y-yyyyy/translator-pet)。欢迎提交问题和改进。

## 使用便携版

1. 下载完整的 Windows x64 离线包并解压到可写目录。
2. 双击 `TranslatorPet.exe`。不要单独移动 exe，运行环境和模型也需要保留。
3. 在网页、文章或 GitHub 复制文字，点击悬浮球 → 粘贴 → 选择上下两个框的语言 → 翻译。

普通用户不需要安装 Python、不需要注册账号或填写 API 密钥。离线包包含模型，解压后翻译无需联网；只有从源码准备依赖时需要下载。

需要 .NET Framework 4.8，以及 Microsoft Visual C++ 2015–2022 x64 运行库。Windows 10/11 通常已有 .NET；如果本地引擎提示缺少运行库，可从 [Microsoft 官方页面](https://learn.microsoft.com/en-us/cpp/windows/latest-supported-vc-redist) 安装 x64 版本。无需显卡，使用 CPU。

### 操作

- 点击圆球：展开 / 收起窗口；按住拖动：移动圆球。
- 上框选择原文语言，下框选择译文语言；⇅ 交换方向。
- Ctrl+Enter：翻译；Esc 或 ×：收起；「复制译文」复制结果。
- 右键圆球：调整 40 / 48 / 64 像素大小，切换形象或退出。
- 形象：默认圆球，也支持简约猫、机器人和自定义静态 PNG / JPG / BMP / GIF（GIF 单帧）。
- 系统托盘：重新打开窗口、显示 / 隐藏圆球、重置位置和退出。

本版没有划词翻译、OCR、自动语言识别、动态皮肤或开机启动。语言下拉框只列出本版支持的中英两种语言。

## 翻译质量与资源

使用 Argos 打包的 OPUS-MT 轻量模型，并通过 CTranslate2 在 CPU 上运行。它适合日常阅读辅助，但复杂长句、专业术语和代码解释的效果可能弱于大型在线模型。翻译不是模型自行训练完成的，模型出处与许可证均列在第三方说明里。

没有按天、按月或累计次数额度。单次保留 10,000 字上限，避免界面和内存压力；长文本会按句子和模型 token 上限拆分，不静默截断。换行和段落缩进会尽量保留。分段可能损失上下文。

模型只在翻译时加载，工作进程在完成或取消后退出，闲置时释放模型占用。安装包与运行内存的实际大小见发布说明；不同 CPU 的速度会不同。

## 隐私与设置

界面与翻译进程通过标准输入 / 输出交换文字，不开放 HTTP 端口，不发送远程翻译请求，不做联网降级，也不保存翻译历史。剪贴板只在手动点击粘贴或按 Ctrl+V 时读取。

便携版的 `portable.marker` 使偏好和自定义图片保存在旁边的 `data/` 文件夹。没有该标记时使用当前 Windows 用户的 LocalAppData/TranslatorPet 目录。源码包和发布压缩包不应包含用户的 `data/`。

## 从源码构建

在 Windows PowerShell 中，进入项目根目录运行：

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\build.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\prepare-runtime.ps1
.\dist\TranslatorPet\TranslatorPet.exe
```

不需要安装 Python 或 .NET SDK。构建脚本使用 .NET Framework 的 C# 编译器；模型准备脚本使用已校验的 Python 嵌入式运行环境。所有资源版本、下载地址和 SHA-256 固定在 `dependencies.lock.json`，缓存位于 `.cache/`。首次准备会下载约 190 MB 的压缩资源，解压后占用更多磁盘空间。

也可以在 Visual Studio / MSBuild 中打开 `src/TranslatorPet/TranslatorPet.csproj` 构建界面；仍需准备模型和 Python 运行环境。模型不加入 Git 仓库。

## 验证与打包

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\test.ps1 -WithModels
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\package.ps1
```

测试包含输入校验、Unicode、长句分段、段落保留、真实离线中英互译、C# 与 Python 通信、取消操作，以及窗口和语言按钮交互。集成测试会阻止 Python 网络连接；它不等于对所有原生库的网络审计。测试例句仅用于功能回归，不构成翻译质量基准。

CI 在 Windows 上编译并检查界面。`workflow_dispatch` 的离线打包流程会准备模型、运行完整测试并生成 artifact；工作流本身不会自动发布 GitHub Release。

## 项目结构

```text
src/TranslatorPet/     WPF 桌宠、窗口、设置与本地进程通信
engine/worker.py       本地推理与文本分段
tools/                构建、准备运行环境、验证、打包
tests/                单元测试、真实模型测试和 C# 通信测试
licenses/             模型许可证全文
dependencies.lock.json 固定资源、镜像与校验值
```

新增翻译方向需要显式增加模型许可、下载锁定记录、引擎语言映射和下拉项，并加入真实翻译测试。界面与引擎使用 JSONL 协议，可在保持协议的前提下替换模型后端。

## 许可与路线

项目自己的代码采用 MIT 许可；模型采用 CC BY 4.0，Python 和依赖各自保留原许可。分发时保留 `THIRD_PARTY_NOTICES.md`、`licenses/`、模型 README、Python 许可和依赖 `.dist-info`。

后续方向：语言包管理、模型质量比较、可选更强本地模型、动态皮肤、多屏和高 DPI 优化。当前尚未进行广泛的不同电脑兼容性测试，没有代码签名、自动更新或安装程序。
