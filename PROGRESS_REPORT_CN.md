# BinaryPathway — 项目进度报告

**日期**: 2026-06-11  
**作者**: 史昀泽 (25126879G)  
**导师**: 彭伟民 博士  
**课程**: COMP5925 元宇宙项目 I  

---

## 1. 执行摘要

BinaryPathway 是一款面向 Meta Quest 的混合现实益智游戏，玩家通过在三维网格中构建连通的二进制路径来解谜。项目已从基础网格原型发展为一个功能完备的游戏，具备状态管理、胜利检测、方块标签显示、关卡配置、逻辑连接系统、远距离抓取交互以及成功的 Android APK 构建能力。

**关键指标：**
- Git 提交：22 次
- 自定义 C# 脚本：20 个
- LFS 追踪的二进制资源：508 个文件 (1.5 GB)
- 场景对象：主场景约 399 个
- 构建：Android APK 构建成功 (144 MB)

---

## 2. 已完成功能

### 2.1 核心游戏系统 (第一阶段)

| 功能 | 状态 | 文件 |
|------|------|------|
| 网格系统 (6×6×1, 可配置) | ✅ 完成 | `GridContainer.cs`, `GridNode.cs`, `GridIndex.cs` |
| BFS 连通性验证 | ✅ 完成 | `GridContainer.cs` — 缓存的 Queue/HashSet |
| 方块系统 (0/1 值) | ✅ 完成 | `TileBase.cs`, `Tile0.cs`, `Tile1.cs`, `TileToggle.cs` |
| XR 插座三级吸附 | ✅ 完成 | `GridSocket.cs` — 弱吸附→平滑预览→硬吸附 |
| 谜题生成 (3 种算法) | ✅ 完成 | `PuzzleInitializer.cs` — 直线/随机/迷宫 |
| 自动起点/终点标记 | ✅ 完成 | `PuzzleInitializer.cs` — 程序化彩色球体 |
| 路径移除（增加可玩性） | ✅ 完成 | 随机移除 40% 路径方块 |
| GameManager 状态机 | ✅ 完成 | `GameManager.cs` — 空闲→游戏中→胜利 |
| 胜利检测 | ✅ 完成 | `GameManager.cs` + `ConnectivityVisualizer.cs` 事件系统 |
| 胜利反馈 | ✅ 完成 | `WinFeedbackController.cs` — 粒子 + TMP 文本 |
| 步数计数器 | ✅ 完成 | 世界空间 TMP "Moves: 0" |
| 方块数值标签 | ✅ 完成 | `TileLabel.cs` — 0(红)/1(绿), 自适应缩放, 广告牌 |

### 2.2 交互系统 (阶段 2A-2B)

| 功能 | 状态 | 文件 |
|------|------|------|
| 方块六面插座系统 | ✅ 预制体设计 | `Tile_0.prefab`, `Tile_1.prefab` — 各 49 个子对象 |
| 物理关节锁定 | ✅ 完成 | `BreakableLatchOnSocket.cs`, `BreakableLinkNode.cs` |
| 方块逻辑连接 | ✅ 完成 | `TileConnector.cs` — 依据 ConnectionRule 验证 |
| 连接规则表 | ✅ 完成 | `ConnectionRule.cs` — ScriptableObject, 5 种逻辑运算 |
| 远距离抓取 (SnapInteractor) | ✅ 完成 | 预制体 — Oculus ISDK SnapInteractor |
| 远距离抓取 (SnapInteractable) | ✅ 完成 | 预制体 — 手部指向的射线目标 |
| 远距离抓取 (GrabInteractor) | ✅ 完成 | 预制体 — Oculus ISDK GrabInteractor |
| 预览线 (TriggerBroadcaster) | ✅ 完成 | 预制体 — InteractableTriggerBroadcaster |
| 准星 + 自动移动 | ✅ 完成 | 预制体 — ReticleDataIcon + AutoMoveTowardsTargetProvider |
| 方块预制体修复 | ✅ 完成 | Tile_0 修复: TileOne→TileZero 组件 |

### 2.3 关卡与配置

| 功能 | 状态 | 文件 |
|------|------|------|
| LevelData ScriptableObject | ✅ 完成 | `LevelData.cs` — 网格大小, 算法, 难度, 限制 |
| 关卡01_简单 (4×4, 30%) | ✅ 完成 | `Assets/Levels/Level_01_Easy.asset` |
| 关卡02_中等 (6×6, 40%, 50步) | ✅ 完成 | `Assets/Levels/Level_02_Medium.asset` |
| 关卡03_困难 (8×6, 50%, 120秒) | ✅ 完成 | `Assets/Levels/Level_03_Hard.asset` |
| DefaultConnectionRule | ✅ 完成 | `Assets/Levels/DefaultConnectionRule.asset` |

### 2.4 性能与稳定性

