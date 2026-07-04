using System.Collections.Generic;

namespace CardGame
{
    /// <summary>
    /// 游戏上下文接口 — 为卡牌效果提供对游戏状态的访问。
    /// 由系统层 GameManager 实现，传给 CardBase.OnPlay / OnFieldCheck。
    /// </summary>
    public interface IGameContext
    {
        /// <summary>牌堆管理器。</summary>
        DeckManager Deck { get; }

        /// <summary>场地管理器。</summary>
        BoardManager Board { get; }

        /// <summary>所有玩家数据（只读列表）。</summary>
        IReadOnlyList<PlayerData> Players { get; }

        /// <summary>当前回合玩家 ID。</summary>
        int CurrentPlayerId { get; set; }

        /// <summary>根据 ID 获取玩家数据。</summary>
        PlayerData GetPlayer(int playerId);

        /// <summary>获取指定玩家的所有对手 ID。</summary>
        List<int> GetOpponentIds(int playerId);

        /// <summary>交换两名玩家的手牌。</summary>
        void SwapHands(int playerA, int playerB);
    }
}
