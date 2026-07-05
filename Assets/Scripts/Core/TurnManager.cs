using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CardGame
{
    /// <summary>
    /// 回合管理 — 驱动游戏主流程。
    /// <para>流程：StartTurn → CheckBoard → Draw → (等待出牌) → TryPlay → EndTurn → 下一位</para>
    /// <para>GameManager 在系统层按序调用各阶段方法，并在 Draw 与 TryPlay 之间处理玩家输入/CPU决策。</para>
    /// </summary>
    public class TurnManager
    {
        private readonly int[] _playerOrder;
        private int _currentIndex;

        /// <summary>当前回合玩家 ID。</summary>
        public int CurrentPlayerId => _playerOrder[_currentIndex];

        /// <summary>当前是第几轮（从 0 开始，每完整循环一轮 +1）。</summary>
        public int Round { get; private set; }

        // ── 事件（供 View 层监听）──────────────────────────────

        /// <summary>回合开始 (playerId)。</summary>
        public event Action<int> OnTurnStart;

        /// <summary>场地检查完成 (playerId)。</summary>
        public event Action<int> OnCheckBoardComplete;

        /// <summary>玩家摸牌 (playerId, card)。card 为 null 表示牌堆已空。</summary>
        public event Action<int, CardBase> OnCardDrawn;

        /// <summary>卡牌被打出到场地 (playerId, card, targetBoardId)。</summary>
        public event Action<int, CardBase, int> OnCardPlayed;

        /// <summary>卡牌效果结算完成 (playerId, card)。</summary>
        public event Action<int, CardBase> OnEffectResolved;

        /// <summary>出牌完整完成 (playerId, card, targetBoardId, effectTarget)。在效果结算和胜负检查之后触发。</summary>
        public event Action<int, CardBase, int, PlayTarget> OnPlayCompleted;

        /// <summary>回合结束 (playerId)。</summary>
        public event Action<int> OnTurnEnd;

        /// <summary>游戏结束，有玩家获胜 (winnerId)。</summary>
        public event Action<int> OnGameWon;

        // ── 生命周期 ──────────────────────────────────────────

        /// <param name="playerOrder">出牌顺序的玩家 ID 数组，首个为起始玩家。</param>
        public TurnManager(int[] playerOrder)
        {
            _playerOrder = playerOrder ?? throw new ArgumentNullException(nameof(playerOrder));
            _currentIndex = 0;
            Round = 0;
        }

        /// <summary>
        /// 初始化游戏：构建牌堆、洗牌、初始化场地、每人发 1 张手牌。
        /// </summary>
        /// <param name="context">游戏上下文。</param>
        /// <param name="copiesPerCard">每种牌的份数（默认 5 → 35 张）。</param>
        public void StartGame(IGameContext context, int copiesPerCard = 5)
        {
            // 构建牌堆
            var cards = CardFactory.CreateDeck(copiesPerCard);
            context.Deck.BuildDeck(cards);

            // 棺材特殊洗牌规则：
            // 1. 分离棺材牌和非棺材牌
            // 2. 只保留1张棺材参与正常洗牌
            // 3. 洗牌后将剩余棺材牌随机插入牌堆后三分之二位置
            var coffins = new List<CardBase>();
            var nonCoffins = new List<CardBase>();
            foreach (var c in cards)
            {
                if (c.Id == 8)
                    coffins.Add(c);
                else
                    nonCoffins.Add(c);
            }

            // 保留1张棺材参与洗牌
            var rng = new System.Random();
            var shuffledCoffin = coffins[0];
            var extraCoffins = coffins.Skip(1).ToList();

            // 合并非棺材 + 1张棺材 → 洗牌
            var shufflePool = new List<CardBase>(nonCoffins) { shuffledCoffin };
            // Fisher-Yates 洗牌
            for (int i = shufflePool.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (shufflePool[i], shufflePool[j]) = (shufflePool[j], shufflePool[i]);
            }

            // 构建牌堆
            context.Deck.BuildDeck(shufflePool);

            // 将剩余棺材牌随机插入牌堆后三分之二位置
            int deckCount = context.Deck.Count;
            int insertStart = deckCount / 3; // 后三分之一的起点
            foreach (var coffin in extraCoffins)
            {
                int insertPos = insertStart + rng.Next(deckCount - insertStart + 1);
                insertPos = Mathf.Min(insertPos, context.Deck.Count);
                context.Deck.InsertAt(insertPos, coffin);
            }

            // 初始化场地
            context.Board.ClearAll();
            foreach (var player in context.Players)
                context.Board.InitPlayer(player.Id);

            // 重置玩家状态
            foreach (var player in context.Players)
            {
                player.Hand.Clear();
                player.Score = 0;
                player.IsImmune = false;
                player.CanPlay = true;
            }

            // 每人发 1 张手牌
            foreach (var player in context.Players)
            {
                var card = context.Deck.DrawCard();
                if (card != null)
                    player.Hand.Add(card);
            }

            _currentIndex = 0;
            Round = 0;
        }

        // ── 回合阶段 ──────────────────────────────────────────

        /// <summary>
        /// 阶段 1：回合开始 — 重置当前玩家的免疫标记。
        /// </summary>
        public void StartTurn(IGameContext context)
        {
            int playerId = CurrentPlayerId;
            var player = context.GetPlayer(playerId);
            if (player != null)
                player.IsImmune = false;

            context.CurrentPlayerId = playerId;
            OnTurnStart?.Invoke(playerId);
        }

        /// <summary>
        /// 阶段 2：检查场地 — 结算所有在场效果（Coffin 禁止出牌等）。
        /// </summary>
        public void CheckBoard(IGameContext context)
        {
            context.Board.CheckAllBoards(context);
            OnCheckBoardComplete?.Invoke(CurrentPlayerId);
        }

        /// <summary>
        /// 阶段 3：摸牌 — 当前玩家从牌堆顶摸 1 张。
        /// <para>若 CanPlay 为 false（Coffin 在场）则不摸牌。</para>
        /// <para>若牌堆已空，触发保底逻辑：当前玩家直接失败（对手获胜）。</para>
        /// </summary>
        /// <returns>摸到的卡牌；未摸到返回 null。</returns>
        public CardBase Draw(IGameContext context)
        {
            int playerId = CurrentPlayerId;
            var player = context.GetPlayer(playerId);
            if (player == null || !player.CanPlay)
                return null;

            // 牌堆为空 → 保底：当前玩家失败，第一个对手获胜
            if (context.Deck.Count <= 0)
            {
                int winnerId = -1;
                foreach (var p in context.Players)
                {
                    if (p.Id != playerId) { winnerId = p.Id; break; }
                }
                if (winnerId >= 0)
                {
                    OnPlayCompleted?.Invoke(playerId, null, -1, null);
                    OnGameWon?.Invoke(winnerId);
                }
                return null;
            }

            var card = context.Deck.DrawCard();
            if (card != null)
                player.Hand.Add(card);

            OnCardDrawn?.Invoke(playerId, card);
            return card;
        }

        /// <summary>
        /// 阶段 4：出牌 — 验证并执行出牌动作。
        /// <para>包括：从手牌移除 → 放入目标场地 → 触发卡牌效果 → 检查胜负。</para>
        /// </summary>
        /// <param name="context">游戏上下文。</param>
        /// <param name="action">出牌动作。</param>
        /// <returns>true 表示出牌成功。</returns>
        public bool TryPlay(IGameContext context, PlayAction action)
        {
            if (!ValidatePlay(context, action, out string error))
                return false;

            int playerId = CurrentPlayerId;
            var player = context.GetPlayer(playerId);

            // 从手牌移除
            player.Hand.Remove(action.Card);

            // 放入目标场地（GoesToBoardAfterPlay = false 的牌不进场地）
            if (action.Card.GoesToBoardAfterPlay)
                context.Board.PlayCard(action.TargetBoardId, action.Card);

            OnCardPlayed?.Invoke(playerId, action.Card, action.TargetBoardId);

            // 触发卡牌效果
            if (!action.Card.EffectsSuppressed)
                action.Card.OnPlay(context, playerId, action.EffectTarget);

            OnEffectResolved?.Invoke(playerId, action.Card);

            // 检查胜负
            int winnerId = WinChecker.CheckWin(context);
            if (winnerId >= 0)
            {
                OnPlayCompleted?.Invoke(playerId, action.Card, action.TargetBoardId, action.EffectTarget);
                OnGameWon?.Invoke(winnerId);
                return true;
            }

            OnPlayCompleted?.Invoke(playerId, action.Card, action.TargetBoardId, action.EffectTarget);
            return true;
        }

        /// <summary>
        /// 阶段 5：回合结束 — 推进到下一位玩家。
        /// </summary>
        public void EndTurn()
        {
            int playerId = CurrentPlayerId;
            OnTurnEnd?.Invoke(playerId);

            _currentIndex = (_currentIndex + 1) % _playerOrder.Length;
            if (_currentIndex == 0)
                Round++;
        }

        // ── 验证 & 辅助 ───────────────────────────────────────

        /// <summary>
        /// 验证出牌动作是否合法。
        /// </summary>
        public bool ValidatePlay(IGameContext context, PlayAction action, out string error)
        {
            error = null;
            int playerId = CurrentPlayerId;
            var player = context.GetPlayer(playerId);

            if (player == null)
            {
                error = "玩家不存在";
                return false;
            }

            if (!player.CanPlay)
            {
                error = "玩家无法出牌（棺材效果）";
                return false;
            }

            if (action?.Card == null)
            {
                error = "出牌动作为空";
                return false;
            }

            // 卡牌必须在手牌中
            if (!player.Hand.Contains(action.Card))
            {
                error = "该牌不在玩家手中";
                return false;
            }

            // 棺材只能打向自己场地
            if (action.Card.Id == 8 && action.TargetBoardId != playerId)
            {
                error = "棺材必须打进自己面前";
                return false;
            }

            // 棺材第三张限制：场上已有两张棺材时，只能在自己场地已集齐 2～7 所有牌时打出
            if (action.Card.Id == 8)
            {
                int coffinCount = 0;
                foreach (var p in context.Players)
                {
                    if (context.Board.HasCardWithId(p.Id, 8))
                        coffinCount++;
                }
                if (coffinCount >= 2)
                {
                    // 检查自己场地是否已有 2～7 全部六张牌
                    bool hasAllOthers = true;
                    for (int id = 2; id <= 7; id++)
                    {
                        if (!context.Board.HasCardWithId(playerId, id))
                        {
                            hasAllOthers = false;
                            break;
                        }
                    }
                    if (!hasAllOthers)
                    {
                        error = "场上已有两张棺材，需在自己场地集齐2～7所有牌后才能打出第三张棺材";
                        return false;
                    }
                }
            }

            // 目标场地不能有相同点数的牌（仅对进入场地的牌检查）
            if (action.Card.GoesToBoardAfterPlay &&
                context.Board.HasCardWithId(action.TargetBoardId, action.Card.Id))
            {
                error = "目标场地已存在相同点数的牌";
                return false;
            }

            return true;
        }

        /// <summary>
        /// 当前玩家是否必须打出棺材（手中有棺材且可以出牌）。
        /// </summary>
        public bool MustPlayCoffin(IGameContext context)
        {
            var player = context.GetPlayer(CurrentPlayerId);
            if (player == null || !player.CanPlay)
                return false;

            // 自己场地没有棺材时，手牌中的棺材必须优先打出
            if (context.Board.HasCardWithId(CurrentPlayerId, 8))
                return false; // 场上已有棺材 → CanPlay 已为 false，不会走到这里

            foreach (var card in player.Hand)
            {
                if (card.Id == 8)
                    return true;
            }
            return false;
        }

        /// <summary>
        /// 获取一张卡牌的合法放置目标（场地无同点数牌的玩家 ID 列表）。
        /// </summary>
        public List<int> GetValidTargets(IGameContext context, int cardId)
        {
            var targets = new List<int>();
            foreach (var player in context.Players)
            {
                if (!context.Board.HasCardWithId(player.Id, cardId))
                    targets.Add(player.Id);
            }
            return targets;
        }

        /// <summary>当前玩家是否可以出牌（CanPlay 且手牌非空）。</summary>
        public bool CanCurrentPlayerPlay(IGameContext context)
        {
            var player = context.GetPlayer(CurrentPlayerId);
            return player != null && player.CanPlay && player.Hand.Count > 0;
        }
    }
}
