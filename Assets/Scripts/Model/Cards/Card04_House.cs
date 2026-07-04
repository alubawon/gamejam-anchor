namespace CardGame.Cards
{
    /// <summary>
    /// 4 房子：免疫下一轮其他玩家对你的影响。
    /// <para>打出后置 IsImmune = true，由游戏流程在下一轮开始时重置为 false。</para>
    /// </summary>
    public class Card04_House : CardBase
    {
        public Card04_House() : base(4, "房子", "免疫下一轮其他玩家对你的影响") { }

        public override void OnPlay(IGameContext context, int playerId, PlayTarget target = null)
        {
            var player = context.GetPlayer(playerId);
            if (player != null)
                player.IsImmune = true;
        }
    }
}
