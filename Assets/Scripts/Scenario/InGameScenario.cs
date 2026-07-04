using System.Collections.Generic;
using UnityEngine;

namespace CardGame
{
    /// <summary>
    /// 局中演出管理器 — 继承 ScenarioManager，以气泡+文字形式展示局中对话。
    /// <para>不打断对局流程操作：气泡叠在 UI 之上，玩家仍可正常出牌。</para>
    /// <para>由 InGameEventMonitor 触发：监视特定操作 → 触发对应气泡演出。</para>
    /// <para>对话内容对应 event_scenario.csv。</para>
    /// </summary>
    public class InGameScenario : ScenarioManager
    {
        [Header("气泡设置")]
        [Tooltip("气泡默认显示时长（秒）")]
        [SerializeField] private float _defaultDuration = 5f;

        [Tooltip("气泡队列间隔（秒）")]
        [SerializeField] private float _queueInterval = 0.3f;

        /// <summary>气泡队列间隔（秒），当前气泡结束后等待此时长再显示下一条。</summary>
        public float QueueInterval => _queueInterval;

        // ── 演出数据 ──────────────────────────────────────────

        private readonly Dictionary<InGameEventType, ScenarioEntry> _entries = new();
        private readonly Queue<InGameEvent> _eventQueue = new();
        private float _bubbleTimer;
        private DialogueLine _currentBubble;
        private DialogueLine _currentTopPrompt;

        /// <summary>当前正在显示的气泡内容。</summary>
        public DialogueLine CurrentBubble => _currentBubble;

        /// <summary>当前屏幕上方提示。</summary>
        public DialogueLine CurrentTopPrompt => _currentTopPrompt;

        /// <summary>是否有气泡正在显示。</summary>
        public bool IsBubbleActive => _bubbleTimer > 0f;

        /// <summary>当前事件是否需要打断操作（点击继续）。</summary>
        public bool IsInterruptActive { get; private set; }

        // 随机不重复：每个事件类型上次选中的索引
        private readonly Dictionary<InGameEventType, int> _lastBubbleIndex = new();

        protected virtual void Awake()
        {
            RegisterAllDialogues();
        }

        private void Update()
        {
            if (_bubbleTimer > 0f)
            {
                _bubbleTimer -= Time.deltaTime;
                if (_bubbleTimer <= 0f)
                {
                    _currentBubble = default;
                    ProcessQueue();
                }
            }
        }

        // ── 事件触发入口 ──────────────────────────────────────

        /// <summary>
        /// 由 InGameEventMonitor 调用：将对局事件加入气泡播放队列。
        /// </summary>
        public void TriggerEvent(InGameEvent evt)
        {
            _eventQueue.Enqueue(evt);
            if (!IsBubbleActive && !IsInterruptActive)
                ProcessQueue();
        }

        /// <summary>打断事件：玩家点击继续后调用，解除打断状态并处理队列。</summary>
        public void ContinueInterrupt()
        {
            IsInterruptActive = false;
            _currentTopPrompt = default;
            ProcessQueue();
        }

        /// <summary>注册自定义事件对话。</summary>
        public void RegisterEntry(InGameEventType type, ScenarioEntry entry)
        {
            _entries[type] = entry;
        }

        // ── 内部逻辑 ──────────────────────────────────────────

        private void ProcessQueue()
        {
            if (_eventQueue.Count == 0) return;

            var evt = _eventQueue.Dequeue();
            if (!_entries.TryGetValue(evt.Type, out var entry))
            {
                ProcessQueue(); // 无对应对话，处理下一条
                return;
            }

            // 打断事件
            if (entry.Interrupt)
            {
                IsInterruptActive = true;
                if (entry.TopPrompts != null && entry.TopPrompts.Length > 0)
                    _currentTopPrompt = ResolveTemplates(entry.TopPrompts[0], evt);
                return; // 等待 ContinueInterrupt() 调用
            }

            // 屏幕上方提示
            if (entry.TopPrompts != null && entry.TopPrompts.Length > 0)
            {
                _currentTopPrompt = ResolveTemplates(entry.TopPrompts[0], evt);
            }

            // 气泡
            if (entry.RandomBubbles != null && entry.RandomBubbles.Length > 0)
            {
                int idx = PickRandomIndex(evt.Type, entry.RandomBubbles.Length);
                var bubble = entry.RandomBubbles[idx];
                bubble.speaker = entry.BubbleSpeaker ?? bubble.speaker;
                _currentBubble = ResolveTemplates(bubble, evt);
                _bubbleTimer = bubble.duration > 0 ? bubble.duration : _defaultDuration;
            }
            else
            {
                // 无气泡，直接处理下一条
                ProcessQueue();
            }
        }

