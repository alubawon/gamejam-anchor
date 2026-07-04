namespace CardGame.Cards
{
    /// <summary>
    /// 7 蛇：与对手互换手牌。
    /// <para>需要目标：target.TargetPlayerId（交换手牌的对手）。</para>
    /// </summary>
    public class Card07_Snake : CardBase
    {
        public Card07_Snake() : base(7, "蛇", "与对手互换手牌") { }

        public override void OnPlay(IGameContext context, int playerId, PlayTarget target = null)
        {
            if (target?.TargetPlayerId == null) return;

            int targetId = target.TargetPlayerId.Value;
            var targetPlayer = context.GetPlayer(targetId);

            // 免疫检查
            if (targetPlayer != null && targetPlayer.IsImmune) return;

            context.SwapHands(playerId, targetId);
        }
    }
}
