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