# Unity 手动操作指南 — 场景搭建与引用绑定

> 本文档详细指导在 Unity Editor 中完成场景搭建、预制体创建、组件挂载和引用绑定的全部步骤。

---

## 第一步：Canvas 与 EventSystem

### 1.1 修改现有 Canvas

当前 `SampleScene.scene` 中已有 `canvas` 对象（World Space），但缺少 `CanvasScaler`。

1. **Hierarchy** 中选中 `canvas`
2. **Inspector** → Canvas 组件 → Render Mode 改为 `Screen Space - Overlay`
3. 右键 → `UI` → 不会有新 Canvas，而是直接在 Inspector 底部 `Add Component` → 搜索 `CanvasScaler` 并添加
4. CanvasScaler 设置：
   - UI Scale Mode = `Scale With Screen Size`
   - Reference Resolution = `1920 × 1080`
   - Screen Match Mode = `Match Width Or Height`
   - Match = `0.5`

### 1.2 添加 EventSystem

当前场景缺少 EventSystem，UI 交互依赖它。

1. Hierarchy 空白处右键 → `UI` → `Event System`（Unity 会自动创建）
2. 确认 EventSystem 对象上有 `Standalone Input Module` 组件

---

## 第二步：系统管理器 GameObject

### 2.1 创建 SystemManager 根节点

1. Hierarchy 右键 → `Create Empty`，命名为 `SystemRoot`
2. 在 `SystemRoot` 上 `Add Component` 依次添加：
   - `SystemManager`（搜索 CardGame.SystemManager）
   - `GameManager`（搜索 CardGame.GameManager）
   - `InGameScenario`（搜索 CardGame.InGameScenario）
   - `UIManager`（搜索 CardGame.UI.UIManager）

### 2.2 绑定 SystemManager 的 SerializeField

选中 `SystemRoot`，在 Inspector 中找到各组件：

**SystemManager 组件：**
| 字段 | 拖入对象 |
|------|----------|
| Game Manager | `SystemRoot` 自身（同对象上的 GameManager） |
| Scenario Manager | `SystemRoot` 自身（同对象上的 InGameScenario） |

**GameManager 组件：**
| 字段 | 值 |
|------|-----|
| Copies Per Card | `5`（默认，7×5=35张牌） |
| CPU Think Delay | `1.5` |
| Skip Delay | `0.5` |
| In Game Scenario | `SystemRoot` 自身（同对象上的 InGameScenario） |

**UIManager 组件：**
| 字段 | 拖入对象 |
|------|----------|
| Title Screen | （第三步创建后绑定） |
| Pre Game Scenario | （第四步创建后绑定） |
| Game Match | （第五步创建后绑定） |
| Post Game | （第七步创建后绑定） |
| Bubble UI | （第五步内创建后绑定） |
| System Manager | `SystemRoot` 自身 |
| Game Manager | `SystemRoot` 自身 |
| In Game Scenario | `SystemRoot` 自身 |

> ⚠️ 先完成后面步骤创建好 UI 面板后，再回来绑定这些引用。

---

## 第三步：TitleScreenUI 面板

### 3.1 整理已有 startgame 结构

当前 Hierarchy 中 `canvas > startgame > background` 已有完整的标题画面结构。

1. 选中 `canvas` 下的 `startgame`，确认其子对象结构：
   ```
   startgame
   └── background
       ├── moving        ← Image + Animator（播放 moving.anim）
       ├── pic           ← Image（标题图/船锚图）
       └── left
           ├── Button_start   ← Image（playstar.png）+ Button
           └── Button_end     ← Image（playend.png）+ Button
   ```

2. 在 `startgame` 上 `Add Component` → 搜索 `TitleScreenUI`

### 3.2 绑定 TitleScreenUI

选中 `startgame`，Inspector 中 TitleScreenUI 组件：

| 字段 | 拖入对象 |
|------|----------|
| Start Button | `startgame > background > left > Button_start` |
| Quit Button | `startgame > background > left > Button_end` |
| Background | `startgame > background > moving`（Image 组件） |
| Background Animator | `startgame > background > moving`（Animator 组件） |
| Title Text | 暂时留空，或创建一个 TextMeshPro 子对象 |

