using System;
using System.Collections.Generic;

namespace CardGame
{
    /// <summary>
    /// 局中事件监视器 — 订阅 TurnManager 事件，检测特定操作，触发 InGameScenario 气泡演出。
    /// <para>对话内容对应 event_scenario.csv 中定义的全部事件。</para>
    /// </summary>
    public class InGameEventMonitor
    {
        private readonly TurnManager _turnManager;
        private readonly IGameContext _context;
        private readonly InGameScenario _scenario;

        private const int NearVictoryThreshold = 6;
        private readonly HashSet<int> _nearVictoryTriggered = new();
        private bool _monitoring;
        private bool _firstDrawTriggered;
        private bool _gameStartTriggered;

        public InGameEventMonitor(
            TurnManager turnManager,
            IGameContext context,
            InGameScenario scenario)
        {
            _turnManager = turnManager ?? throw new ArgumentNullException(nameof(turnManager));
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _scenario = scenario ?? throw new ArgumentNullException(nameof(scenario));
        }

        /// <summary>开始监视 — 订阅 TurnManager 事件。</summary>
        public void StartMonitoring()
        {
            if (_monitoring) return;
            _monitoring = true;
            _nearVictoryTriggered.Clear();
            _firstDrawTriggered = false;
            _gameStartTriggered = false;

            _turnManager.OnTurnStart += OnTurnStart;
            _turnManager.OnCardDrawn += OnCardDrawn;
            _turnManager.OnCardPlayed += OnCardPlayed;
            _turnManager.OnPlayCompleted += OnPlayCompleted;
            _turnManager.OnCheckBoardComplete += OnCheckBoardComplete;
            _turnManager.OnGameWon += OnGameWon;
        }

        /// <summary>停止监视 — 取消订阅。</summary>
        public void StopMonitoring()
        {
            if (!_monitoring) return;
            _monitoring = false;

            _turnManager.OnTurnStart -= OnTurnStart;
            _turnManager.OnCardDrawn -= OnCardDrawn;
            _turnManager.OnCardPlayed -= OnCardPlayed;
            _turnManager.OnPlayCompleted -= OnPlayCompleted;
            _turnManager.OnCheckBoardComplete -= OnCheckBoardComplete;
            _turnManager.OnGameWon -= OnGameWon;
        }

        /// <summary>重置监视状态（新一局时调用）。</summary>
        public void Reset()
        {
            _nearVictoryTriggered.Clear();
            _firstDrawTriggered = false;
            _gameStartTriggered = false;
        }

        // ── 事件处理 ──────────────────────────────────────────

        private void OnTurnStart(int playerId)
        {
            // 第一回合开始时触发 GameStart
            if (!_gameStartTriggered && _turnManager.Round == 0 && playerId == 0)
            {
                _gameStartTriggered = true;
                _scenario.TriggerEvent(new InGameEvent(InGameEventType.GameStart) { PlayerId = playerId });
            }
        }

        private void OnCardDrawn(int playerId, CardBase card)
        {
            // 玩家先手第一次抽牌 → FirstDraw 教学
            if (!_firstDrawTriggered && playerId == 0 && _turnManager.Round == 0)
            {
                _firstDrawTriggered = true;
                _scenario.TriggerEvent(new InGameEvent(InGameEventType.FirstDraw) { PlayerId = playerId });
            }
        }

        private void OnCardPlayed(int playerId, CardBase card, int targetBoardId)
        {
            var player = _context.GetPlayer(playerId);

            // #4 PlayerPlayACard — 玩家打牌提示
            if (player != null && !player.IsCPU)
            {
                _scenario.TriggerEvent(new InGameEvent(InGameEventType.PlayerPlayACard)
                {
                    PlayerId = playerId,
                    CardId = card.Id,
                    CardName = card.Name,
                });
            }

            // #5 CardIntoBoard — 任何人打出牌后
            _scenario.TriggerEvent(new InGameEvent(InGameEventType.CardIntoBoard)
            {
                PlayerId = playerId,
                CardId = card.Id,
                CardName = card.Name,
                TargetPlayerId = targetBoardId,
            });

            // #6-9 CPU 气泡 — 不在游戏结束前触发
            if (player != null && player.IsCPU)
            {
                // 判断是否会导致游戏结束（检查打出后是否集齐）
                bool willEndGame = false;
                if (card.GoesToBoardAfterPlay && targetBoardId >= 0)
                {
                    // 粗略检查：打出后该场地不同牌数是否达到7
                    int uniqueCount = WinChecker.GetUniqueCardCount(_context, targetBoardId);
                    if (!context.Board.HasCardWithId(targetBoardId, card.Id))
                        uniqueCount++;
                    willEndGame = uniqueCount >= 7;
                }

                if (!willEndGame)
                {
                    var bubbleType = targetBoardId == playerId
                        ? InGameEventType.CPUPlayCardSelf
                        : InGameEventType.CPUPlayCardOther;

                    var evt = new InGameEvent(bubbleType)
                    {
                        PlayerId = playerId,
                        CardId = card.Id,
                        CardName = card.Name,
                        TargetPlayerId = targetBoardId,
                    };

                    // 胖子 (PlayerId=2) 使用专用台词
                    if (playerId == 2)
                        _scenario.TriggerFatGuyBubble(bubbleType, evt);
                    else
                        _scenario.TriggerEvent(evt);
                }
            }

            // #21 CoffinPlaced
            if (card.Id == 8 && card.GoesToBoardAfterPlay)
            {
                _scenario.TriggerEvent(new InGameEvent(InGameEventType.CoffinPlaced)
                {
                    PlayerId = targetBoardId,
                    CardId = card.Id,
                    CardName = card.Name,
                });
            }

            // 卡牌效果事件 — 根据 card.Id 触发对应子流程
            TriggerCardEffectEvent(playerId, card, targetBoardId, null);

            // 检查接近胜利
            CheckNearVictory();
        }

