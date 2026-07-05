namespace CardGame.Cards
{
    /// <summary>
    /// 3 船：将对手牌区的一张牌移回牌堆底。
    /// <para>需要目标：target.TargetPlayerId（对手）、target.TargetCardIndex（对手场地上的卡牌索引）。</para>
    /// <para>若无合法目标（对手免疫或无牌），target 为 null，船正常进入场地但不拆牌。</para>
    /// </summary>
    public class Card03_Boat : CardBase
    {
        public Card03_Boat() : base(3, "船", "将对手牌区的一张牌移回牌堆底") { }

        public override void OnPlay(IGameContext context, int playerId, PlayTarget target = null)
        {
            // 无目标 → 不拆牌，船正常进入场地
            if (target?.TargetPlayerId == null || target.TargetCardIndex < 0) return;

            int targetId = target.TargetPlayerId.Value;
            var targetPlayer = context.GetPlayer(targetId);

            // 免疫检查
            if (targetPlayer != null && targetPlayer.IsImmune)
            {
                RaiseEffectBlocked(targetId, playerId);
                return;
            }

            var card = context.Board.RemoveCard(targetId, target.TargetCardIndex);
            if (card != null)
                context.Deck.PutOnBottom(card);
        }
    }
}
