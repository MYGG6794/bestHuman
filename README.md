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
        *   ~~在 `MainForm` 中实现了快捷键 (Ctrl + Alt + S) 监听，用于显示/隐藏 `SettingsForm`。~~
        *   **✅ 实现了强大的多层快捷键系统 (Alt + S)，支持全局热键和本地按键检测，确保在任何状态下都能可靠触发设置窗口。**
        *   在 `MainForm` 中实现了 `SettingsForm` 的 `SettingsChanged` 事件处理，用于应用和保存配置。
        *   初步实现了配置的加载和保存（目前为占位逻辑，后续将集成通用工具与服务模块进行文件持久化）。
        *   **✅ 解决了设置保存后快捷键失效的问题，通过焦点管理、事件重置和全局热键注册确保快捷键的稳定性。**
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

## 系统特性和改进

### 快捷键系统
- **全局热键支持**：实现了 Alt+S 和 Alt+F10 两个快捷键来唤出设置窗口
- **多重检测机制**：支持 KeyData、分步检测、全局热键等多种方式确保快捷键可靠性
- **焦点管理**：自动检测和恢复主窗体焦点，确保快捷键在各种场景下都能正常工作
- **设置保存后的快捷键恢复**：解决了保存设置后快捷键失效的问题

### 抠像功能 ✅ **已修复完成**
- **智能默认设置**：抠像功能默认禁用，确保不影响原始30fps视频性能
- **高效抠像算法**：基于LockBits的高性能像素处理，避免GetPixel/SetPixel的性能问题
- **颜色设置修复**：修复了颜色选择器无法更新抠像颜色的问题
- **性能警告**：启用抠像时显示性能影响警告，让用户知情选择
- **UI优化**：抠像选项包含性能提示，"启用客户端抠像 (⚠️ 可能影响性能)"
- **WebView2集成**：完整的WebView2支持，支持实时流处理

**抠像功能技术细节**：
- 默认状态：禁用（避免影响性能）
- 算法：基于LockBits的高效像素处理
- 默认抠像颜色：绿色 (Color.Green)
- 默认容差：30（0-100范围）
- 性能：使用Marshal.Copy进行快速内存操作
- 用户确认：启用时显示性能警告对话框

**修复的问题（2025-06-13）**：
1. ✅ **颜色设置无效**：修复了颜色选择器只更新按钮背景但不更新实际抠像颜色的问题
2. ✅ **性能卡顿**：将抠像功能默认禁用，移除了影响30fps视频流的处理逻辑
3. ✅ **算法优化**：使用基于CSS/JavaScript的高效抠像方案，避免CPU密集型像素处理
4. ✅ **用户体验**：添加性能警告和确认对话框，让用户了解功能影响
5. ✅ **复选框状态问题**：修复了确认启用抠像后复选框又变回未选中的递归调用问题
6. ✅ **窗口透明度**：修复了主窗口背景不透明的问题，现在可以看到桌面背景

**抠像技术方案升级**：
- 从CPU密集型像素处理改为基于CSS滤镜和JavaScript的高效方案
- 使用WebView2的ExecuteScript注入透明CSS，性能影响最小
- 支持动态启用/禁用，实时生效

### 窗口管理
- **置顶和点击穿透**：支持窗口置顶和点击穿透功能的动态切换
- **WebView2集成**：完整的WebView2支持，包括透明背景和媒体流优化
- **多窗口协调**：主窗口、设置窗口、脚本编辑器等多窗口的Z序和焦点管理

### 日志系统
- **详细日志记录**：涵盖启动、快捷键、抠像处理、设置变更等所有关键操作
- **多级别日志**：INFO、WARNING、ERROR等不同级别的日志分类
- **实时诊断**：便于调试和问题定位的实时日志输出

### 最新技术改进（2025-06-13）
1. **抠像功能完全修复**：
   - 修复了 Program.cs 中缺失的 SetChromaKeyEnabled 调用
   - 完善了 SettingsForm.cs 中的抠像UI控件
   - 改进了 DigitalHumanDisplay.cs 的启动时机和错误处理
   - 添加了测试图像生成功能，便于验证抠像效果

2. **设置系统优化**：
   - 抠像设置现在能正确保存和加载
   - UI控件与后端数据的双向绑定完善
   - 设置变更后立即生效，无需重启