| 修复 | 效果 | 提交 |
|------|------|------|
| 移除 Debug.Log 刷屏 | CPU 帧时间: 41ms→1.15ms (35倍提升) | `e3c52e1` |
| BFS 容器缓存 | 零逐帧 GC 分配 | `e3c52e1` |
| OnDrawGizmos 崩溃修复 | 消除域重载时的空引用异常 | `2eb73db` |
| GetNode/GetWorldPos 空值保护 | 编辑器稳定性 | `2eb73db` |
| 运动学刚体速度修复 | 消除运行时错误 | `4a1c627` |
| EndManualInteraction 保护 | 消除运行时错误 | `4a1c627` |
| FindObjectOfType 废弃警告 | 编译清洁 | `26f4c09` |
| Shader.Find 空引用修复 | 标记创建修复 | `a9ecfa9` |

### 2.5 构建与部署

| 项目 | 状态 |
|------|------|
| Android IL2CPP 构建 | ✅ 成功 (144 MB APK) |
| 构建场景 | ✅ `ComprehensiveRigExample.unity` |
| 编辑器专用脚本隔离 | ✅ `#if UNITY_EDITOR` 保护 |
| Quest Link 立体渲染修复 | ✅ `QuestStartupCleanup.cs` — 禁用屏幕空间 Canvas |
| Git LFS | ✅ 508 个二进制文件已追踪 |

### 2.6 AI 辅助开发

| 工具 | 用途 |
|------|------|
| Unity-MCP (CoplayDev) | Claude Code ↔ Unity Editor 桥接 |
| HTTP 传输 (端口 :8080) | 场景操作, 脚本编辑, 构建控制 |
| 22 次自动化提交 | 完整的 git 历史及详细提交信息 |
| CLAUDE.md | AI 导向的项目文档 |

---

## 3. 当前架构

### 场景层级 (ComprehensiveRigExample.unity)

```
Scene Root
├── GameManager (144842)
│   ├── GameManager 组件 — 状态: 空闲→游戏中→胜利
│   ├── WinFeedbackController — 粒子 + TMP 文本
│   ├── QuestStartupCleanup — Quest 上禁用有问题的 Canvas
│   ├── WinParticles — 胜利特效粒子系统
│   ├── WinText — "谜题解决!" (初始隐藏)
│   └── MovesText — "Moves: 0"
├── Debug/
│   └── ConnectTest (ConnectivityVisualizer)
├── GridContainer (144448)
│   ├── GridContainer — 6×6×1, cell=0.3, 允许对角线=true
│   ├── XRInteractionManager
│   ├── PuzzleInitializer — 迷宫(2), 自动标记, 路径移除
│   └── SocketsRoot — GridSocket ×36 (运行时)
├── SnapZone-InteractableToHand — 远距离抓取目标
├── Stone-InteractableToHand — 方块远距离抓取参考
├── Stone-HandToInteractable — 远距离抓取参考
├── Dialog (来自 Meta XR 模板, Quest 上已禁用)
└── [XR Rig — CenterEyeAnchor, 控制器, 手部追踪]
```

### 方块预制体组件栈 (各 16 个组件)

```
Tile_0 / Tile_1
├── Transform, Rigidbody
├── XRGrabInteractable (XRI 抓取)
├── TileZero / TileOne (数值逻辑)
├── Grabbable (Oculus ISDK 抓取)
├── GrabInteractor (远距离抓取执行)
├── SnapInteractor (远距离抓取控制)
├── SnapInteractable (射线目标)
├── InteractableTriggerBroadcaster (预览线)
├── ReticleDataIcon (准星显示)
├── AutoMoveTowardsTargetProvider (平滑移动)
├── BreakableLinkNode (关节管理)
├── TileLabel (数值标签)
├── TileConnector (逻辑连接)
├── RespawnOnDrop, PointableUnityEventWrapper
└── Sockets/ (每方块 6 个面插座)
    ├── 上/下/左/右/前/后
    └── 每个: XRSocketInteractor + BoxCollider + BreakableLatchOnSocket
```

---

## 4. 差距分析 (对照中期报告目标)

### 目标一 — 与学习机制对齐的游戏化
| 需求 | 状态 |
|------|------|
| 放置-测试-修正循环 | ✅ BFS 连通性 + 实时反馈 |
| 方块预算 | ❌ 尚未实现 |
| 非法分支惩罚 | ❌ 尚未实现 |
| 微成就 | ❌ 尚未实现 |
| 多模态反馈 | ⚠️ 仅视觉（粒子 + 标签）；音频/触觉待定 |

### 目标二 — 具身交互与多模态反馈
| 需求 | 状态 |
|------|------|
| 光球遍历可视化 | ❌ 尚未实现 |
| 路径高亮 | ❌ 尚未实现 |
| 触觉反馈 | ❌ 尚未实现 |
| 音效 | ⚠️ 预制体已挂载 AudioSources；未接线 |
| 世界空间插座吸附 | ✅ 完成 |
| BFS 规则检查 | ✅ 完成 |
| 手部追踪手势 | ⚠️ 预制体已挂载 HandGrab 姿态；未测试 |

