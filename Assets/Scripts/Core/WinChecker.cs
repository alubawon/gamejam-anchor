using System.Collections.Generic;

namespace CardGame
{
    /// <summary>
    /// 胜负判定 — 检查玩家场地是否集齐 2～8 全部七张牌。
    /// </summary>
    public static class WinChecker
    {
        /// <summary>获胜所需的全部卡牌 ID。</summary>
        public static readonly int[] RequiredCardIds = { 2, 3, 4, 5, 6, 7, 8 };

        /// <summary>
        /// 检查是否有玩家获胜。
        /// </summary>
        /// <returns>获胜玩家 ID；无人获胜返回 -1。</returns>
        public static int CheckWin(IGameContext context)
        {
            foreach (var player in context.Players)
            {
                if (HasAllCards(context, player.Id))
                    return player.Id;
            }
            return -1;
        }

        /// <summary>玩家场地是否集齐 2～8 全部七张牌。</summary>
        public static bool HasAllCards(IGameContext context, int playerId)
        {
            var board = context.Board.GetPlayerBoard(playerId);
            if (board == null || board.Count < RequiredCardIds.Length)
                return false;

            var ids = new HashSet<int>();
            foreach (var card in board)
                ids.Add(card.Id);

            foreach (var requiredId in RequiredCardIds)
            {
                if (!ids.Contains(requiredId))
                    return false;
            }
            return true;
        }

        /// <summary>玩家场地当前已集齐的不同卡牌数量（0～7）。</summary>
        public static int GetUniqueCardCount(IGameContext context, int playerId)
        {
            var board = context.Board.GetPlayerBoard(playerId);
            if (board == null) return 0;

            var ids = new HashSet<int>();
            foreach (var card in board)
                ids.Add(card.Id);
            return ids.Count;
        }
    }
}