### 3.3 确认 Animator 设置

1. 选中 `moving` 对象
2. Inspector → Animator 组件 → Controller = `moving.controller`（应已自动绑定）
3. 确认 Speed = `0.5`（代码中会覆盖，但 Editor 中也设一下）

---

## 第四步：PreGame/PostGame Scenario UI 面板

### 4.1 创建剧情演出面板

1. 在 `canvas` 下右键 → `Create Empty`，命名为 `ScenarioPanel`
2. 设 RectTransform：
   - Anchor = Stretch（上下左右都拉满）
   - Left/Right/Top/Bottom = `0`

3. 在 `ScenarioPanel` 下创建子对象：

```
ScenarioPanel (CanvasGroup + ScenarioUI)
├── Background (Image, 全屏半透明黑色, Alpha=0.8)
├── DialogueArea (RectTransform, 中下方)
│   ├── SpeakerText (TextMeshProUGUI)
│   └── DialogueText (TextMeshProUGUI)
└── ContinueButton (Button + TextMeshProUGUI "点击继续")
```

### 4.2 具体创建步骤

**4.2.1 ScenarioPanel 上添加组件：**
- `CanvasGroup`（Alpha=1, Interactable=true, Blocks Raycast=true）
- `ScenarioUI`（搜索 CardGame.UI.ScenarioUI）

**4.2.2 创建 Background：**
- 右键 ScenarioPanel → `UI` → `Image`，命名 `Background`
- Image Color = `(0, 0, 0, 0.8)`（半透明黑）
- RectTransform Anchor = Stretch，四边=0

**4.2.3 创建 DialogueArea：**
- 右键 ScenarioPanel → `Create Empty`，命名 `DialogueArea`
- RectTransform Anchor = Bottom Stretch，Height=`300`，Bottom=`100`
- 添加 `Vertical Layout Group`（间距=20，Child Alignment=Middle Center）

**4.2.4 创建 SpeakerText：**
- 右键 DialogueArea → `UI` → `Text - TextMeshPro`，命名 `SpeakerText`
- Font Size = `36`，Alignment = Center
- Color = `(1, 0.9, 0.5)`（暖色）

**4.2.5 创建 DialogueText：**
- 右键 DialogueArea → `UI` → `Text - TextMeshPro`，命名 `DialogueText`
- Font Size = `28`，Alignment = Center
- Color = 白色

**4.2.6 创建 ContinueButton：**
- 右键 ScenarioPanel → `UI` → `Button - TextMeshPro`，命名 `ContinueButton`
- 放在屏幕右下方
- 子对象 Text 改为 "点击继续 ▶"

### 4.3 绑定 ScenarioUI

选中 `ScenarioPanel`，Inspector 中 ScenarioUI 组件：

| 字段 | 拖入对象 |
|------|----------|
| Speaker Text | `ScenarioPanel > DialogueArea > SpeakerText` |
| Dialogue Text | `ScenarioPanel > DialogueArea > DialogueText` |
| Continue Button | `ScenarioPanel > ContinueButton` |
| Canvas Group | `ScenarioPanel` 自身的 CanvasGroup |
| Fade Duration | `0.5` |

---

## 第五步：GameMatchUI 面板（核心对局界面）

### 5.1 整体结构

```
GameMatchPanel (GameMatchUI)
├── TopBar
│   └── CurrentTurnText (TextMeshProUGUI)
├── BoardArea_Player (BoardAreaUI)        ← 屏幕下方
│   ├── PlayerNameText
│   ├── CardContainer (Horizontal Layout Group)
│   └── StatusIcon (Image)
├── BoardArea_CPU1 (BoardAreaUI)          ← 屏幕右上方（瘦子）
│   ├── PlayerNameText
│   ├── CardContainer
│   └── StatusIcon
├── BoardArea_CPU2 (BoardAreaUI)          ← 屏幕左上方（胖子）
│   ├── PlayerNameText
│   ├── CardContainer
│   └── StatusIcon
├── DeckArea
│   ├── DeckImage (Image, 船锚占位)
│   └── DeckCountText (TextMeshProUGUI)
├── HandContainer (Horizontal Layout Group)  ← 屏幕最底部
├── PlayButton (Button + TextMeshProUGUI)
└── BubblePanel (DialogueBubbleUI)
    ├── BubblePanel (子对象)
    │   ├── BubbleSpeakerText
    │   └── BubbleContentText
    ├── TopPromptPanel
    │   └── TopPromptText
    └── InterruptPanel
        ├── InterruptText
        └── InterruptContinueButton
```