### 目标三 — 模块化 Quest 就绪框架
| 需求 | 状态 |
|------|------|
| 可配置网格生成 | ✅ 完成 |
| 基于插座的吸附 | ✅ 完成 |
| BFS 连通性检查 | ✅ 完成 |
| 模块分离 | ✅ ScriptableObject 关卡 + 连接规则 |
| URP + SRP Batcher | ✅ 已配置 |
| 72/80/90 Hz 目标 | ⚠️ 尚未在真机上剖析 |
| OpenXR 对齐 | ✅ 完成 |
| 文档化 API | ✅ CLAUDE.md + 内联注释 |

### 方块逻辑系统 (依据中期报告 §5.2)
| 需求 | 状态 |
|------|------|
| 带值与逻辑的方块 | ⚠️ ToggleTile 已存在；逻辑运算已接线 |
| 六面插座连接 | ✅ 预制体完成 |
| 方块组合（连接） | ✅ 物理: BreakableLatchOnSocket; 逻辑: TileConnector |
| 方块组合（断开） | ⚠️ 拉开分离: 未实现 |
| 数值方块+逻辑方块融合 | ✅ ConnectionRule.LogicOperation (SetValue/Toggle/And/Or/Xor) |
| 含逻辑运算的谜题生成 | ❌ 目前仅随机移除；NOT/XOR/AND 变换尚未实现 |
| QR 码关卡加载 | ❌ 未来工作 |
| MR 透视模式 | ❌ 未来工作 |

---

## 5. 已知问题

### 开发范围内 (计划修复)

| # | 问题 | 优先级 |
|---|------|--------|
| 1 | 谜题生成使用简单移除，未使用逻辑变换 | P1 |
| 2 | 方块组合断开（拉开分离）未实现 | P1 |
| 3 | 无抓取/放置/胜利音效反馈 | P2 |
| 4 | 无触觉反馈 | P2 |
| 5 | 步数限制 + 自动失败未接入 UI | P2 |
| 6 | MovesText 视觉不够突出 | P3 |
| 7 | 远距离抓取 `_timeOutInteractable` 需在预制体中手动接线 | P3 |

### 开发范围外 (Meta SDK / 模拟器)

| # | 问题 | 影响 |
|---|------|------|
| 8 | OpenXR ProcessOpenXRMessageLoop 空引用（模拟器中每帧） | 仅编辑器 — Quest 不受影响 |
| 9 | 27 个重复的 XR 子系统注册 | 仅模拟器 |
| 10 | OVR 退出 Play Mode 时未销毁的交换链 | 编辑器崩溃风险, Quest 不受影响 |
| 11 | Quest Link 合成器交换链损坏 | 重启 Oculus 应用修复 |

---

## 6. 构建与测试状态

| 平台 | 状态 | 备注 |
|------|------|------|
| **编辑器 Play Mode** | ✅ 可用 | XR 模拟器交互正常（有模拟器限制） |
| **Android APK 构建** | ✅ 成功 | 144 MB, IL2CPP Release |
| **Quest 3 部署** | ⚠️ 已构建但有视觉问题 | 立体渲染修复已应用；需重新构建测试 |
| **自动化测试** | ❌ 无 | EditMode/PlayMode 测试已规划 |
| **性能剖析** | ⚠️ 仅编辑器 | 编辑器: ~2ms 稳定; Quest: 尚未剖析 |

---

## 7. 下一步开发优先级

### 近期 (第 11-12 周)
1. 使用 QuestStartupCleanup + 正确场景重新构建 APK → Quest 3 测试
2. 在预制体中手动接线远距离抓取 `_timeOutInteractable`
3. 实现含逻辑运算的谜题生成 (NOT/XOR/AND, 依据报告)
4. 添加胜利时路径高亮可视化

### 短期 (第 13-14 周)
5. 音频反馈系统 (抓取, 放置, 连接, 胜利)
6. 触觉反馈集成
7. 步数限制执行 + 自动失败 UI
8. 多关卡流程 (关卡 1→2→3 推进)

### 中期 (第 15-16 周)
9. 方块拉开分离机制
10. Quest 性能优化 (剖析, FFR, 动态分辨率)
11. EditMode 单元测试 (BFS, 路径生成, 连接规则)

### 远期
12. QR 码关卡加载
13. MR 透视模式
14. 多人模式 (按报告时间表)

---

## 8. 仓库信息

- **GitHub**: https://github.com/STCloudLake/BinaryPathway
- **分支**: `master` (22 次提交)
- **LFS**: 508 个二进制文件
- **文档**: `CLAUDE.md` (架构), `README.md` (简介), `Leveraging VR Technologies...pdf` (学术报告)

---

*本报告由 Claude Code 协助生成。更新于 2026-06-11。*
