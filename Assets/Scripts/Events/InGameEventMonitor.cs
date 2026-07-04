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

        // 追踪 Boat 打出前目标玩家是否有 Coffin
        private int _boatTargetPlayerId = -1;
        private bool _boatTargetHadCoffin = false;

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
            CardBase.OnEffectBlocked += OnEffectBlocked;
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
            CardBase.OnEffectBlocked -= OnEffectBlocked;
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
            if (!_gameStartTriggered && _turnManager.Round == 0 && playerId == 0)
            {
                _gameStartTriggered = true;
                _scenario.TriggerEvent(new InGameEvent(InGameEventType.GameStart) { PlayerId = playerId });
            }
        }

        private void OnCardDrawn(int playerId, CardBase card)
        {
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
                bool willEndGame = false;
                if (card.GoesToBoardAfterPlay && targetBoardId >= 0)
                {
                    int uniqueCount = WinChecker.GetUniqueCardCount(_context, targetBoardId);
                    if (!_context.Board.HasCardWithId(targetBoardId, card.Id))
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

            // Boat 打出前记录目标玩家是否有 Coffin
            if (card.Id == 3)
            {
                // 无法在此获取 EffectTarget（OnCardPlayed 不带它），在 OnPlayCompleted 中处理
            }

            // 卡牌效果事件
            TriggerCardEffectEvent(playerId, card, targetBoardId);

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

            // 船移除牌 → 检查是否移除了棺材
            if (card.Id == 3 && effectTarget?.TargetPlayerId != null)
            {
                int targetId = effectTarget.TargetPlayerId.Value;
                // 如果目标玩家现在没有棺材，可能被移除了
                // 更精确：检查牌堆底最后一张是否是棺材
                if (!_context.Board.HasCardWithId(targetId, 8))
                {
                    // 牌刚被放入牌堆底，检查 DeckManager.PeekBottom
                    var bottomCard = _context.Deck.PeekBottom();
                    if (bottomCard != null && bottomCard.Id == 8)
                    {
                        _scenario.TriggerEvent(new InGameEvent(InGameEventType.CoffinRemoved)
                        {
                            PlayerId = targetId,
                            CardId = 8,
                            CardName = "棺材",
                        });
                    }
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
        }

        private void OnGameWon(int winnerId)
        {
            var winner = _context.GetPlayer(winnerId);

            if (winner != null && winner.IsCPU)
            {
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
                _scenario.TriggerEvent(new InGameEvent(InGameEventType.PlayerWin)
                {
                    PlayerId = 0,
                });
                _scenario.EnqueueFatGuyLoseBubble();
            }
        }

        // ── 免疫触发 ──────────────────────────────────────────

        private void OnEffectBlocked(int targetPlayerId, int sourcePlayerId)
        {
            _scenario.TriggerEvent(new InGameEvent(InGameEventType.HouseImmunityTriggered)
            {
                PlayerId = targetPlayerId,
                TargetPlayerId = sourcePlayerId,
            });
        }

        // ── 卡牌效果事件 ──────────────────────────────────────

        private void TriggerCardEffectEvent(int playerId, CardBase card, int targetBoardId)
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
    }
}