### 5.2 创建 GameMatchPanel

1. 在 `canvas` 下右键 → `Create Empty`，命名 `GameMatchPanel`
2. RectTransform Anchor = Stretch，四边=0
3. 添加 `GameMatchUI` 组件
4. 初始 SetActive = `false`（游戏开始时由 UIManager 控制）

### 5.3 创建 TopBar

1. 右键 GameMatchPanel → `Create Empty`，命名 `TopBar`
   - Anchor = Top Stretch，Height=`60`
2. 右键 TopBar → `UI` → `Text - TextMeshPro`，命名 `CurrentTurnText`
   - Font Size = `32`，Alignment = Center
   - Color = 白色

### 5.4 创建三个 BoardArea

#### 5.4.1 BoardArea_Player（你 — 屏幕下方）

1. 右键 GameMatchPanel → `Create Empty`，命名 `BoardArea_Player`
2. RectTransform：Anchor = Bottom Stretch，Height=`180`，Bottom=`250`
3. 添加 `Image`（Panel 背景），Color = `(0.15, 0.15, 0.2, 0.4)`
4. 添加 `BoardAreaUI` 组件
5. 在 BoardArea_Player 下创建子对象：
   - `PlayerNameText`（TextMeshPro，"你"，Font Size=24，放在顶部）
   - `CardContainer`（Create Empty + Horizontal Layout Group）
     - RectTransform：Anchor = Stretch（底部区域），留出顶部给名字
     - Horizontal Layout Group：Spacing=`10`，Child Alignment=Middle Center
     - 还要加 `Content Size Fitter`（Horizontal Fit=Preferred Size）
   - `StatusIcon`（Image，初始 SetActive=false）
6. 选中 BoardArea_Player 的 BoardAreaUI 组件绑定：
   | 字段 | 拖入 |
   |------|------|
   | Player Name Text | `BoardArea_Player > PlayerNameText` |
   | Card Container | `BoardArea_Player > CardContainer` |
   | Card UI Prefab | （第六步创建的 CardUI prefab） |
   | Panel Background | BoardArea_Player 自身的 Image |
   | Status Icon | `BoardArea_Player > StatusIcon` |

#### 5.4.2 BoardArea_CPU1（瘦子 — 屏幕右上方）

1. 右键 GameMatchPanel → `Create Empty`，命名 `BoardArea_CPU1`
2. RectTransform：Anchor = 右上角，Width=`400`，Height=`150`，Top=`80`，Right=`20`
3. 重复 5.4.1 的步骤 3-5
4. BoardAreaUI 组件 Setup：PlayerId=1, Name="瘦子"

#### 5.4.3 BoardArea_CPU2（胖子 — 屏幕左上方）

1. 右键 GameMatchPanel → `Create Empty`，命名 `BoardArea_CPU2`
2. RectTransform：Anchor = 左上角，Width=`400`，Height=`150`，Top=`80`，Left=`20`
3. 重复 5.4.1 的步骤 3-5
4. BoardAreaUI 组件 Setup：PlayerId=2, Name="胖子"

### 5.5 创建 DeckArea（牌堆区）

1. 右键 GameMatchPanel → `Create Empty`，命名 `DeckArea`
2. RectTransform：Anchor = 右下角，Width=`120`，Height=`160`，Bottom=`80`，Right=`20`
3. 在 DeckArea 下创建：
   - `DeckImage`（Image，Color=深蓝 `(0.1, 0.2, 0.4)`，代表牌背）
   - `DeckCountText`（TextMeshPro，"剩余 XX 张"，Font Size=18）
