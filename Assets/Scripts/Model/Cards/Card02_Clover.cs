namespace CardGame.Cards
{
    /// <summary>
    /// 2 幸运草：抽一张牌，将原手牌放回牌堆顶。
    /// <para>打出后不进入场地，而是回收至牌堆顶并抽取新牌。</para>
    /// </summary>
    public class Card02_Clover : CardBase
    {
        public Card02_Clover() : base(2, "幸运草", "抽一张牌，将原手牌放回牌堆顶") { }

        /// <summary>此牌打出后回收至牌堆顶，不进入场地。</summary>
        public override bool GoesToBoardAfterPlay => false;

        public override void OnPlay(IGameContext context, int playerId, PlayTarget target = null)
        {
            var player = context.GetPlayer(playerId);
            // 抽一张牌到手牌
            var drawn = context.Deck.DrawCard();
            if (drawn != null)
                player.Hand.Add(drawn);
            // 将此牌（原手牌）放回牌堆顶
            context.Deck.PutOnTop(this);
        }
    }
}
