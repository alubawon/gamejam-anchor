namespace CardGame.Cards
{
    /// <summary>
    /// 8 棺材：必须优先打进自己面前；存在于自己场上时，无法再出牌。
    /// <para>正常进入自己场地；OnFieldCheck 时将玩家 CanPlay 置 false。</para>
    /// <para>BoardManager.CheckAllBoards 每轮先重置 CanPlay = true，再遍历卡牌，
    /// 若 Coffin 仍在场上则重新置 false，移除后自动恢复。</para>
    /// </summary>
    public class Card08_Coffin : CardBase
    {
        public Card08_Coffin() : base(8, "棺材", "必须优先打进自己面前；存在于自己场上时，无法再出牌") { }

        // 打出时无即时效果，棺材的效果通过 OnFieldCheck 持续生效
        public override void OnPlay(IGameContext context, int playerId, PlayTarget target = null) { }

        public override void OnFieldCheck(IGameContext context, int playerId)
        {
            var player = context.GetPlayer(playerId);
            if (player != null)
                player.CanPlay = false;
        }
    }
}