4. 布局：DeckImage 在上方，DeckCountText 在下方

### 5.6 创建 HandContainer（手牌区）

1. 右键 GameMatchPanel → `Create Empty`，命名 `HandContainer`
2. RectTransform：Anchor = Bottom Stretch，Height=`200`，Bottom=`20`
3. 添加组件：
   - `Horizontal Layout Group`：Spacing=`15`，Child Alignment=Middle Center，Child Force Expand=false
   - `Content Size Fitter`：Horizontal Fit=Preferred Size

### 5.7 创建 PlayButton

1. 右键 GameMatchPanel → `UI` → `Button - TextMeshPro`，命名 `PlayButton`
2. RectTransform：Anchor = 右下角，Width=`200`，Height=`60`，Bottom=`30`，Right=`160`
3. 子对象 Text 命名 `PlayButtonText`，Font Size=`24`
4. 初始 SetActive = `false`

### 5.8 创建 BubblePanel（DialogueBubbleUI）

1. 右键 GameMatchPanel → `Create Empty`，命名 `BubblePanel`
2. 添加 `DialogueBubbleUI` 组件

#### 5.8.1 气泡本体

1. 右键 BubblePanel → `Create Empty`，命名 `BubblePanel`
2. RectTransform：Anchor = 中上方，Width=`500`，Height=`80`，Top=`80`
3. 添加 `Image`（Color = `(0, 0, 0, 0.7)`）
4. 添加 `CanvasGroup`
5. 在 BubblePanel 下创建：
   - `BubbleSpeakerText`（TextMeshPro，Font Size=20，左上角，黄色）
   - `BubbleContentText`（TextMeshPro，Font Size=24，居中，白色）

#### 5.8.2 上方提示

1. 右键 BubblePanel → `Create Empty`，命名 `TopPromptPanel`
2. RectTransform：Anchor = Top Stretch，Height=`50`，Top=`10`
3. 添加 `Image`（Color = `(0, 0, 0, 0.5)`）
4. 初始 SetActive = `false`
5. 在 TopPromptPanel 下创建 `TopPromptText`（TextMeshPro，Font Size=20，白色，居中）

#### 5.8.3 打断面板

1. 右键 BubblePanel → `Create Empty`，命名 `InterruptPanel`
2. RectTransform：Anchor = Stretch，四边=0
3. 添加 `Image`（Color = `(0, 0, 0, 0.6)`）
4. 初始 SetActive = `false`
5. 在 InterruptPanel 下创建：
   - `InterruptText`（TextMeshPro，Font Size=28，居中，白色）
   - `InterruptContinueButton`（Button + TextMeshPro "点击继续 ▶"，放在下方）

#### 5.8.4 绑定 DialogueBubbleUI

选中 `BubblePanel`（父对象），Inspector 中 DialogueBubbleUI 组件：

| 字段 | 拖入对象 |
|------|----------|
| Bubble Panel | `BubblePanel > BubblePanel`（子对象） |
| Speaker Text | `BubblePanel > BubblePanel > BubbleSpeakerText` |
| Content Text | `BubblePanel > BubblePanel > BubbleContentText` |
| Canvas Group | `BubblePanel > BubblePanel` 的 CanvasGroup |
| Fade Duration | `0.2` |
| Top Prompt Text | `BubblePanel > TopPromptPanel > TopPromptText` |
| Top Prompt Panel | `BubblePanel > TopPromptPanel` |
| Interrupt Panel | `BubblePanel > InterruptPanel` |
| Interrupt Text | `BubblePanel > InterruptPanel > InterruptText` |
| Interrupt Continue Button | `BubblePanel > InterruptPanel > InterruptContinueButton` |

### 5.9 绑定 GameMatchUI

选中 `GameMatchPanel`，Inspector 中 GameMatchUI 组件：