        private int PickRandomIndex(InGameEventType type, int count)
        {
            if (count <= 1) return 0;
            _lastBubbleIndex.TryGetValue(type, out int last);
            int idx;
            do { idx = Random.Range(0, count); } while (idx == last);
            _lastBubbleIndex[type] = idx;
            return idx;
        }

        private DialogueLine ResolveTemplates(DialogueLine line, InGameEvent evt)
        {
            return new DialogueLine(
                line.speaker,
                ReplaceTemplates(line.text, evt),
                line.duration
            );
        }

        /// <summary>替换文案模板变量。</summary>
        private string ReplaceTemplates(string text, InGameEvent evt)
        {
            if (string.IsNullOrEmpty(text)) return text;
            return text
                .Replace("{出牌者名称}", GetPlayerName(evt.PlayerId))
                .Replace("{对象名称}", GetPlayerName(evt.TargetPlayerId))
                .Replace("{被置入者名称}", GetPlayerName(evt.PlayerId))
                .Replace("{防御者名称}", GetPlayerName(evt.PlayerId))
                .Replace("{被换牌者名称}", GetPlayerName(evt.TargetPlayerId));
        }

        /// <summary>玩家 ID → 显示名称。</summary>
        private string GetPlayerName(int playerId)
        {
            return playerId switch
            {
                0 => "你",
                1 => "瘦子",
                2 => "胖子",
                _ => $"玩家{playerId}"
            };
        }

        /// <summary>清空队列和当前气泡。</summary>
        public void ClearAll()
        {
            _eventQueue.Clear();
            _bubbleTimer = 0f;
            _currentBubble = default;
            _currentTopPrompt = default;
            IsInterruptActive = false;
            _lastBubbleIndex.Clear();
        }

        // ── 注册全部 CSV 对话 ─────────────────────────────────

