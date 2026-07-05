using UnityEngine;

namespace CardGame.Cards
{
    /// <summary>
    /// 2 幸运草：抽一张牌，将原手牌放回牌堆顶。
    /// <para>流程：0.打出Clover → 1.翻开牌堆顶一张牌 → 2.把另一张手牌放回牌堆顶 → 3.将翻开的牌放回手牌</para>
    /// <para>当牌堆为空时，Clover仅打出不触发效果。</para>
    /// </summary>
    public class Card02_Clover : CardBase
    {
        public Card02_Clover() : base(2, "幸运草", "抽一张牌，将原手牌放回牌堆顶") { }

        public override void OnPlay(IGameContext context, int playerId, PlayTarget target = null)
        {
            var player = context.GetPlayer(playerId);
            if (player == null) return;

            // 牌堆为空 → 不触发效果
            if (context.Deck.Count <= 0)
            {
                Debug.Log("[Clover] Deck empty, no effect");
                return;
            }

            // 1. 翻开牌堆顶一张牌（不移除，先记住）
            var topCard = context.Deck.PeekTop();
            Debug.Log($"[Clover] Deck top: {topCard.Name}(Id={topCard.Id})");

            // 实际抽出来
            context.Deck.DrawCard();

            // 2. 把另一张手牌（原手牌）放回牌堆顶
            if (player.Hand.Count > 0)
            {
                var originalCard = player.Hand[0];
                player.Hand.RemoveAt(0);
                context.Deck.PutOnTop(originalCard);
                Debug.Log($"[Clover] Put on top: {originalCard.Name}(Id={originalCard.Id})");
            }

            // 3. 将翻开的牌放回手牌
            player.Hand.Add(topCard);
            Debug.Log($"[Clover] Added to hand: {topCard.Name}(Id={topCard.Id})");

            Debug.Log($"[Clover] After: hand={player.Hand.Count}");
            foreach (var c in player.Hand)
                Debug.Log($"[Clover]   Hand: {c.Name}(Id={c.Id})");
        }
    }
}
