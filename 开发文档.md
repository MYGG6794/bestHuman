# bestHuman
数字人助手软件开发需求清单 (更新版)
项目概述： 本项目旨在开发一款桌面端数字人助手软件，作为UE端数字人实时渲染的前置窗口和控制中心。软件需具备像素流接收、透明化显示、语音交互、自定义讲解、本地AI问答等核心功能，并通过模块化设计，确保高内聚、低耦合，便于迭代与维护。
模块划分与详细需求
我们将整个软件划分为以下核心模块：
核心应用框架 (Core Application Framework)
数字人显示模块 (Digital Human Display Module)
后台管理界面模块 (Backend Management UI Module)
语音服务模块 (Speech Services Module)
讲解词与动作脚本模块 (Scripting & Action Script Module)
本地AI与知识库模块 (Local AI & Knowledge Base Module)
网络通信模块 (Network Communication Module)
通用工具与服务模块 (Utilities & Common Services Module)
1. 核心应用框架 (Core Application Framework)
模块功能： 负责整个应用程序的生命周期管理、主窗口创建与初始化、多线程调度、以及各模块间的消息传递与事件处理。
核心功能点：
应用程序启动与关闭逻辑。
主事件循环管理。
线程管理与同步机制（例如：UI线程与后台处理线程分离）。
进程间通信基础（如果需要）。
系统托盘图标及菜单（可选）。
技术选型建议：
主框架语言： C++ (高性能) 或 C# (快速开发，生态完善)。考虑到与Windows API的深度交互和潜在的像素流处理性能要求，C++可能更具优势，但C# (WPF/WinForms) 在UI开发上更便捷。建议使用C#作为主开发语言，对于性能瓶颈的部分可以考虑C++/CLI或直接调用C++库。
跨平台框架（可选）： Qt (C++), Electron (如果UI部分需要Web技术栈) - 但用户需求明确是Windows应用，故优先考虑原生Windows桌面框架。
对外接口： 提供统一的模块注册、事件发布/订阅机制。
与其他模块的依赖： 所有模块都将依赖于核心框架来获取运行上下文和进行通信。
需要注意的点：
确保应用程序启动速度。
合理分配UI线程与工作线程，避免UI卡顿。
考虑进程的稳定性与异常处理。
中文注释与文档要求：
提供README.md文件，说明项目结构、编译运行方式。
核心启动文件、主消息循环、线程管理类等需详细中文注释。
模块间通信协议的文档说明。
2. 数字人显示模块 (Digital Human Display Module)
模块功能： 接收并渲染UE端推送的数字人像素流，实现抠像透明底效果，并管理窗口的置顶、大小和点击穿透。
核心功能点：
像素流接收： 支持至少一种主流的实时像素流协议（如WebRTC、自定义TCP/UDP协议，或UE的Pixel Streaming插件协议）。
实时渲染： 将接收到的像素数据实时渲染到窗口上。
抠像处理：
支持自定义抠像背景颜色（RGB或RGBA）。
支持颜色容差设置，以优化抠像效果。
实现基于颜色键（Chroma Key）的透明化处理。
窗口管理：
窗口置顶： 确保数字人窗口始终显示在其他应用之上。
点击穿透： 允许用户点击数字人窗口区域时，操作其下方的应用程序，而不响应数字人窗口。
窗口大小与位置调整： 可通过后端管理界面控制。
无边框窗口： 实现自定义窗口外观。
性能优化： 确保高帧率渲染，降低CPU/GPU占用。
技术选型建议：
渲染技术： DirectX (如Direct2D/Direct3D) 或 OpenGL/Vulkan，用于高效的像素渲染和Shader编写（实现抠像）。
流媒体协议： UE Pixel Streaming官方库（如果适用），或自定义TCP/UDP协议解析。WebRTC是一个复杂但功能强大的选择。
编程语言： C++ (DirectX/OpenGL原生API) 或 C# (通过P/Invoke调用Windows API，或使用SharpDX/OpenTK等Wrapper)。
对外接口：
SetStreamAddress(string address): 设置像素流接收地址。
SetChromaKeyColor(Color color, float tolerance): 设置抠像颜色与容差。
SetWindowSize(int width, int height): 设置窗口尺寸。
SetWindowPosition(int x, int y): 设置窗口位置。
SetClickThrough(bool enable): 启用/禁用点击穿透。
StartStream(): 开始接收并渲染像素流。
StopStream(): 停止像素流。
事件：OnStreamError(string errorMessage), OnStreamConnected(), OnStreamDisconnected().
与其他模块的依赖：
依赖 后台管理界面模块 获取配置信息。
依赖 网络通信模块 进行数据传输。
需要注意的点：
像素流的同步与延迟控制。
不同分辨率下的性能表现。
抠像算法的效率与效果。
DPI缩放兼容性。
中文注释与文档要求：
渲染管线、抠像算法实现、窗口API调用等核心代码需详细中文注释。
说明支持的像素流协议格式与解析方法。
解释如何设置窗口置顶和点击穿透。
3. 后台管理界面模块 (Backend Management UI Module)
模块功能： 提供一个易于操作的图形用户界面，用于配置和管理软件的各项功能参数，并支持快捷键呼出/隐藏。
核心功能点：
界面设计： 简洁、直观，方便用户操作。
快捷键管理：
设置并监听自定义快捷键，用于显示/隐藏管理界面。
快捷键可配置。
配置项管理：
数字人推流地址（URL/IP:Port）。
自定义抠像颜色选择器（支持RGB值输入、拾色器）。
抠像颜色容差设置。
数字人窗口尺寸（宽度、高度）。
数字人窗口位置（X、Y坐标）。
与UE端的通信状态显示。
语音识别/合成设置（音色选择、语速、音量）。
本地AI模型路径、知识库路径配置。
讲解词/动作脚本的导入、编辑、保存入口。
点击穿透 开关。
窗口置顶 开关。
配置持久化： 自动保存和加载所有配置到本地文件（如JSON, XML或INI）。
状态显示： 显示数字人连接状态、语音服务状态、AI模型加载状态等。
技术选型建议：
UI框架：
C#/.NET： WPF (推荐，功能强大，支持MVVM模式，易于构建复杂UI) 或 WinForms (简单快速，但功能相对较弱)。
C++： Qt (功能强大，但学习曲线较陡峭) 或 MFC (老旧，不推荐新项目)。
对外接口：
ShowManagerUI(): 显示管理界面。
HideManagerUI(): 隐藏管理界面。
事件：OnSettingsChanged(Settings settings): 当配置发生改变时触发，通知其他模块更新。
与其他模块的依赖：
向 数字人显示模块 发送窗口、抠像、流地址设置。
向 语音服务模块 发送语音识别/合成设置。
向 本地AI与知识库模块 发送AI配置。
向 网络通信模块 发送WebSocket连接配置。
依赖 通用工具与服务模块 进行配置的读写。
需要注意的点：
界面的响应速度与用户体验。
配置文件的安全性（如果包含敏感信息）。
多语言支持（可选）。
中文注释与文档要求：
UI布局、控件事件处理、快捷键监听逻辑需详细中文注释。
配置文件结构与读写逻辑的说明。
用户手册中详细说明各配置项的功能和使用方法。
4. 语音服务模块 (Speech Services Module)
模块功能： 集成Windows系统内置的语音识别（ASR）和语音合成（TTS）功能，并通过WebSocket接口提供给UE端调用。
核心功能点：
语音识别 (ASR)：
调用Windows Speech Recognition API。
支持选择系统已安装的识别语言模型。
实时将识别结果（文本）通过WebSocket发送给UE端。
提供识别开始/停止控制。
语音合成 (TTS)：
调用Windows Speech Synthesis API (SAPI)。
支持选择系统已安装的音色（Voice）。
支持调节语速（Rate）和音量（Volume）。
接收UE端通过WebSocket推送的文本，并进行合成播放。
合成播放状态回调（例如：播放开始、播放结束）。
WebSocket接口：
定义ASR文本推送协议。
定义TTS文本接收协议。
定义TTS播放状态（如：TTS_START, TTS_END）通知协议。
技术选型建议：
C#/.NET： System.Speech 命名空间提供了对Windows ASR/TTS的良好封装。
C++： COM接口调用SAPI API。
对外接口：
StartSpeechRecognition(): 启动语音识别。
StopSpeechRecognition(): 停止语音识别。
SynthesizeSpeech(string text, string voiceName, int rate, int volume): 合成语音并播放。
GetAvailableVoices(): 获取系统可用音色列表。
GetAvailableRecognitionLanguages(): 获取系统可用识别语言。
事件：OnSpeechRecognized(string text) (发送给网络通信模块), OnSpeechSynthesisStarted(), OnSpeechSynthesisEnded().
与其他模块的依赖：
依赖 后台管理界面模块 获取ASR/TTS配置。
依赖 网络通信模块 进行WebSocket数据传输。
需要注意的点：
确保ASR的准确性和响应速度。
TTS播放的流畅性。
错误处理，如识别失败、音色不存在等。
多线程操作SAPI的注意事项。
中文注释与文档要求：
ASR和TTS的初始化、事件处理、参数设置等需详细中文注释。
说明WebSocket语音服务协议的定义（JSON格式示例）。
5. 讲解词与动作脚本模块 (Scripting & Action Script Module)
模块功能： 允许用户自定义讲解词内容和数字人对应的动作序列，并能通过WebSocket接口驱动UE端数字人执行。
核心功能点：
脚本管理：
创建、编辑、保存、加载自定义讲解词和动作脚本。
支持文本输入框编辑讲解词。
支持结构化编辑动作（例如：动作ID、持续时间、参数等）。
脚本格式： 定义清晰的脚本文件格式，推荐使用JSON或YAML，包含：
id: 脚本唯一标识。
title: 脚本标题。
segments: 讲解段落数组。
每个段落包含 text: 讲解文本。
actions: 对应文本的动作列表（如：{"actionId": "idle", "duration": 5.0}）。
speechRate, speechVolume, voiceName: 段落级别的语音参数（可选，覆盖全局设置）。
播放控制：
播放、暂停、停止、循环播放脚本。
当前播放进度显示。
同步逻辑： 确保语音合成与数字人动作的同步播放。
WebSocket接口：
定义脚本播放控制指令（如：PLAY_SCRIPT, PAUSE_SCRIPT, STOP_SCRIPT, LOAD_SCRIPT）。
定义动作触发指令（如：TRIGGER_ACTION, PLAY_ANIMATION）。
定义讲解文本推送指令（供UE端TTS合成或驱动唇形）。
技术选型建议：
数据序列化： JSON.NET (C#) 或 RapidJSON/nlohmann/json (C++)。
UI编辑： 文本框、列表视图、属性网格等UI控件。
对外接口：
LoadScript(string filePath): 加载脚本文件。
SaveScript(string filePath): 保存当前编辑脚本。
PlayScript(string scriptId): 播放指定脚本。
PauseScript(): 暂停当前脚本。
StopScript(): 停止当前脚本。
SeekScript(float time): 跳转到脚本某时间点。
事件：OnScriptPlayProgress(float progress), OnScriptFinished().
与其他模块的依赖：
依赖 后台管理界面模块 提供脚本编辑UI。
依赖 语音服务模块 进行讲解词的语音合成。
依赖 网络通信模块 将播放指令和动作指令发送给UE。
需要注意的点：
脚本与动作同步的精确性。
复杂的脚本逻辑（如条件判断、循环）可以考虑后续扩展。
用户友好的脚本编辑界面。
中文注释与文档要求：
脚本数据结构定义、解析与序列化代码需详细中文注释。
播放控制、同步逻辑的代码注释。
提供脚本文件的示例和编写规范。
说明WebSocket脚本控制协议的定义。
6. 本地AI与知识库模块 (Local AI & Knowledge Base Module)
模块功能： 内嵌 DeepSeek Distillation 7B 模型，实现语义理解和输出，并支持配置知识库，通过接口供UE端调用。
核心功能点：
AI模型集成：
加载 DeepSeek Distillation 7B 模型。
实现用户输入（文本）到模型输入的预处理（如分词、Tokenization）。
模型推理，获取输出（文本）。
考虑模型量化（如INT8、FP16）以优化本地推理性能和资源占用。
知识库管理：
支持多种知识库格式（如：简单的Q&A对、结构化文本文件、Markdown文档）。
提供知识库的导入、更新功能。
实现基于知识库的检索增强生成 (RAG) 机制，即在AI模型回答前，先从知识库中检索相关信息，并将检索结果作为上下文输入给 DeepSeek Distillation 7B 模型，以生成更精准的回复。
语义理解与生成：
根据用户输入，结合知识库检索到的上下文，由 DeepSeek Distillation 7B 模型生成语义连贯、准确的回复。
提供可选的机制，允许在本地模型回答不满意时，将复杂查询转发到云端API（如OpenAI, 百度文心一言等），作为增强功能（此功能需在管理界面中配置开关）。
WebSocket接口：
定义用户问题发送协议。
定义AI回答接收协议。
技术选型建议：
AI模型框架/推理引擎：
ONNX Runtime (推荐)： 如果DeepSeek Distillation 7B模型能转换为ONNX格式，ONNX Runtime是跨平台且高效的推理引擎，支持CPU和GPU推理。
llama.cpp 或 ggml-based 库： 如果模型以GGUF或其他ggml兼容格式提供，可以使用相关的C++库进行高效CPU推理。
Hugging Face Transformers (Python)： 如果需要更灵活的模型加载和预处理，可以考虑使用Python子进程或通过gRPC/HTTP服务与主C#/C++应用进行通信，但需注意部署复杂度和资源占用。
知识库： SQLite (小型嵌入式数据库) 或简单的文本文件（如JSON、CSV、TXT）进行管理。对于RAG，可能需要引入向量数据库（如Faiss、ChromaDB等）或简单的文本检索方案（如基于倒排索引或BM25）。
对外接口：
LoadAIModel(string modelPath): 加载 DeepSeek Distillation 7B 模型。
LoadKnowledgeBase(string kbPath): 加载知识库。
AskAI(string question): 向AI提问。
UpdateKnowledgeBase(string newKnowledge): 动态更新知识库（可选）。
事件：OnAIResponse(string responseText), OnAIError(string errorMessage).
与其他模块的依赖：
依赖 后台管理界面模块 进行AI模型和知识库的路径配置。
依赖 网络通信模块 进行与UE端的问答数据传输。
需要注意的点：
DeepSeek Distillation 7B 模型的部署方式（CPU/GPU推理），确保用户设备具备足够的硬件资源。
模型的量化和优化，以在本地设备上获得最佳性能。
知识库的更新与管理机制，以及RAG检索效率。
模型推理的性能与延迟，尤其是在问答交互中需要快速响应。
确保模型和知识库文件路径的正确性。
中文注释与文档要求：
DeepSeek Distillation 7B 模型的加载、推理过程、知识库索引与RAG检索逻辑需详细中文注释。
说明AI模型（如ONNX/GGUF）和知识库文件的预期格式。
说明WebSocket AI交互协议的定义。
解释如何配置本地推理环境（如CUDA/ONNX Runtime安装指引）。
7. 网络通信模块 (Network Communication Module)
模块功能： 统一管理软件与UE端之间的所有WebSocket通信，包括连接管理、消息发送与接收、错误处理和协议解析。
核心功能点：
WebSocket客户端/服务器：
可配置作为WebSocket客户端连接UE端（如果UE是服务器）。
或作为WebSocket服务器接收UE端连接（如果UE是客户端）。
推荐以客户端模式连接UE的Pixel Streaming WebSocket。
连接管理： 连接、断开、重连机制。
消息发送： 封装各种需要发送给UE的数据（ASR文本、TTS状态、讲解词指令、AI回答等）。
消息接收： 接收来自UE的数据（TTS文本、用户操作、AI请求等）。
协议解析与封装：
定义统一的JSON格式消息协议，包含消息类型、数据内容等。
负责将内部数据结构序列化为JSON发送，将接收到的JSON反序列化为内部数据结构。
错误处理与日志： 连接失败、消息发送失败等。
技术选型建议：
C#/.NET： System.Net.WebSockets 或第三方库如 websocket-sharp, Fleck。
C++： Boost.Beast, libwebsockets 等。
对外接口：
Connect(string uri): 连接到指定WebSocket地址。
Disconnect(): 断开连接。
SendMessage(string messageType, object data): 发送结构化消息。
IsConnected(): 获取连接状态。
事件：OnConnected(), OnDisconnected(), OnMessageReceived(string messageType, string jsonData), OnError(string errorMessage).
与其他模块的依赖：
被 数字人显示模块 调用发送流控制指令。
被 语音服务模块 调用发送ASR文本、接收TTS文本。
被 讲解词与动作脚本模块 调用发送脚本控制指令。
被 本地AI与知识库模块 调用发送AI回答、接收AI请求。
依赖 后台管理界面模块 获取连接地址。
依赖 通用工具与服务模块 进行日志记录。
需要注意的点：
WebSocket协议的稳定性与可靠性。
消息队列管理，避免消息堵塞。
不同消息类型的数据结构定义。
网络抖动和断线重连的处理。
中文注释与文档要求：
WebSocket连接、断开、消息发送/接收循环、错误处理等核心代码需详细中文注释。
详细说明与UE端约定的所有WebSocket消息协议（包括JSON结构、消息类型、字段含义）。
8. 通用工具与服务模块 (Utilities & Common Services Module)
模块功能： 提供应用程序通用的辅助功能，例如日志记录、配置文件管理、数据校验、时间处理等。
核心功能点：
日志系统：
支持不同级别的日志输出（Debug, Info, Warning, Error）。
日志文件写入，可按日期或大小分割。
控制台输出、文件输出。
配置管理：
统一的配置读取、写入接口。
支持多种格式（JSON, XML, INI）。
数据校验：
输入参数的合法性校验。
辅助函数：
例如字符串处理、文件路径操作、时间戳生成等。
技术选型建议：
日志： NLog, Serilog (C#) 或 log4cpp, spdlog (C++)。
配置： 内置System.Configuration (C#), 或手动读写JSON/XML文件。
对外接口：
Logger.Debug(string message), Logger.Info(...) 等。
ConfigManager.GetValue<T>(string key), ConfigManager.SetValue(string key, object value)。
与其他模块的依赖： 被所有模块调用，提供通用服务。
需要注意的点：
日志的性能开销。
配置文件的安全性与并发访问。
中文注释与文档要求：
日志系统、配置管理、公共辅助函数的实现代码需详细中文注释。
说明日志级别、配置文件格式等。
总结与后续建议
分阶段实现： 建议按照模块的依赖关系和重要性进行分阶段实现。例如：
第一阶段： 核心应用框架、数字人显示模块（基础像素流接收与透明化）、后台管理界面模块（基础配置管理）。
第二阶段： 网络通信模块（基础WebSocket连接）、语音服务模块。
第三阶段： 讲解词与动作脚本模块。
第四阶段： 本地AI与知识库模块（侧重 DeepSeek Distillation 7B 的本地部署和RAG集成）。
技术验证： 在正式开发前，对关键技术点（如UE Pixel Streaming协议解析、Windows ASR/TTS调用、DeepSeek Distillation 7B 模型加载与推理）进行技术预研和POC (概念验证)。特别注意DeepSeek Distillation 7B的最佳部署方式（如ONNX转换、量化、CPU/GPU推理配置）。
接口先行： 在每个模块开始编码前，先定义好模块对外的接口和与其他模块的交互协议，这样可以并行开发。
持续集成/测试： 尽早引入单元测试和集成测试，确保每个模块的功能正确性和模块间的协同工作。
性能考量： 尤其是在像素流处理和AI推理部分，需要持续关注性能，进行优化。