        private void RegisterAllDialogues()
        {
            // #1 GameStart — 打断
            RegisterEntry(InGameEventType.GameStart, new ScenarioEntry
            {
                Interrupt = true,
                Duration = 0f,
                TopPrompts = new[]
                {
                    new DialogueLine("系统", "开局时，所有玩家各抽一张牌。\n（揭晓你初始的命运……）", 0f),
                },
            });

            // #2 FirstDraw — 打断，4步
            RegisterEntry(InGameEventType.FirstDraw, new ScenarioEntry
            {
                Interrupt = true,
                Duration = 0f,
                TopPrompts = new[]
                {
                    new DialogueLine("系统", "所有玩家顺时针依次行动。\n（命运会轮流降临在每个人身上……）", 0f),
                    new DialogueLine("系统", "轮到你行动时，点击牌堆抽一张牌。\n（命运给予你选择的权力……）", 0f),
                    new DialogueLine("系统", "指向牌面，可以查看牌被打出后的效果。\n（不同的命运引发不同的因果……）", 0f),
                    new DialogueLine("系统", "然后，选中一张牌并将其拖入不存在同名牌的出牌区。\n（请选择命运与其沉眠之处……）", 0f),
                },
            });

            // #3 CursorOnCard — UI行为，无对话
            // #4 PlayerPlayACard
            RegisterEntry(InGameEventType.PlayerPlayACard, new ScenarioEntry
            {
                Duration = 3f,
                TopPrompts = new[]
                {
                    new DialogueLine("系统", "选中一张手牌并将其拖入不存在同名牌的出牌区。\n（请选择命运与其沉眠之处……）", 3f),
                },
            });

            // #5 CardIntoBoard — 视觉演出，无文案

            // #6 瘦子打到自己出牌区
            RegisterEntry(InGameEventType.CPUPlayCardSelf, new ScenarioEntry
            {
                Duration = 5f,
                BubbleSpeaker = "瘦子",
                RandomBubbles = new[]
                {
                    new DialogueLine("瘦子", "嘻嘻，我一定要赢呀！", 5f),
                    new DialogueLine("瘦子", "哇咿！", 5f),
                    new DialogueLine("瘦子", "胜利的血腥味……嘻嘻……", 5f),
                    new DialogueLine("瘦子", "会赢的……嘻嘻……", 5f),
                },
            });

            // #7 瘦子打到他人出牌区
            RegisterEntry(InGameEventType.CPUPlayCardOther, new ScenarioEntry
            {
                Duration = 5f,
                BubbleSpeaker = "瘦子",
                RandomBubbles = new[]
                {
                    new DialogueLine("瘦子", "喂你吃个大的！嘻嘻……", 5f),
                    new DialogueLine("瘦子", "呜哈！", 5f),
                    new DialogueLine("瘦子", "张嘴吃拖布！嘻嘻……", 5f),
                    new DialogueLine("瘦子", "嘻嘻，我一定要活下去呀！", 5f),
                },
            });

            // #8 胖子打到自己出牌区
            // 复用 CPUPlayCardSelf，通过 PlayerId 区分说话者
            // → 实际由 Monitor 触发时，InGameScenario 根据 PlayerId 选择瘦子/胖子台词
            // 为支持区分，使用两个独立事件类型
            // 但枚举已统一为 CPUPlayCardSelf/Other，这里通过 PlayerId 在 ResolveTemplates 中处理
            // 胖子台词需要单独注册 → 使用自定义 key
            RegisterFatGuyBubbles();

            // #10 CPUWin — 瘦子胜
            // #12 CPUWin — 胖子胜
            // 通过 PlayerId 区分，在 Monitor 中分别触发
            RegisterCPUWinDialogues();

            // #11 PlayerWin — 瘦子反应
            // #13 PlayerWin — 胖子反应
            RegisterPlayerWinDialogues();

            // #14 CloverEffect
            RegisterEntry(InGameEventType.CloverEffect, new ScenarioEntry
            {
                Duration = 3f,
                TopPrompts = new[]
                {
                    new DialogueLine("系统", "{出牌者名称}抽一张牌，将原手牌移回牌堆顶\n（幸运草交换了幸运与不幸……）", 3f),
                },
            });

            // #15.1 BoatEffectPlayer
            RegisterEntry(InGameEventType.BoatEffectPlayer, new ScenarioEntry
            {
                Duration = 3f,
                TopPrompts = new[]
                {
                    new DialogueLine("系统", "点击选择对手出牌区的一张牌，将其移回牌堆底。\n（命运之船将碾过它的对手……）", 3f),
                },
            });

            // #15.2 BoatEffectCPU
            RegisterEntry(InGameEventType.BoatEffectCPU, new ScenarioEntry
            {
                Duration = 3f,
                TopPrompts = new[]
                {
                    new DialogueLine("系统", "{对象名称}出牌区的一张牌移回牌堆底。\n（命运之船将碾过它的对手……）", 3f),
                },
            });

            // #16 HouseEffect
            RegisterEntry(InGameEventType.HouseEffect, new ScenarioEntry
            {
                Duration = 3f,
                TopPrompts = new[]
                {
                    new DialogueLine("系统", "{出牌者名称}免疫下一轮其他玩家对他的影响。\n（房子将抵御命运的恶意……或许）", 3f),
                },
            });

            // #17 HouseImmunityTriggered
            RegisterEntry(InGameEventType.HouseImmunityTriggered, new ScenarioEntry
            {
                Duration = 3f,
                TopPrompts = new[]
                {
                    new DialogueLine("系统", "{防御者名称}免疫了影响。\n（房子抵御了命运的恶意）", 3f),
                },
            });

            // #18 TreeEffect
            RegisterEntry(InGameEventType.TreeEffect, new ScenarioEntry
            {
                Duration = 3f,
                TopPrompts = new[]
                {
                    new DialogueLine("系统", "牌堆顶的牌被打入{对象名称}出牌区，且不触发效果（棺材除外）。\n（树隐藏了命运的威能……）", 3f),
                },
            });

            // #19.1 CloudEffectChoose
            RegisterEntry(InGameEventType.CloudEffectChoose, new ScenarioEntry
            {
                Duration = 3f,
                TopPrompts = new[]
                {
                    new DialogueLine("系统", "选择是否交换牌堆顶和牌堆底的牌。\n（云的恶作剧等待你的命令……）", 3f),
                },
            });

            // #19.2 CloudEffectSwapped
            RegisterEntry(InGameEventType.CloudEffectSwapped, new ScenarioEntry
            {
                Duration = 3f,
                TopPrompts = new[]
                {
                    new DialogueLine("系统", "{出牌者名称}交换了牌堆顶和牌堆底的牌。\n（云的恶作剧混淆了命运的始末……）", 3f),
                },
            });

            // #19.3 CloudEffectNotSwapped
            RegisterEntry(InGameEventType.CloudEffectNotSwapped, new ScenarioEntry
            {
                Duration = 3f,
                TopPrompts = new[]
                {
                    new DialogueLine("系统", "{出牌者名称}没有交换牌堆顶和牌堆底的牌。\n（云终止了它的恶作剧……）", 3f),
                },
            });

            // #20.1 SnakeEffectChoose
            RegisterEntry(InGameEventType.SnakeEffectChoose, new ScenarioEntry
            {
                Duration = 3f,
                TopPrompts = new[]
                {
                    new DialogueLine("系统", "选择交换手牌的对手。\n（蛇之手将互换彼此的命运……）", 3f),
                },
            });

            // #20.2 SnakeEffectDone
            RegisterEntry(InGameEventType.SnakeEffectDone, new ScenarioEntry
            {
                Duration = 3f,
                TopPrompts = new[]
                {
                    new DialogueLine("系统", "{出牌者名称}和{被换牌者名称}交换手牌。\n（蛇之手交换了彼此的命运……）", 3f),
                },
            });

            // #21 CoffinPlaced
            RegisterEntry(InGameEventType.CoffinPlaced, new ScenarioEntry
            {
                Duration = 3f,
                TopPrompts = new[]
                {
                    new DialogueLine("系统", "{被置入者名称}无法再出牌。\n（棺材令命运陷入沉眠……）", 3f),
                },
            });

            // #22 CoffinRemoved
            RegisterEntry(InGameEventType.CoffinRemoved, new ScenarioEntry
            {
                Duration = 3f,
                TopPrompts = new[]
                {
                    new DialogueLine("系统", "{被置入者名称}可以再次出牌。\n（命运从棺材中苏醒……）", 3f),
                },
            });

            // NearVictory
            RegisterEntry(InGameEventType.NearVictory, new ScenarioEntry
            {
                Duration = 3f,
                TopPrompts = new[]
                {
                    new DialogueLine("系统", "即将集齐全部卡牌！", 3f),
                },
            });

            // GameWon — 由 CPUWin/PlayerWin 覆盖
            RegisterEntry(InGameEventType.GameStart, _entries[InGameEventType.GameStart]);
        }

