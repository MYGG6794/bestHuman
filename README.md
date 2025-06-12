# bestHuman
数字人助手软件开发项目

## 项目概述
本项目旨在开发一款桌面端数字人助手软件，作为UE端数字人实时渲染的前置窗口和控制中心。软件需具备像素流接收、透明化显示、语音交互、自定义讲解、本地AI问答等核心功能，并通过模块化设计，确保高内聚、低耦合，便于迭代与维护。

## 模块划分
整个软件划分为以下核心模块：

1.  **核心应用框架 (Core Application Framework)**
    *   负责整个应用程序的生命周期管理、主窗口创建与初始化、多线程调度、以及各模块间的消息传递与事件处理。
2.  **数字人显示模块 (Digital Human Display Module)**
    *   接收并渲染UE端推送的数字人像素流，实现抠像透明底效果，并管理窗口的置顶、大小和点击穿透。
3.  **后台管理界面模块 (Backend Management UI Module)**
    *   提供一个易于操作的图形用户界面，用于配置和管理软件的各项功能参数，并支持快捷键呼出/隐藏。
4.  **语音服务模块 (Speech Services Module)**
    *   集成Windows系统内置的语音识别（ASR）和语音合成（TTS）功能，并通过WebSocket接口提供给UE端调用。
5.  **讲解词与动作脚本模块 (Scripting & Action Script Module)**
    *   允许用户自定义讲解词内容和数字人对应的动作序列，并能通过WebSocket接口驱动UE端数字人执行。
6.  **本地AI与知识库模块 (Local AI & Knowledge Base Module)**
    *   内嵌 DeepSeek Distillation 7B 模型，实现语义理解和输出，并支持配置知识库，通过接口供UE端调用。
7.  **网络通信模块 (Network Communication Module)**
    *   统一管理软件与UE端之间的所有WebSocket通信，包括连接管理、消息发送与接收、错误处理和协议解析。
8.  **通用工具与服务模块 (Utilities & Common Services Module)**
    *   提供应用程序通用的辅助功能，例如日志记录、配置文件管理、数据校验、时间处理等。

## 开发阶段
### 第一阶段
*   核心应用框架 (Core Application Framework)
*   数字人显示模块 (Digital Human Display Module)
*   后台管理界面模块 (Backend Management UI Module)
    *   已完成：
        *   创建 `bestHuman` 解决方案和 `CoreApplication` 项目。
        *   配置 `CoreApplication.csproj` 为 Windows Forms 应用程序。
        *   创建 `Program.cs` 中的 `MainForm` 作为主窗口。
*   数字人显示模块 (Digital Human Display Module)
    *   已完成：
        *   创建 `DigitalHumanDisplay.cs`，继承自 `PictureBox`，并实现基本的抠像逻辑。
        *   在 `MainForm` 中集成 `DigitalHumanDisplay` 控件。
        *   通过 P/Invoke 实现了主窗口的无边框、置顶和点击穿透功能。
*   后台管理界面模块 (Backend Management UI Module)
    *   已完成：
        *   创建 `SettingsForm.cs` 作为后台管理界面，包含基本的UI元素和设置保存事件。
        *   在 `MainForm` 中实例化 `SettingsForm`。
        *   在 `MainForm` 中实现了快捷键 (Ctrl + Alt + S) 监听，用于显示/隐藏 `SettingsForm`。
        *   在 `MainForm` 中实现了 `SettingsForm` 的 `SettingsChanged` 事件处理，用于应用和保存配置。
        *   初步实现了配置的加载和保存（目前为占位逻辑，后续将集成通用工具与服务模块进行文件持久化）。
    *   **第一阶段已完成。**

### 第二阶段
*   网络通信模块 (Network Communication Module)
    *   已完成：
        *   创建 `WebSocketClient.cs`，封装 WebSocket 客户端的连接、断开、发送和接收消息逻辑。
        *   在 `SettingsForm.cs` 中添加 WebSocket 服务器地址配置项。
        *   在 `MainForm.cs` 中集成 `WebSocketClient`，实现连接管理和事件处理。
        *   在 `MainForm` 的 `FormClosing` 事件中确保 WebSocket 连接被正确关闭。
*   语音服务模块 (Speech Services Module)
    *   已完成：
        *   创建 `SpeechService.cs`，封装语音识别 (ASR) 和语音合成 (TTS) 的基本逻辑。
        *   在 `SettingsForm.cs` 中添加 TTS 音色、语速、音量配置项。
        *   在 `MainForm.cs` 中集成 `SpeechService`，处理语音识别结果和语音合成状态，并通过 `WebSocketClient` 进行数据传输。
        *   在 `MainForm` 的 `FormClosing` 事件中确保 `SpeechService` 被正确释放。
    *   **第二阶段已完成。**

### 第三阶段
* 讲解词与动作脚本模块 (Scripting & Action Script Module)
    * 已完成：
        * 创建 `ScriptService.cs`，实现脚本数据结构（`Script`、`ScriptSegment`、`DigitalHumanAction`）和核心服务逻辑。
        * 封装脚本的加载、保存、播放控制和同步逻辑。
        * 与语音服务和 WebSocket 通信模块集成，实现语音合成与动作同步。
        * 创建 `ScriptEditForm.cs`，实现脚本编辑界面，支持段落管理和动作编辑。
        * 创建 `ScriptPlayerForm.cs`，实现播放控制界面，支持播放、暂停、停止和循环播放功能。
        * 在 `SettingsForm.cs` 中添加讲解词管理和播放控制的入口按钮。
        * 优化 `Program.cs` 中的全局服务访问逻辑。
    * **第三阶段已完成。**

### 第四阶段
* 本地AI与知识库模块 (Local AI & Knowledge Base Module)
    * 已完成基础架构：
        * 创建 `ModelInference.cs`，封装模型加载、分词和推理功能。
        * 创建 `AIService.cs`，设计 AI 服务的核心结构，包含模型管理和知识库管理功能。
        * 集成 ONNX Runtime 作为推理引擎。
        * 创建 `AIManagerForm.cs`，提供 AI 配置和知识库管理界面。
        * 实现知识库的加载、保存和基本检索逻辑。
        * 实现模型信息展示（名称、词汇表大小、输入形状等）。
        * 建立项目基础结构，包括模型和知识库存储目录。
        * 实现了 RAG 的基础架构：
            * 创建 VectorSearchService 类，支持文本分块和向量检索。
            * 实现智能文本分块，支持段落完整性和句子边界。
            * 实现基于余弦相似度的向量检索。
            * 支持异步文档加载和检索操作。
    * 待完成功能：
        * DeepSeek Distillation 7B 模型的具体集成：
            * 获取并转换模型为 ONNX 格式
            * 实现分词器
            * 优化推理性能
        * 高级 RAG 机制：
            * 添加向量索引以提高检索性能
            * 支持更多知识库文件格式
            * 优化相似度计算策略
        * 云端 API 回退：
            * 实现 OpenAI API 集成
            * 实现百度文心一言 API 集成
            * 添加智能切换策略