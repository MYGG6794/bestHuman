# bestHuman
数字人助手软件开发项目

## 项目概述
本项目旨在开发一款桌面端数字人助手软件，作为UE端数字人实时渲染的前置窗口和控制中心。软件需具备像素流接收、透明化显示、语音交互、自定义讲解、本地AI问答等核心功能，并通过模块化设计，确保高内聚、低耦合，便于迭代与维护。

## 核心特性
- ✅ **透明抠像显示**：支持 WebView2 接收像素流，实现绿色抠像透明背景
- ✅ **桌面穿透**：支持点击穿透，数字人可悬浮在桌面上不影响其他应用操作
- ✅ **语音交互**：集成语音识别（ASR）和语音合成（TTS）功能
- ✅ **讲解词系统**：支持自定义讲解内容，与数字人动作同步
- ✅ **本地AI**：集成本地AI模型，支持知识库问答
- ✅ **WebSocket通信**：与UE端实时数据交换
- 🔄 **原生透明窗口**：开发中，将提供更好的桌面集成和性能

## 当前运行状态
### WebView2 模式（当前可用）
- ✅ 主窗口透明抠像显示正常
- ✅ 热键 Alt+S 打开设置界面
- ✅ 语音服务正常工作
- ✅ WebSocket客户端（需要配置服务器地址）
- ✅ 讲解词播放控制
- ✅ AI管理和知识库

### 原生透明窗口模式（开发中）
- 🔄 原生Windows API透明窗口实现
- 🔄 WebView2集成到原生窗口
- 🔄 完整的桌面穿透功能

## 项目结构
```
bestHuman/
├── README.md                    # 项目文档
├── 开发文档.md                  # 详细开发文档
├── bestHuman/
│   ├── bestHuman.sln           # 解决方案文件
│   └── CoreApplication/        # 主应用程序
│       ├── Program.cs          # 程序入口点，主窗口管理
│       ├── DigitalHumanDisplay.cs   # 数字人显示控件（WebView2）
│       ├── SettingsForm.cs     # 设置界面
│       ├── WebSocketClient.cs  # WebSocket通信客户端
│       ├── SpeechService.cs    # 语音服务（ASR/TTS）
│       ├── ScriptService.cs    # 讲解词脚本服务
│       ├── ScriptEditForm.cs   # 脚本编辑界面
│       ├── ScriptPlayerForm.cs # 脚本播放界面
│       ├── AIService.cs        # AI服务管理
│       ├── AIManagerForm.cs    # AI管理界面
│       ├── ModelInference.cs   # 模型推理引擎
│       ├── VectorSearchService.cs # 向量检索服务
│       ├── NativeLayeredWindow.cs # 原生透明窗口（开发中）
│       ├── Utilities.cs        # 通用工具类
│       ├── data/              # 数据目录
│       │   └── knowledge.json # 知识库数据
│       └── models/            # AI模型目录
└── NativeWindowTest/          # 原生窗口功能测试项目
```

## 使用说明

### 系统要求
- Windows 10/11 (x64)
- .NET 9.0 Runtime
- Microsoft Edge WebView2 Runtime

### 运行程序
1. 编译项目：`dotnet build`
2. 运行程序：`dotnet run` 或直接运行 `CoreApplication.exe`
3. 使用热键 `Alt+S` 打开设置界面

### 配置文件
程序配置保存在 `appsettings.json` 中，支持以下配置：
- `StreamAddress`: 数字人像素流地址（默认：http://127.0.0.1:11188/video.html）
- `WebSocketServerAddress`: WebSocket服务器地址
- `EnableChromaKey`: 是否启用抠像功能
- `ChromaKeyColor`: 抠像颜色（默认：绿色）
- `UseNativeLayeredWindow`: 是否使用原生透明窗口（开发中）
- `ClickThroughEnabled`: 是否启用点击穿透
- `TopMostEnabled`: 是否窗口置顶

### 快捷键
- `Alt+S`: 打开/关闭设置界面
- `Alt+F10`: 备用设置快捷键（如果主快捷键冲突）

## 开发进度

### ✅ 已完成模块

#### 1. 核心应用框架 (Core Application Framework)
- ✅ 应用程序生命周期管理
- ✅ 主窗口创建与初始化
- ✅ 多线程调度和事件处理
- ✅ 全局热键系统
- ✅ 配置文件管理