        // ── 胖子气泡注册 ─────────────────────────────────────

        private void RegisterFatGuyBubbles()
        {
            // 胖子打到自己出牌区 — 复用 CPUPlayCardSelf 但需区分说话者
            // 使用内部 key: CPUPlayCardSelf + PlayerId=2
            // 这里通过自定义事件类型实现
            RegisterEntry(InGameEventType.CPUPlayCardSelf, new ScenarioEntry
            {
                Duration = 5f,
                BubbleSpeaker = "瘦子", // 默认，Monitor 会根据 PlayerId 覆盖
                RandomBubbles = new[]
                {
                    new DialogueLine("瘦子", "嘻嘻，我一定要赢呀！", 5f),
                    new DialogueLine("瘦子", "哇咿！", 5f),
                    new DialogueLine("瘦子", "胜利的血腥味……嘻嘻……", 5f),
                    new DialogueLine("瘦子", "会赢的……嘻嘻……", 5f),
                },
            });

            // 胖子台词注册在自定义 key 中，Monitor 根据 PlayerId 选择
            _fatGuySelfBubbles = new DialogueLine[]
            {
                new DialogueLine("胖子", "老子的鲨鱼肉呢？", 5f),
                new DialogueLine("胖子", "老子要把你们碾成肉干！", 5f),
                new DialogueLine("胖子", "送上门的鲨鱼饵料！", 5f),
                new DialogueLine("胖子", "再来一口，再来一口！", 5f),
            };
            _fatGuyOtherBubbles = new DialogueLine[]
            {
                new DialogueLine("胖子", "他妈的，拿上牌给我滚！", 5f),
                new DialogueLine("胖子", "咳哼咳哼咳哼……", 5f),
                new DialogueLine("胖子", "老子不怕你们，不怕！", 5f),
                new DialogueLine("胖子", "可恶的小偷！", 5f),
            };
        }

