namespace CardGame
{
    /// <summary>
    /// 出牌动作 — 描述玩家的一次完整出牌决策。
    /// </summary>
    public class PlayAction
    {
        /// <summary>要打出的卡牌（必须来自当前玩家手牌）。</summary>
        public CardBase Card { get; set; }

        /// <summary>卡牌放置到哪名玩家的场地（可为任意玩家，含自己）。</summary>
        public int TargetBoardId { get; set; }

        /// <summary>卡牌效果所需的额外目标信息（Boat 的目标牌索引、Cloud 的交换选择等）。</summary>
        public PlayTarget EffectTarget { get; set; }

        public PlayAction(CardBase card, int targetBoardId, PlayTarget effectTarget = null)
        {
            Card = card;
            TargetBoardId = targetBoardId;
            EffectTarget = effectTarget;
        }
    }
}
