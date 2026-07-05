using System.Collections.Generic;

namespace CardGame
{
    /// <summary>
    /// 牌堆管理 — 构建、洗牌、抽牌、放回牌堆顶/底、查看顶底。
    /// 纯 C# 逻辑，无 Unity 依赖。
    /// </summary>
    public class DeckManager
    {
        private readonly List<CardBase> _deck = new List<CardBase>();

        public int Count => _deck.Count;

        /// <summary>从牌堆顶抽一张牌（移除并返回）。</summary>
        public CardBase DrawCard()
        {
            if (_deck.Count == 0) return null;
            var card = _deck[0];
            _deck.RemoveAt(0);
            return card;
        }

        /// <summary>将牌放回牌堆顶。</summary>
        public void PutOnTop(CardBase card)
        {
            if (card == null) return;
            _deck.Insert(0, card);
        }

        /// <summary>将牌放回牌堆底。</summary>
        public void PutOnBottom(CardBase card)
        {
            if (card == null) return;
            _deck.Add(card);
        }

        /// <summary>查看牌堆顶（不移除）。</summary>
        public CardBase PeekTop()
        {
            return _deck.Count > 0 ? _deck[0] : null;
        }

        /// <summary>查看牌堆底（不移除）。</summary>
        public CardBase PeekBottom()
        {
            return _deck.Count > 0 ? _deck[_deck.Count - 1] : null;
        }

        /// <summary>交换牌堆顶和牌堆底。</summary>
        public void SwapTopBottom()
        {
            if (_deck.Count < 2) return;
            (_deck[0], _deck[_deck.Count - 1]) = (_deck[_deck.Count - 1], _deck[0]);
        }

        /// <summary>构建牌堆（替换现有内容）。</summary>
        public void BuildDeck(IEnumerable<CardBase> cards)
        {
            _deck.Clear();
            _deck.AddRange(cards);
        }

        /// <summary>在指定索引处插入一张牌。</summary>
        public void InsertAt(int index, CardBase card)
        {
            _deck.Insert(index, card);
        }

        /// <summary>从牌堆中移除指定牌。</summary>
        public bool Remove(CardBase card)
        {
            return _deck.Remove(card);
        }

        /// <summary>Fisher-Yates 洗牌。</summary>
        public void Shuffle(System.Random rng = null)
        {
            rng ??= new System.Random();
            for (int i = _deck.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (_deck[i], _deck[j]) = (_deck[j], _deck[i]);
            }
        }
    }
}