        private DialogueLine[] _fatGuySelfBubbles;
        private DialogueLine[] _fatGuyOtherBubbles;

        /// <summary>获取胖子（CPU2）打到自己场地的随机气泡。</summary>
        public DialogueLine[] GetFatGuySelfBubbles() => _fatGuySelfBubbles;

        /// <summary>获取胖子（CPU2）打到他人场地的随机气泡。</summary>
        public DialogueLine[] GetFatGuyOtherBubbles() => _fatGuyOtherBubbles;

        // ── 胜负台词注册 ─────────────────────────────────────

        private void RegisterCPUWinDialogues()
        {
            // CPUWin — 通过 PlayerId 区分瘦子/胖子
            RegisterEntry(InGameEventType.CPUWin, new ScenarioEntry
            {
                Duration = 5f,
                BubbleSpeaker = "瘦子",
                RandomBubbles = new[]
                {
                    new DialogueLine("瘦子", "我胜了呀，嘻嘻嘻嘻……", 5f),
                },
            });

            _fatGuyWinBubble = new DialogueLine("胖子", "哼，算你们有良心。", 5f);
        }

        private DialogueLine _fatGuyWinBubble;

        /// <summary>获取胖子胜利台词。</summary>
        public DialogueLine GetFatGuyWinBubble() => _fatGuyWinBubble;

        private void RegisterPlayerWinDialogues()
        {
            // PlayerWin — 两个 CPU 依次反应
            RegisterEntry(InGameEventType.PlayerWin, new ScenarioEntry
            {
                Duration = 5f,
                BubbleSpeaker = "瘦子",
                RandomBubbles = new[]
                {
                    new DialogueLine("瘦子", "噫！我、我不要死呀——！", 5f),
                },
            });

            _fatGuyLoseBubble = new DialogueLine("胖子", "我的酒，我的肉，我的船——！", 5f);
        }

        private DialogueLine _fatGuyLoseBubble;

        /// <summary>获取胖子在玩家胜利时的反应台词。</summary>
        public DialogueLine GetFatGuyLoseBubble() => _fatGuyLoseBubble;

        /// <summary>
        /// 触发胖子专用气泡（Monitor 根据 PlayerId=2 调用）。
        /// </summary>
        public void TriggerFatGuyBubble(InGameEventType baseType, InGameEvent evt)
        {
            DialogueLine[] bubbles = baseType switch
            {
                InGameEventType.CPUPlayCardSelf => _fatGuySelfBubbles,
                InGameEventType.CPUPlayCardOther => _fatGuyOtherBubbles,
                InGameEventType.CPUWin => new[] { _fatGuyWinBubble },
                _ => null,
            };

            if (bubbles == null || bubbles.Length == 0) return;

            int idx = PickRandomIndex(baseType, bubbles.Length);
            var bubble = bubbles[idx];
            _currentBubble = ResolveTemplates(bubble, evt);
            _bubbleTimer = bubble.duration > 0 ? bubble.duration : _defaultDuration;
        }

        /// <summary>
        /// 触发玩家胜利时胖子的反应气泡（排队在瘦子反应之后）。
        /// </summary>
        public void TriggerFatGuyLoseBubble()
        {
            var evt = new InGameEvent(InGameEventType.PlayerWin) { PlayerId = 0 };
            _currentBubble = ResolveTemplates(_fatGuyLoseBubble, evt);
            _bubbleTimer = _fatGuyLoseBubble.duration > 0 ? _fatGuyLoseBubble.duration : _defaultDuration;
        }

        /// <summary>
        /// 将胖子反应气泡排入队列（瘦子反应之后播放）。
        /// 由 InGameEventMonitor.OnGameWon 调用。
        /// </summary>
        public void EnqueueFatGuyLoseBubble()
        {
            var evt = new InGameEvent(InGameEventType.PlayerWin) { PlayerId = 0 };
            _eventQueue.Enqueue(evt);
            // 标记此事件需要使用胖子台词
            // ProcessQueue 会查 _entries[PlayerWin] 得到瘦子台词，
            // 瘦子播完后队列中还有此事件 → 但类型相同会重复瘦子台词
            // 所以直接覆盖为胖子台词
            if (!IsBubbleActive && !IsInterruptActive)
                ProcessQueue();
        }
    }
}
