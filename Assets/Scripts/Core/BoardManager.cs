using System.Collections.Generic;

namespace CardGame
{
    /// <summary>
    /// 场地管理 — 管理每个玩家面前已打出的卡牌区域（3 名玩家：1 人 + 2 CPU）。
    /// <para>TurnManager 在出牌后将卡牌放入场地，在 checkBoard 阶段调用 CheckAllBoards。</para>
    /// </summary>
    public class BoardManager
    {
        private readonly Dictionary<int, List<CardBase>> _boards = new Dictionary<int, List<CardBase>>();

        /// <summary>初始化一个玩家的空场地。</summary>
        public void InitPlayer(int playerId)
        {
            if (!_boards.ContainsKey(playerId))
                _boards[playerId] = new List<CardBase>();
        }

        /// <summary>将卡牌放入玩家场地。</summary>
        public void PlayCard(int playerId, CardBase card)
        {
            EnsurePlayer(playerId);
            _boards[playerId].Add(card);
        }

        /// <summary>获取玩家场地上的所有卡牌（返回列表引用，外部不应直接修改）。</summary>
        public List<CardBase> GetPlayerBoard(int playerId)
        {
            EnsurePlayer(playerId);
            return _boards[playerId];
        }

        /// <summary>移除玩家场地上指定索引的卡牌并返回。</summary>
        public CardBase RemoveCard(int playerId, int index)
        {
            if (!_boards.ContainsKey(playerId)) return null;
            var board = _boards[playerId];
            if (index < 0 || index >= board.Count) return null;
            var card = board[index];
            board.RemoveAt(index);
            return card;
        }

        /// <summary>移除玩家场地上指定的卡牌。</summary>
        public bool RemoveCard(int playerId, CardBase card)
        {
            return _boards.ContainsKey(playerId) && _boards[playerId].Remove(card);
        }

        /// <summary>玩家场地上是否存在指定卡牌。</summary>
        public bool HasCard(int playerId, CardBase card)
        {
            return _boards.ContainsKey(playerId) && _boards[playerId].Contains(card);
        }

        /// <summary>玩家场地上是否存在指定点数（ID）的卡牌。</summary>
        public bool HasCardWithId(int playerId, int cardId)
        {
            if (!_boards.ContainsKey(playerId)) return false;
            foreach (var card in _boards[playerId])
                if (card.Id == cardId) return true;
            return false;
        }

        /// <summary>玩家场地上是否存在指定类型的卡牌。</summary>
        public bool HasCard<T>(int playerId) where T : CardBase
        {
            if (!_boards.ContainsKey(playerId)) return false;
            foreach (var card in _boards[playerId])
                if (card is T) return true;
            return false;
        }

        /// <summary>清空玩家场地。</summary>
        public void ClearBoard(int playerId)
        {
            if (_boards.ContainsKey(playerId))
                _boards[playerId].Clear();
        }

        /// <summary>清空所有玩家场地。</summary>
        public void ClearAll()
        {
            foreach (var board in _boards.Values)
                board.Clear();
        }

        /// <summary>
        /// 遍历所有玩家的场地，对每张牌调用 OnFieldCheck。
        /// 由 TurnManager 在 checkBoard 阶段调用。
        /// </summary>
        public void CheckAllBoards(IGameContext context)
        {
            foreach (var kvp in _boards)
            {
                int playerId = kvp.Key;
                // 每轮检查前重置 CanPlay，若场上有 Coffin 则会被重新置 false
                var player = context.GetPlayer(playerId);
                if (player != null)
                    player.CanPlay = true;

                foreach (var card in kvp.Value)
                {
                    if (!card.EffectsSuppressed)
                        card.OnFieldCheck(context, playerId);
                }
            }
        }

        private void EnsurePlayer(int playerId)
        {
            if (!_boards.ContainsKey(playerId))
                _boards[playerId] = new List<CardBase>();
        }
    }
}
