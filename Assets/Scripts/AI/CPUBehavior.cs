namespace CardGame
{
    /// <summary>
    /// CPU 行为控制器 — GameManager 调用此入口获取 CPU 的出牌决策。
    /// <para>决策优先级：</para>
    /// <para>1. 棺材(8)优先规则 — 手中有棺材且自己场地没有时必须打出</para>
    /// <para>2. 分配的策略（Greedy/Hateful/Default）</para>
    /// <para>3. BaseBehavior fallback</para>
    /// </summary>
    public class CPUBehavior
    {
        private readonly CPUCharacteristics _characteristics;
        private readonly IBehaviorStrategy _strategy;
        private readonly BaseBehavior _fallback = new BaseBehavior();

        public CPUCharacteristics Characteristics => _characteristics;

        public CPUBehavior(CPUCharacteristics characteristics)
        {
            _characteristics = characteristics;
            _strategy = CPUCharacteristics.CreateStrategy(characteristics.Personality);
        }

        /// <summary>
        /// 产出 CPU 的出牌决策。
        /// </summary>
        /// <returns>PlayAction；无法出牌返回 null。</returns>
        public PlayAction Decide(IGameContext context, int playerId)
        {
            var player = context.GetPlayer(playerId);
            if (player == null || !player.CanPlay || player.Hand.Count == 0)
                return null;

            // 1. 棺材优先规则（游戏规则，非策略）
            var coffinAction = CheckCoffinPriority(context, playerId);
            if (coffinAction != null) return coffinAction;

            // 2. 尝试分配的策略
            var strategyAction = _strategy.Decide(context, playerId);
            if (strategyAction != null) return strategyAction;

            // 3. Fallback 到 BaseBehavior
            return _fallback.Decide(context, playerId);
        }

        /// <summary>
        /// 棺材优先检查：手中有棺材(8)且自己场地没有 → 必须打出。
        /// </summary>
        private PlayAction CheckCoffinPriority(IGameContext context, int playerId)
        {
            // 自己场地已有棺材 → CanPlay 应为 false，不会走到这里
            if (context.Board.HasCardWithId(playerId, 8)) return null;

            var player = context.GetPlayer(playerId);
            foreach (var card in player.Hand)
            {
                if (card.Id == 8)
                    return new PlayAction(card, playerId);
            }
            return null;
        }
    }
}
