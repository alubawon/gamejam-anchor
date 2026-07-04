using System.Collections.Generic;

namespace CardGame
{
    /// <summary>
    /// 记仇策略 — 优先拆人类玩家的场地。
    /// <para>1. 手中有船(3) → 用船移除人类玩家场地的最高价值牌，船优先放自己场地</para>
    /// <para>2. 无船或无法使用 → fallback 到 BaseBehavior</para>
    /// </summary>
    public class HatefulBehavior : BaseBehavior
    {
        public override PlayAction Decide(IGameContext context, int playerId)
        {
            var player = context.GetPlayer(playerId);
            if (player == null || player.Hand.Count == 0) return null;

            // 1. 尝试用船拆人类玩家的场地
            var boatAction = TryBoatOnHumanPlayer(context, playerId);
            if (boatAction != null) return boatAction;

            // 2. Fallback: 填自己场地 → 随机合法
            return base.Decide(context, playerId);
        }

        private PlayAction TryBoatOnHumanPlayer(IGameContext context, int playerId)
        {
            var player = context.GetPlayer(playerId);
            var boat = FindCard(player.Hand, 3);
            if (boat == null) return null;

            // 找到人类玩家（IsCPU == false）
            int? humanId = null;
            foreach (var p in context.Players)
            {
                if (!p.IsCPU) { humanId = p.Id; break; }
            }

            if (humanId == null) return null;

            int targetId = humanId.Value;
            var targetPlayer = context.GetPlayer(targetId);
            if (targetPlayer == null || targetPlayer.IsImmune) return null;

            var targetBoard = context.Board.GetPlayerBoard(targetId);
            if (targetBoard.Count == 0) return null;

            // 选择移除最高价值的牌
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