| 字段 | 拖入对象 |
|------|----------|
| Hand Container | `GameMatchPanel > HandContainer` |
| Card UI Prefab | （第六步创建的 CardUI prefab，拖入 Project 窗口的 prefab） |
| Player Board Area | `GameMatchPanel > BoardArea_Player` |
| CPU1 Board Area | `GameMatchPanel > BoardArea_CPU1` |
| CPU2 Board Area | `GameMatchPanel > BoardArea_CPU2` |
| Deck Count Text | `GameMatchPanel > DeckArea > DeckCountText` |
| Deck Image | `GameMatchPanel > DeckArea > DeckImage` |
| Play Button | `GameMatchPanel > PlayButton` |
| Play Button Text | `GameMatchPanel > PlayButton > PlayButtonText` |
| Bubble UI | `GameMatchPanel > BubblePanel`（DialogueBubbleUI） |
| Current Turn Text | `GameMatchPanel > TopBar > CurrentTurnText` |

---

## 第六步：CardUI 预制体

### 6.1 创建 CardUI GameObject

1. 在 Hierarchy 中右键 → `Create Empty`，命名 `CardUI`
2. RectTransform：Width=`120`，Height=`170`

### 6.2 添加组件

在 `CardUI` 上依次添加：
1. `Image` — 卡牌背景色块，Color 先设白色（运行时由代码覆盖）
2. `Canvas Group` — 用于控制透明度/交互
3. `CardUI` 脚本组件

### 6.3 创建子文本对象

1. 右键 CardUI → `UI` → `Text - TextMeshPro`，命名 `CardIdText`
   - Font Size = `48`，Alignment = Center
   - RectTransform：Anchor = Top Half，Top=`10`
2. 右键 CardUI → `UI` → `Text - TextMeshPro`，命名 `CardNameText`
   - Font Size = `20`，Alignment = Center
   - RectTransform：Anchor = Bottom Half，Bottom=`10`

### 6.4 绑定 CardUI 组件

选中 `CardUI`，Inspector 中 CardUI 组件：

| 字段 | 拖入对象 |
|------|----------|
| Card Id Text | `CardUI > CardIdText` |
| Card Name Text | `CardUI > CardNameText` |
| Background | `CardUI` 自身的 Image |
| Canvas Group | `CardUI` 自身的 CanvasGroup |

### 6.5 保存为 Prefab

1. 在 Project 窗口中，进入 `Assets/Prefab/` 目录
2. 将 Hierarchy 中的 `CardUI` 拖入 Project 窗口
3. 命名 `CardUI.prefab`
4. 从 Hierarchy 中删除 `CardUI`（场景中不需要，运行时实例化）

### 6.6 回填 CardUI Prefab 引用

回到第五步的两个地方，将 `CardUI.prefab` 拖入：
- `GameMatchPanel` 上 GameMatchUI 的 `Card UI Prefab` 字段
- 三个 BoardAreaUI 的 `Card UI Prefab` 字段

---

## 第七步：PostGameUI 面板

### 7.1 创建结算面板

1. 在 `canvas` 下右键 → `Create Empty`，命名 `PostGamePanel`
2. RectTransform Anchor = Stretch，四边=0
3. 添加 `CanvasGroup`（Alpha=0）
4. 添加 `Image`（Color = `(0, 0, 0, 0.85)`）
5. 添加 `PostGameUI` 组件
6. 初始 SetActive = `false`

### 7.2 创建子对象

```
PostGamePanel (CanvasGroup + Image + PostGameUI)
├── ResultText (TextMeshProUGUI, 居中, Font Size=48)
├── ScoreText (TextMeshProUGUI, 居中下方, Font Size=28)
├── RestartButton (Button + TextMeshProUGUI "再来一局")
└── ReturnMenuButton (Button + TextMeshProUGUI "返回菜单")
```

### 7.3 绑定 PostGameUI

选中 `PostGamePanel`，Inspector 中 PostGameUI 组件：

| 字段 | 拖入对象 |
|------|----------|
| Result Text | `PostGamePanel > ResultText` |
| Score Text | `PostGamePanel > ScoreText` |
| Restart Button | `PostGamePanel > RestartButton` |
| Return Menu Button | `PostGamePanel > ReturnMenuButton` |
| Canvas Group | `PostGamePanel` 自身的 CanvasGroup |
| Fade Duration | `0.5` |

