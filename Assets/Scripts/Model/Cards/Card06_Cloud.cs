namespace CardGame.Cards
{
    /// <summary>
    /// 6 云：查看牌堆顶和牌堆底的牌，并可以将它们互换。
    /// <para>查看操作由 View 层处理；若 target.Choice == true 则执行交换。</para>
    /// </summary>
    public class Card06_Cloud : CardBase
    {
        public Card06_Cloud() : base(6, "云", "查看牌堆顶和牌堆底的牌，并可以将它们互换") { }

        public override void OnPlay(IGameContext context, int playerId, PlayTarget target = null)
        {
            // 查看牌堆顶和底（实际展示由 View 层在调用前处理）
            // 若玩家/AI选择互换，执行交换
            if (target?.Choice == true)
            {
                context.Deck.SwapTopBottom();
            }
        }
    }
}
