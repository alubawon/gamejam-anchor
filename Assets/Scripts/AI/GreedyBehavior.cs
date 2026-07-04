using System.Collections.Generic;

namespace CardGame
{
    /// <summary>
    /// 贪婪策略 — 优先填满自己场地，并拆场地最满的对手场地。
    /// <para>1. 手中有船(3) → 用船移除场地最满对手的最高价值牌，船优先放自己场地</para>
    /// <para>2. 无船或无法使用 → fallback 到 BaseBehavior（填自己场地）</para>
    /// </summary>
    public class GreedyBehavior : BaseBehavior
    {
        public override PlayAction Decide(IGameContext context, int playerId)
        {
            var player = context.GetPlayer(playerId);
            if (player == null || player.Hand.Count == 0) return null;

            // 1. 尝试用船拆场地最满的对手
            var boatAction = TryBoatOnFullestOpponent(context, playerId);
            if (boatAction != null) return boatAction;

            // 2. Fallback: 填自己场地 → 随机合法
            return base.Decide(context, playerId);
        }

        private PlayAction TryBoatOnFullestOpponent(IGameContext context, int playerId)
        {
            var player = context.GetPlayer(playerId);
            var boat = FindCard(player.Hand, 3);
            if (boat == null) return null;

            // 找到场地最满的对手
            int? fullestId = null;
            int maxCount = 0;
            foreach (var p in context.Players)
            {
                if (p.Id == playerId) continue;
                var board = context.Board.GetPlayerBoard(p.Id);
                if (board.Count > maxCount)
                {
                    maxCount = board.Count;
                    fullestId = p.Id;
                }
            }

            if (fullestId == null || maxCount == 0) return null;

            int targetId = fullestId.Value;
            var targetPlayer = context.GetPlayer(targetId);
            if (targetPlayer == null || targetPlayer.IsImmune) return null;

            // 选择移除最高价值的牌（最大 disruption）
            var targetBoard = context.Board.GetPlayerBoard(targetId);
            int bestIdx = 0;
            int bestValue = targetBoard[0].Id;
            for (int i = 1; i < targetBoard.Count; i++)
            {
                if (targetBoard[i].Id > bestValue)
                {
                    bestValue = targetBoard[i].Id;
                    bestIdx = i;
                }
            }

            // 确定船放置的目标场地：优先自己场地
            int boatTarget = playerId;
            if (context.Board.HasCardWithId(playerId, 3))
            {
                // 自己场地已有船 → 找其他合法场地
                var validTargets = GetValidTargets(context, 3);
                if (validTargets.Count == 0) return null;
                boatTarget = validTargets[_rng.Next(validTargets.Count)];
            }

            var effectTarget = new PlayTarget
            {
                TargetPlayerId = targetId,
                TargetCardIndex = bestIdx,
            };
            return new PlayAction(boat, boatTarget, effectTarget);
        }
    }
}