---

## 第八步：回填所有引用

回到第二步的 UIManager 组件，现在所有面板都创建好了：

选中 `SystemRoot`，Inspector 中 UIManager 组件：

| 字段 | 拖入对象 |
|------|----------|
| Title Screen | `canvas > startgame`（TitleScreenUI） |
| Pre Game Scenario | `canvas > ScenarioPanel`（ScenarioUI） |
| Game Match | `canvas > GameMatchPanel`（GameMatchUI） |
| Post Game | `canvas > PostGamePanel`（PostGameUI） |
| Bubble UI | `canvas > GameMatchPanel > BubblePanel`（DialogueBubbleUI） |
| System Manager | `SystemRoot` 自身 |
| Game Manager | `SystemRoot` 自身 |
| In Game Scenario | `SystemRoot` 自身 |

---

## 第九步：最终检查清单

### 9.1 Hierarchy 应有的结构

```
SampleScene
├── Main Camera
├── Directional Light
├── EventSystem                          ← 第1步
├── SystemRoot                           ← 第2步
│   └── (SystemManager + GameManager + InGameScenario + UIManager)
└── canvas                               ← 已有，第1步修改
    ├── startgame                        ← 已有，第3步添加 TitleScreenUI
    │   └── background
    │       ├── moving (Animator)
    │       ├── pic
    │       └── left
    │           ├── Button_start
    │           └── Button_end
    ├── ScenarioPanel                   ← 第4步
    │   ├── Background
    │   ├── DialogueArea
    │   │   ├── SpeakerText
    │   │   └── DialogueText
    │   └── ContinueButton
    ├── GameMatchPanel                  ← 第5步
    │   ├── TopBar
    │   │   └── CurrentTurnText
    │   ├── BoardArea_Player
    │   │   ├── PlayerNameText
    │   │   ├── CardContainer
    │   │   └── StatusIcon
    │   ├── BoardArea_CPU1
    │   │   ├── PlayerNameText
    │   │   ├── CardContainer
    │   │   └── StatusIcon
    │   ├── BoardArea_CPU2
    │   │   ├── PlayerNameText
    │   │   ├── CardContainer
    │   │   └── StatusIcon
    │   ├── DeckArea
    │   │   ├── DeckImage
    │   │   └── DeckCountText
    │   ├── HandContainer
    │   ├── PlayButton
    │   │   └── PlayButtonText
    │   └── BubblePanel
    │       ├── BubblePanel
    │       │   ├── BubbleSpeakerText
    │       │   └── BubbleContentText
    │       ├── TopPromptPanel
    │       │   └── TopPromptText
    │       └── InterruptPanel
    │           ├── InterruptText
    │           └── InterruptContinueButton
    └── PostGamePanel                   ← 第7步
        ├── ResultText
        ├── ScoreText
        ├── RestartButton
        └── ReturnMenuButton
```

### 9.2 初始 Active 状态

| 对象 | 初始 Active |
|------|-------------|
| `startgame` | ✅ true |
| `ScenarioPanel` | ❌ false（由代码控制） |
| `GameMatchPanel` | ❌ false |
| `PostGamePanel` | ❌ false |
| `GameMatchPanel > BubblePanel` | ✅ true（子组件内部控制可见性） |

### 9.3 Project 窗口 Prefab

```
Assets/Prefab/
├── startgame.prefab    ← 已有
└── CardUI.prefab        ← 第6步创建
```

### 9.4 保存场景

1. `Ctrl+S` / `Cmd+S` 保存场景
2. 确认无编译错误（Console 窗口）

---

## 启动测试

1. 确认 `SystemRoot` 上 UIManager 组件的 Initialize 方法需要被调用。两种方式：
   - 在 UIManager 脚件的 Awake/Start 中调用 Initialize()（当前未实现，需手动加）
   - 或在场景中添加一个启动脚本调用 `UIManager.Instance.Initialize()`

2. 按 Play，应看到标题画面动画播放
3. 点击 START → 进入剧情 → 点击继续 → 进入对局
4. 对局中手牌区显示卡牌，点击选中后出牌按钮出现
