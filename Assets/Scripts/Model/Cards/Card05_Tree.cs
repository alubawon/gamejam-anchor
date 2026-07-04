namespace CardGame.Cards
{
    /// <summary>
    /// 5 树：立刻打出牌堆最上面那张牌，不触发效果。
    /// <para>抽取牌堆顶牌放入自己场地，并设置 EffectsSuppressed = true 使其不触发任何效果。</para>
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
            context.Board.PlayCard(playerId, topCard);
        }
    }
}
