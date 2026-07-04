using System.Collections.Generic;

namespace CardGame
{
    /// <summary>
    /// 游戏大流程阶段。
    /// </summary>
    public enum GamePhase
    {
        Menu,
        IntroStory,
        Match,
        EndingStory,
        GameComplete,
    }

    /// <summary>
    /// 单局对局结果。
    /// </summary>
    public class MatchResult
    {
        public int WinnerId { get; set; }
        public int HumanPlayerId { get; set; }
        public bool HumanWon => WinnerId == HumanPlayerId;

        /// <summary>每名玩家场地上的不同卡牌点数（集齐进度）。</summary>
        public Dictionary<int, int> BoardPoints { get; set; } = new Dictionary<int, int>();

        /// <summary>人类玩家的场上点数。</summary>
        public int HumanBoardPoints =>
            BoardPoints.TryGetValue(HumanPlayerId, out var pts) ? pts : 0;

        /// <summary>总回合数。</summary>
        public int TotalRounds { get; set; }
    }
}