        private void OnPlayCompleted(int playerId, CardBase card, int targetBoardId, PlayTarget effectTarget)
        {
            // 云的交换结果
            if (card.Id == 6)
            {
                var cloudType = effectTarget?.Choice == true
                    ? InGameEventType.CloudEffectSwapped
                    : InGameEventType.CloudEffectNotSwapped;
                _scenario.TriggerEvent(new InGameEvent(cloudType)
                {
                    PlayerId = playerId,
                    CardId = card.Id,
                    CardName = card.Name,
                });
            }

            // 蛇交换完成
            if (card.Id == 7 && effectTarget?.TargetPlayerId != null)
            {
                _scenario.TriggerEvent(new InGameEvent(InGameEventType.SnakeEffectDone)
                {
                    PlayerId = playerId,
                    TargetPlayerId = effectTarget.TargetPlayerId.Value,
                    CardId = card.Id,
                    CardName = card.Name,
                });
            }

            // 船移除棺材 → CoffinRemoved
            if (card.Id == 3 && effectTarget?.TargetPlayerId != null && effectTarget.TargetCardIndex >= 0)
            {
                // 检查被移除的牌是否是棺材（在 OnPlay 中已被移除，无法直接检查）
                // 这里通过 OnPlayCompleted 的上下文推断：如果目标玩家之前有棺材但现在没有
                if (!context.Board.HasCardWithId(effectTarget.TargetPlayerId.Value, 8))
                {
                    // 可能是棺材被移除了（也可能本来就没有）
                    // 更精确的判断需要在 OnPlay 前记录，这里简化处理
                }
            }
        }

        private void OnCheckBoardComplete(int playerId)
        {
            var player = _context.GetPlayer(playerId);
            if (player != null && !player.CanPlay)
            {
                _scenario.TriggerEvent(new InGameEvent(InGameEventType.CoffinPlaced)
                {
                    PlayerId = playerId,
                });
            }

            // 检查房子免疫触发
            // 当其他玩家对有 House 的玩家使用效果时，免疫被触发
            // 这里简化处理：在 CheckBoard 时如果玩家 IsImmune 但不是本回合刚放的 House
            // 更精确的免疫触发检测需要在 Boat/Snake 的 OnPlay 中处理
        }

        private void OnGameWon(int winnerId)
        {
            var winner = _context.GetPlayer(winnerId);

            if (winner != null && winner.IsCPU)
            {
                // CPU 胜利
                if (winnerId == 2)
                    _scenario.TriggerFatGuyBubble(InGameEventType.CPUWin,
                        new InGameEvent(InGameEventType.CPUWin) { PlayerId = winnerId });
                else
                    _scenario.TriggerEvent(new InGameEvent(InGameEventType.CPUWin)
                    {
                        PlayerId = winnerId,
                    });
            }
            else
            {
                // 玩家胜利 → 两个 CPU 依次反应
                _scenario.TriggerEvent(new InGameEvent(InGameEventType.PlayerWin)
                {
                    PlayerId = 0,
                });
                // 胖子反应排在瘦子之后（通过队列）
                _scenario.EnqueueFatGuyLoseBubble();
            }
        }

        // ── 卡牌效果事件 ──────────────────────────────────────

        private void TriggerCardEffectEvent(int playerId, CardBase card, int targetBoardId, PlayTarget effectTarget)
        {
            var player = _context.GetPlayer(playerId);
            bool isCPU = player != null && player.IsCPU;

            InGameEventType? type = card.Id switch
            {
                2 => InGameEventType.CloverEffect,
                3 => isCPU ? InGameEventType.BoatEffectCPU : InGameEventType.BoatEffectPlayer,
                4 => InGameEventType.HouseEffect,
                5 => InGameEventType.TreeEffect,
                6 => isCPU ? InGameEventType.CloudEffectSwapped : InGameEventType.CloudEffectChoose,
                7 => isCPU ? InGameEventType.SnakeEffectDone : InGameEventType.SnakeEffectChoose,
                _ => null,
            };

            if (type.HasValue)
            {
                _scenario.TriggerEvent(new InGameEvent(type.Value)
                {
                    PlayerId = playerId,
                    TargetPlayerId = targetBoardId,
                    CardId = card.Id,
                    CardName = card.Name,
                });
            }
        }

        // ── 条件检测 ──────────────────────────────────────────

        private void CheckNearVictory()
        {
            foreach (var player in _context.Players)
            {
                int count = WinChecker.GetUniqueCardCount(_context, player.Id);
                if (count >= NearVictoryThreshold && !_nearVictoryTriggered.Contains(player.Id))
                {
                    _nearVictoryTriggered.Add(player.Id);
                    _scenario.TriggerEvent(new InGameEvent(InGameEventType.NearVictory)
                    {
                        PlayerId = player.Id,
                        BoardUniqueCount = count,
                    });
                }
            }
        }

        // 引用外部 context（用于 OnPlayCompleted 中检查）
        private IGameContext context => _context;
    }
}
