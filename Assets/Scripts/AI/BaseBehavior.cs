using System.Collections.Generic;

namespace CardGame
{
    /// <summary>
    /// 基础出牌策略 — 所有策略的 fallback。
    /// <para>规则：</para>
    /// <para>1. 手牌中自己场地上没有的牌 → 打到自己的场地（优先填满自己）</para>
    /// <para>2. 若两张都没有（都不在自己场地上）→ 打出点数最小的</para>
    /// <para>3. 若两张都有了（都在自己场地上）→ 随机打一张到随机合法场地</para>
    /// </summary>
    public class BaseBehavior : IBehaviorStrategy
    {
        protected readonly System.Random _rng = new System.Random();

        public virtual PlayAction Decide(IGameContext context, int playerId)
        {
            var player = context.GetPlayer(playerId);
            if (player == null || player.Hand.Count == 0) return null;

            var boardIds = GetBoardCardIds(context, playerId);

            // 分类：不在自己场地上的 / 已在自己场地上的
            var missing = new List<CardBase>();
            var present = new List<CardBase>();
            foreach (var card in player.Hand)
            {
                if (boardIds.Contains(card.Id))
                    present.Add(card);
                else
                    missing.Add(card);
            }

            // 1 & 2: 有不在自己场地上的牌 → 打到自己的场地
            if (missing.Count > 0)
            {
                CardBase chosen;
                if (missing.Count == 1)
                {
                    chosen = missing[0];
                }
                else
                {
                    // 两张都没有 → 打出点数最小的
                    chosen = missing[0];
                    foreach (var c in missing)
                        if (c.Id < chosen.Id) chosen = c;
                }
                return new PlayAction(chosen, playerId);
            }

            // 3: 两张都在自己场地上 → 随机打一张到随机合法场地
            if (present.Count > 0)
            {
                // 随机排序
                var shuffled = new List<CardBase>(present);
                for (int i = shuffled.Count - 1; i > 0; i--)
                {
                    int j = _rng.Next(i + 1);
                    (shuffled[i], shuffled[j]) = (shuffled[j], shuffled[i]);
                }

                foreach (var card in shuffled)
                {
                    var validTargets = GetValidTargets(context, card.Id);
                    if (validTargets.Count > 0)
                    {
                        int target = validTargets[_rng.Next(validTargets.Count)];
                        return new PlayAction(card, target);
                    }
                }
            }

            return null;
        }

        // ── 辅助方法（供子类复用）──────────────────────────────

        /// <summary>获取玩家场地上所有卡牌的 ID 集合。</summary>
        protected HashSet<int> GetBoardCardIds(IGameContext context, int playerId)
        {
            var board = context.Board.GetPlayerBoard(playerId);
            var ids = new HashSet<int>();
            foreach (var card in board)
                ids.Add(card.Id);
            return ids;
        }

        /// <summary>获取一张卡牌的所有合法放置目标（场地无同点数牌的玩家 ID）。</summary>
        protected List<int> GetValidTargets(IGameContext context, int cardId)
        {
            var targets = new List<int>();
            foreach (var p in context.Players)
            {
                if (!context.Board.HasCardWithId(p.Id, cardId))
                    targets.Add(p.Id);
            }
            return targets;
        }

        /// <summary>在手牌中查找指定 ID 的卡牌。</summary>
        protected CardBase FindCard(IReadOnlyList<CardBase> hand, int cardId)
        {
            foreach (var card in hand)
                if (card.Id == cardId) return card;
            return null;
        }
    }
}