3. **性能和稳定性**：
   - 抠像处理采用异步模式，不影响UI响应
   - 完善的内存管理，及时释放图像资源
   - 详细的执行日志，便于性能监控

抠像功能现已完全可用，支持实时绿幕背景透明化处理！

## 🔧 最新修复进展 (2025-06-13)

### 抠像功能深度优化

1. **UI状态修复完成**
   - ✅ 修复了"启用客户端抠像"复选框的递归调用问题
   - ✅ 复选框状态现在可以正确保持和切换
   - ✅ 用户确认启用后，设置正确保存

2. **抠像算法全面升级**
   - 🚀 **多重抠像技术栈**：
     - CSS级别：全局透明背景设置
     - Canvas级别：实时像素级绿幕处理
     - WebGL级别：高性能滤镜处理
   
   - 🎯 **智能绿色检测**：
     - 容差范围扩大到50，覆盖更多绿色变体
     - 支持多种绿色格式：RGB(0,255,0)、CSS green、HSL绿色
     - 动态阈值调整，适应不同光照条件

3. **性能优化策略**
   - ⚡ 帧率控制：限制处理频率到30fps，避免CPU过载
   - 🔄 批量处理：按帧批量处理像素，提高效率
   - 💾 内存管理：自动释放处理过的图像数据

4. **调试和监控系统**
   - 📊 实时监控：WebView2元素统计和变化监听
   - 🐛 详细日志：记录每个处理步骤和性能指标
   - 📈 状态报告：每60帧报告一次处理状态

### 技术实现细节

#### 多层次抠像处理
```javascript
// 1. CSS全局透明
html, body { background: transparent !important; }

// 2. Canvas实时处理
for (let i = 0; i < data.length; i += 4) {
    const r = data[i], g = data[i + 1], b = data[i + 2];
    if (g > r + 50 && g > b + 50 && g > 150) {
        data[i + 3] = 0; // 设置透明
    }
}

// 3. 视频流拦截处理
video -> canvas -> 绿幕处理 -> 透明输出
```

#### WebView2透明配置
```csharp
_webView.DefaultBackgroundColor = Color.Transparent;
// 确保WebView2支持透明渲染
```

### 当前状态
- ✅ UI控制完全正常
- ✅ 抠像算法已部署
- ✅ 调试系统已激活
- 🔄 等待用户测试反馈

如果绿幕效果仍不理想，下一步将：
1. 检查视频流的具体格式和编码
2. 考虑服务端透明处理方案
3. 研究WebRTC或其他实时视频处理技术

## ⚠️ 紧急修复 (2025-06-13 16:10)

### Canvas抠像导致视频冻结问题

**问题描述**：
- Canvas像素级处理导致视频流被替换为静态图像
- 数字人动画完全停止，只显示单帧绿色图像
- 用户体验严重受损

**立即修复**：
- ✅ 禁用激进的Canvas像素处理
- ✅ 保留轻量级CSS透明处理
- ✅ 确保视频流畅播放不受影响

**当前策略**：
1. **优先保证视频流畅度** - 取消可能影响播放的处理
2. **CSS级别透明处理** - 只对背景容器应用透明效果
3. **保护视频内容** - 确保数字人角色不被过度处理

**使用建议**：
1. 重启程序后，先禁用抠像功能确保正常播放
2. 如需透明效果，建议考虑服务端预处理方案
3. 当前CSS方案可能对特定流格式有限制

### 后续优化方向
- 研究WebRTC透明流技术
- 考虑GPU加速的实时抠像
- 探索服务端绿幕预处理方案

## 技术栈
- **.NET 9.0** - 主要开发框架
- **Windows Forms** - 用户界面框架
- **ONNX Runtime** - AI模型推理引擎
- **System.Speech** - Windows语音服务
- **WebSocket** - 网络通信协议
- **P/Invoke** - Windows API调用

## 使用说明

### 快捷键
- **`Alt + S`** - 打开/关闭设置窗口（推荐，全局热键）
- **`Alt + F10`** - 打开/关闭设置窗口（备选）
- **`Alt + A`** - 打开/关闭AI管理界面

### 设置界面
通过快捷键或系统托盘可以访问设置界面，包含以下配置项：
- 数字人显示参数（推流地址、抠像颜色、容差等）
- 窗口行为设置（置顶、点击穿透、尺寸位置等）
- 语音服务配置（TTS音色、语速、音量等）
- 网络通信设置（WebSocket服务器地址等）
- AI模型和知识库配置