#### 2. 数字人显示模块 (Digital Human Display Module)
- ✅ WebView2 集成，接收并渲染数字人像素流
- ✅ 绿色抠像透明效果实现
- ✅ 窗口置顶、无边框、点击穿透功能
- ✅ 动态透明度调整
- ✅ 窗口大小和位置配置

#### 3. 后台管理界面模块 (Backend Management UI Module)
- ✅ 设置界面（SettingsForm）
- ✅ 强大的多层快捷键系统（Alt+S全局热键）
- ✅ 配置项管理和实时应用
- ✅ 窗口状态管理和焦点控制

#### 4. 网络通信模块 (Network Communication Module)
- ✅ WebSocket 客户端实现
- ✅ 连接管理、消息发送与接收
- ✅ 错误处理和重连机制
- ✅ 与UE端数据交换协议

#### 5. 语音服务模块 (Speech Services Module)
- ✅ Windows 语音识别（ASR）集成
- ✅ Windows 语音合成（TTS）集成
- ✅ 语音参数配置（音色、语速、音量）
- ✅ WebSocket 语音数据传输

#### 6. 讲解词与动作脚本模块 (Scripting & Action Script Module)
- ✅ 脚本数据结构设计
- ✅ 脚本编辑界面
- ✅ 播放控制界面
- ✅ 语音合成与数字人动作同步
- ✅ 脚本文件管理

#### 7. 本地AI与知识库模块 (Local AI & Knowledge Base Module)
- ✅ ONNX Runtime 集成
- ✅ AI 服务架构
- ✅ 知识库管理界面
- ✅ 向量检索服务（RAG基础）
- ✅ 文档智能分块和检索

#### 8. 通用工具与服务模块 (Utilities & Common Services Module)
- ✅ 日志记录系统
- ✅ 配置文件管理
- ✅ 数据校验和时间处理
- ✅ 异常处理机制

### 🔄 开发中模块

#### 原生透明窗口增强 (Native Layered Window Enhancement)
- 🔄 原生 Windows API 透明窗口实现
- 🔄 真正的桌面穿透（无WebView2限制）
- 🔄 WebView2 与原生窗口集成
- 🔄 性能优化和资源管理

### 📋 待开发功能
- 高级 RAG 机制和向量索引优化
- DeepSeek 7B 模型完整集成
- 更多数字人动作类型支持
- 插件系统架构

## 技术特性

### 抠像透明显示系统
- **WebView2 集成**：使用微软 WebView2 控件接收数字人像素流
- **高效抠像算法**：基于 CSS 滤镜和 JavaScript 的高性能抠像方案
- **动态配置**：支持抠像颜色、容差等参数的实时调整
- **性能优化**：默认禁用抠像以保证30fps流畅播放
- **透明效果**：实现真正的透明背景显示

### 多层快捷键系统
- **全局热键**：Alt+S 和 Alt+F10 两个全局快捷键
- **多重检测机制**：支持本地按键检测和全局热键双重保障
- **焦点管理**：自动检测和恢复窗口焦点，确保快捷键可靠性
- **异常恢复**：设置保存后自动重置快捷键状态

### 窗口管理系统
- **原生透明窗口**：开发中的原生 Windows API 透明窗口支持
- **点击穿透**：支持鼠标点击穿透到桌面和其他应用
- **置顶显示**：始终保持在最顶层显示
- **多窗口协调**：主窗口、设置、编辑器等多窗口的协调管理

## 开发环境
- **.NET 9.0**: 最新的 .NET 框架支持
- **Windows Forms**: 原生 Windows 桌面应用开发
- **WebView2**: 微软官方的现代 Web 控件
- **ONNX Runtime**: 跨平台 AI 模型推理引擎
- **System.Speech**: Windows 内置语音服务

## 贡献指南
本项目正在积极开发中，欢迎贡献代码和建议：

1. Fork 本项目
2. 创建特性分支 (`git checkout -b feature/AmazingFeature`)
3. 提交更改 (`git commit -m 'Add some AmazingFeature'`)
4. 推送到分支 (`git push origin feature/AmazingFeature`)
5. 开启 Pull Request

## 许可证
本项目采用 MIT 许可证 - 查看 [LICENSE](LICENSE) 文件了解详情

## 联系方式
如有问题或建议，请通过 Issue 系统联系我们。

---
*最后更新时间：2025年6月17日*
