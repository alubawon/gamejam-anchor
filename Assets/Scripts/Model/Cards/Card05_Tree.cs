namespace CardGame.Cards
{
    /// <summary>
    /// 5 树：立刻打出牌堆最上面那张牌，不触发效果。
    /// <para>子流程：1.翻开牌堆顶牌 2.选择目标场地(通过 PlayTarget.TargetBoardId) 3.放入目标场地，EffectsSuppressed=true</para>
    /// </summary>
    public class Card05_Tree : CardBase
    {
        public Card05_Tree() : base(5, "树", "立刻打出牌堆最上面那张牌，不触发效果") { }

        public override void OnPlay(IGameContext context, int playerId, PlayTarget target = null)
        {
            var topCard = context.Deck.DrawCard();
            if (topCard == null) return;

            // 不触发该牌的任何效果
            topCard.EffectsSuppressed = true;

            // 放入 target 指定的场地，fallback 放入自己场地
            int targetBoardId = target?.TargetBoardId ?? playerId;
            context.Board.PlayCard(targetBoardId, topCard);
        }
    }
}