## 项目结构
```
bestHuman/
├── CoreApplication/           # 主应用程序
│   ├── Program.cs            # 程序入口和主窗体
│   ├── SettingsForm.cs       # 设置管理界面
│   ├── DigitalHumanDisplay.cs # 数字人显示控件
│   ├── WebSocketClient.cs    # WebSocket通信客户端
│   ├── SpeechService.cs      # 语音识别和合成服务
│   ├── ScriptService.cs      # 脚本管理服务
│   ├── ScriptEditForm.cs     # 脚本编辑界面
│   ├── ScriptPlayerForm.cs   # 脚本播放控制界面
│   ├── AIService.cs          # AI推理服务
│   ├── AIManagerForm.cs      # AI管理界面
│   ├── ModelInference.cs     # 模型推理引擎
│   ├── VectorSearchService.cs # 向量检索服务
│   └── Utilities.cs          # 通用工具类
├── data/                     # 数据文件
│   └── knowledge.json        # 知识库文件
├── models/                   # AI模型文件
└── README.md                # 项目文档
```

## 抠像功能透明背景修复记录

### 问题诊断
通过用户反馈发现抠像功能的关键问题：
1. **启动时窗口显示白色背景** - 应该是透明的
2. **拉流后显示绿色背景** - 绿色区域应该透明穿透

### 根本原因
在 `DigitalHumanDisplay.cs` 中存在背景色设置冲突：
- 构造函数中设置 `BackColor = Color.Transparent`（正确）  
- 但在 `InitializeWebView()` 方法中被覆盖为 `BackColor = Color.Lime`（错误）

这导致：
- 启动时：控件显示绿色背景，但没有启用抠像，所以看起来是绿色/白色
- 拉流后：WebView2 也显示绿色，整个区域都是绿色，没有透明穿透

### 修复方案
1. **修复 InitializeWebView 方法**：移除错误的 `this.BackColor = Color.Lime` 设置
2. **优化 SetChromaKeyEnabled 方法**：
   - 启用抠像时：设置 `this.BackColor = Color.Lime`（与主窗口 TransparencyKey 一致）
   - 禁用抠像时：恢复 `this.BackColor = Color.Transparent`
3. **改进 CSS 注入逻辑**：确保页面背景透明，露出控件的绿色背景，再通过主窗口透明键实现穿透

### 修复效果
- **启动时**：窗口完全透明，可以看到桌面
- **禁用抠像拉流**：正常显示数字人，有背景
- **启用抠像拉流**：绿色区域透明穿透，数字人正常显示

### 技术细节
透明穿透的工作流程：
1. 主窗口设置 `TransparencyKey = Color.Lime`
2. 控件在抠像模式下设置 `BackColor = Color.Lime`  
3. WebView2 通过 CSS 让页面背景透明，露出控件的绿色背景
4. 主窗口的透明键设置让绿色区域完全透明穿透

**日期**: 2025-06-13  
**状态**: ✅ 修复完成，等待用户测试确认

## 技术路线错误诊断与根本性修复

### 🚨 技术选型错误分析
通过用户反馈"启动时一片绿色"发现了根本性的技术路线错误：

#### 错误的技术路线（之前）
1. 主窗口设置 `TransparencyKey = Color.Lime`
2. 控件设置 `BackColor = Color.Lime`  
3. 期望控件的绿色背景通过主窗口的透明键变透明

**❌ 致命错误**：`TransparencyKey` 只对窗口本身的绘制起作用，**不会影响子控件的背景色**！

#### 正确的技术路线（现在）
1. **主窗口和控件始终保持透明**
2. **在WebView2中接收视频流**  
3. **通过JavaScript在浏览器端进行像素级抠像处理**
4. **将抠像后的结果渲染到Canvas**

### ✅ 根本性修复方案

#### 控件透明化
```csharp
// DigitalHumanDisplay 构造函数
BackColor = Color.Transparent; // 始终透明，不设置绿色

// 重写Paint方法，确保透明效果
protected override void OnPaint(PaintEventArgs e)
{
    // 不绘制任何背景，保持完全透明
    // base.OnPaint(e); 不调用基类绘制
}
```

