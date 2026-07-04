using System.Collections.Generic;

namespace CardGame
{
    /// <summary>
    /// 卡牌工厂 — 按 ID 创建卡牌实例、构建完整牌堆。
    /// </summary>
    public static class CardFactory
    {
        /// <summary>卡牌 ID 范围：2～8。</summary>
        public static readonly int[] CardIds = { 2, 3, 4, 5, 6, 7, 8 };

        /// <summary>根据 ID 创建单张卡牌。</summary>
        public static CardBase CreateCard(int id)
        {
            return id switch
            {
                2 => new Cards.Card02_Clover(),
                3 => new Cards.Card03_Boat(),
                4 => new Cards.Card04_House(),
                5 => new Cards.Card05_Tree(),
                6 => new Cards.Card06_Cloud(),
                7 => new Cards.Card07_Snake(),
                8 => new Cards.Card08_Coffin(),
                _ => throw new System.ArgumentException($"未知卡牌 ID: {id}")
            };
        }

        /// <summary>
        /// 构建完整牌堆。
        /// <para>每种卡牌（2～8）各 copiesPerCard 张，默认 5 份 → 7×5=35 张。</para>
        /// </summary>
        public static List<CardBase> CreateDeck(int copiesPerCard = 5)
        {
            var deck = new List<CardBase>(CardIds.Length * copiesPerCard);
            for (int i = 0; i < copiesPerCard; i++)
            {
                foreach (var id in CardIds)
                    deck.Add(CreateCard(id));
            }
            return deck;
        }
    }
}
