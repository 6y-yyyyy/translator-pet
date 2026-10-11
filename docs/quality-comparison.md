# v0.3.0 翻译样例对比

以下是开发时自行编写的 6 组回归样例，在同一台 Windows 电脑、CPU 模式下实测。每次调用都重新加载模型；新版耗时包含模型加载和进程启动。测试禁用了 Python 网络连接。它不是独立评测、总体准确率或所有电脑的性能承诺。

旧版为 OPUS-MT / CTranslate2；新版为 Qwen3-4B-Instruct-2507 Q4_K_M / llama.cpp，并包含代码警告中 “here be dragons” 的限定语境处理。该规则只改善匹配场景，不代表模型学会了所有习语。

## 样例 1

原文：

```text
HANDLE WITH CARE: HERE BE DRAGONS!
```

v0.2.1：

```text
小心点,这里是Dragons!
```

v0.3.0：

```text
小心对待：这里存在隐藏风险！
```

本机耗时：旧版 0.75 秒；新版 5.05 秒。

## 样例 2

原文：

```text
// Publishing events synchronously can be faster.
// Nested callbacks may trigger further events in the same call stack.
// Here be dragons: proceed with care.
```

v0.2.1：

```text
//同步发布事件可以更快.
// Nested callbacks可能会在同一调用堆栈中触发进一步的事件.
/这是龙: 继续小心。
```

v0.3.0：

```text
// 同步发布事件可能更快。  
// 嵌套回调可能会在同一个调用栈中触发更多事件。  
// 这里存在潜在风险：需谨慎处理。
```

本机耗时：旧版 0.78 秒；新版 7.45 秒。

## 样例 3

原文：

```text
This change is a double-edged sword: it saves memory but slows down startup.
```

v0.2.1：

```text
这个变化是一把双刃剑:它保存了记忆但慢了启动.
```

v0.3.0：

```text
这一改变是一把双刃剑：它节省了内存，但会减慢启动速度。
```

本机耗时：旧版 0.93 秒；新版 5.66 秒。

## 样例 4

原文：

```text
The API returns HTTP 404 when `user_id` does not exist.
```

v0.2.1：

```text
当“ user_ id” 不存在时, API 返回 HTTP 404 。
```

v0.3.0：

```text
当 `user_id` 不存在时，API 返回 HTTP 404。
```

本机耗时：旧版 0.61 秒；新版 5.15 秒。

## 样例 5

原文：

```text
Please keep an eye on the build; the fix is not a silver bullet.
```

v0.2.1：

```text
请注意建筑;修饰不是银弹.
```

v0.3.0：

```text
请密切关注构建过程；这个修复方案并非万能药。
```

本机耗时：旧版 0.72 秒；新版 4.77 秒。

## 样例 6

原文：

```text
同步发布事件可能更快，但嵌套回调会使执行顺序难以理解，请谨慎使用。
```

v0.2.1：

```text
Synchronization of release events may be faster, but embedding backshows may make the order of execution difficult to understand and be used with caution.
```

v0.3.0：

```text
Synchronous event publishing might be faster, but nested callbacks make it difficult to understand the execution order—use with caution.
```

本机耗时：旧版 0.74 秒；新版 5.37 秒。