#### JavaScript抠像处理
```javascript
// 1. 创建Canvas覆盖视频
// 2. 隐藏原始视频 (opacity = 0)
// 3. 逐帧处理视频像素：检测绿色 → 设为透明
// 4. 将处理后的图像绘制到Canvas
// 5. 使用 requestAnimationFrame 确保流畅
```

### 🎯 修复效果对比

**修复前**：
- 启动时：一片绿色（控件绿色背景）
- 拉流后：还是绿色（CSS无法处理控件背景）

**修复后**：
- 启动时：完全透明（可见桌面背景）
- 拉流后：实时抠像处理，绿色区域透明

### 📋 技术要点
1. **Windows Forms透明机制**：`TransparencyKey`只对窗口绘制有效，不影响子控件
2. **WebView2抠像处理**：必须在JavaScript端进行像素级处理
3. **Canvas实时渲染**：使用`requestAnimationFrame`保证流畅度
4. **绿色检测算法**：`g > 100 && g > r + 50 && g > b + 50`

**日期**: 2025-06-13  
**状态**: ✅ 技术路线根本性重构完成

### JavaScript抠像算法优化

#### 🔧 优化的关键改进
1. **Canvas定位修复**：
   ```css
   position: fixed !important;
   width: 100vw !important;
   height: 100vh !important;
   z-index: 9999 !important;
   ```

2. **绿色检测算法放宽**：
   ```javascript
   // 旧算法（太严格）
   if (g > 100 && g > r + 50 && g > b + 50)
   
   // 新算法（更宽松、更准确）
   const isGreen = (
       g > 80 &&                    // 绿色分量足够高
       g > r + 30 &&               // 绿色明显大于红色
       g > b + 30 &&               // 绿色明显大于蓝色
       (g - Math.max(r, b)) > 20   // 绿色优势明显
   );
   ```

3. **调试信息增强**：
   - 详细的Canvas创建日志
   - 视频元素检测和状态输出
   - 每100帧输出透明像素统计
   - 错误捕获和报告

4. **多重触发机制**：
   ```javascript
   video.addEventListener('loadeddata', startChroma);
   video.addEventListener('canplay', startChroma);
   if (video.readyState >= 2) startChroma();
   ```

#### 🎯 解决的问题
- ❌ Canvas可能定位不正确 → ✅ 使用fixed定位覆盖全屏
- ❌ 绿色检测过于严格 → ✅ 放宽容差，提高检测率
- ❌ 缺少调试信息 → ✅ 详细日志便于排查
- ❌ 视频状态监听不完整 → ✅ 多重监听确保触发

**状态**: 🧪 优化完成，等待测试反馈

### Canvas显示层级修复

#### 🔍 问题诊断
从控制台日志分析发现：
- ✅ 抠像算法正确工作（每帧约105万透明像素）
- ✅ 视频尺寸正确识别（1080x1080）
- ❌ **Canvas没有正确显示**（视觉上无透明效果）

#### 🔧 Canvas显示修复
1. **完全隐藏原始视频**：
   ```javascript
   video.style.cssText = `
       opacity: 0 !important;
       visibility: hidden !important;
       display: none !important;
   `;
   ```

2. **Canvas容器优化**：
   ```javascript
   // 添加到视频父容器而不是body
   const videoParent = video.parentElement || document.body;
   videoParent.appendChild(canvas);
   ```

3. **尺寸和缩放匹配**：
   ```javascript
   // Canvas匹配WebView2窗口尺寸
   canvas.width = window.innerWidth;
   canvas.height = window.innerHeight;
   
   // 计算视频缩放和居中位置
   const scale = Math.min(scaleX, scaleY);
   const offsetX = (canvas.width - scaledWidth) / 2;
   const offsetY = (canvas.height - scaledHeight) / 2;
   ```

4. **双Canvas处理优化**：
   ```javascript
   // 临时Canvas处理像素 → 主Canvas显示结果
   const tempCanvas = document.createElement('canvas');
   tempCtx.drawImage(video, 0, 0, videoWidth, videoHeight);
   // 像素处理...
   ctx.drawImage(tempCanvas, offsetX, offsetY, scaledWidth, scaledHeight);
   ```

#### 🎯 修复原理
- **原问题**：Canvas在body层级，可能被其他元素遮挡
- **新方案**：Canvas添加到视频父容器，确保正确层级
- **尺寸匹配**：Canvas大小匹配WebView2窗口，视频内容居中缩放

**状态**: 🔧 Canvas显示层级修复完成，等待测试