# bestHuman
数字人助手软件开发项目

## 项目概述
本项目是一款桌面端数字人助手软件，作为UE端数字人实时渲染的前置窗口和控制中心。具备透明抠像显示、语音交互、脚本播放、本地AI问答等核心功能。

## 核心特性
- ✅ **透明抠像显示**：WebView2接收像素流，支持绿色抠像透明背景
- ✅ **桌面穿透**：点击穿透功能，数字人悬浮不影响其他应用操作
- ✅ **语音交互**：集成Windows语音识别(ASR)和语音合成(TTS)
- ✅ **脚本系统**：支持自定义讲解内容编辑和播放控制
- ✅ **本地AI**：ONNX Runtime本地AI推理，支持知识库问答
- ✅ **WebSocket通信**：与UE端实时数据交换
- 🔄 **原生透明窗口**：开发中，提供更好的桌面集成

## 当前状态
- ✅ 主程序运行正常，支持透明抠像显示
- ✅ 全局热键 `Alt+S` 打开设置界面
- ✅ 语音服务、WebSocket通信、脚本播放功能完整
- ✅ AI管理界面和知识库检索服务
- 🔄 原生透明窗口功能正在完善中

## 项目结构
```
bestHuman/
├── README.md                   # 项目文档
├── 开发文档.md                 # 详细开发文档
├── bestHuman/
│   ├── bestHuman.sln          # Visual Studio解决方案
│   └── CoreApplication/       # 主应用程序
│       ├── Program.cs         # 程序入口，主窗口管理
│       ├── DigitalHumanDisplay.cs  # 数字人显示控件（WebView2）
│       ├── SettingsForm.cs    # 设置管理界面
│       ├── WebSocketClient.cs # WebSocket通信模块
│       ├── SpeechService.cs   # 语音识别和合成服务
│       ├── ScriptService.cs   # 脚本数据管理
│       ├── ScriptEditForm.cs  # 脚本编辑器
│       ├── ScriptPlayerForm.cs # 脚本播放控制
│       ├── AIService.cs       # AI推理服务
│       ├── AIManagerForm.cs   # AI管理界面
│       ├── ModelInference.cs  # ONNX模型推理
│       ├── VectorSearchService.cs # 向量检索（RAG）
│       ├── NativeLayeredWindow.cs # 原生透明窗口（开发中）
│       ├── Utilities.cs       # 通用工具类
│       ├── data/              # 数据文件目录
│       │   └── knowledge.json # 知识库数据
│       └── models/            # AI模型文件目录
└── NativeWindowTest/          # 原生窗口测试项目
```

## 使用说明

### 系统要求
- Windows 10/11 (x64)
- .NET 9.0 Runtime
- Microsoft Edge WebView2 Runtime

### 运行程序
1. 编译：`dotnet build`
2. 运行：`dotnet run` 或直接运行可执行文件
3. 热键：`Alt+S` 打开设置界面，`Alt+A` 打开AI管理

### 主要功能
- **透明显示**：数字人悬浮桌面，支持绿色抠像
- **语音交互**：语音识别输入，TTS语音输出
- **脚本播放**：自定义讲解内容编辑和播放
- **AI问答**：本地AI模型，支持知识库检索
- **实时通信**：WebSocket与UE端数据交换

## 技术特性

### 架构设计
- **.NET 9.0 + Windows Forms**：现代.NET框架，原生Windows桌面体验
- **WebView2集成**：微软官方Web控件，高性能视频流渲染
- **模块化架构**：松耦合设计，便于功能扩展和维护

### 核心技术
- **抠像算法**：JavaScript像素级处理，支持绿色背景透明化
- **全局热键**：Windows API实现，支持多重快捷键机制
- **语音服务**：Windows内置Speech API，支持ASR和TTS
- **AI推理**：ONNX Runtime本地推理，支持多种AI模型
- **实时通信**：WebSocket双向通信，与UE端数据同步

### 开发进度
- ✅ **基础框架完成**：应用生命周期、窗口管理、配置系统
- ✅ **显示模块完成**：WebView2集成、抠像算法、透明显示
- ✅ **交互模块完成**：语音服务、脚本系统、热键管理
- ✅ **AI模块完成**：本地推理、知识库、向量检索
- 🔄 **原生窗口优化中**：Windows API透明窗口，性能提升

## 开发环境
- **.NET 9.0**: 最新的.NET框架
- **Windows Forms**: 原生Windows桌面应用
- **WebView2**: 微软官方Web控件
- **ONNX Runtime**: AI模型推理引擎
- **System.Speech**: Windows语音服务

## 贡献指南
欢迎贡献代码和建议：
1. Fork 本项目
2. 创建特性分支 (`git checkout -b feature/NewFeature`)
3. 提交更改 (`git commit -m 'Add NewFeature'`)
4. 推送分支 (`git push origin feature/NewFeature`)
5. 创建 Pull Request

## 许可证
本项目采用 MIT 许可证 - 查看 [LICENSE](LICENSE) 文件了解详情

---
*最后更新：2025年6月18日*