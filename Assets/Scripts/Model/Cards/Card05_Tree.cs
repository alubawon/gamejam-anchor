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

            // 检查目标场地是否已有同名牌 → 如果有则放回牌堆顶
            if (context.Board.HasCardWithId(targetBoardId, topCard.Id))
            {
                // 目标场地已有同名牌，尝试放入其他合法场地
                bool placed = false;
                foreach (var p in context.Players)
                {
                    if (!context.Board.HasCardWithId(p.Id, topCard.Id))
                    {
                        context.Board.PlayCard(p.Id, topCard);
                        placed = true;
                        break;
                    }
                }
                if (!placed)
                {
                    // 所有场地都有同名牌 → 放回牌堆顶
                    context.Deck.PutOnTop(topCard);
                }
            }
            else
            {
                context.Board.PlayCard(targetBoardId, topCard);
            }
        }
    }
}
