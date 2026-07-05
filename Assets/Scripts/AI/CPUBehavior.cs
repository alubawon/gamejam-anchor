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

            // 1. 尝试分配的策略
            var strategyAction = _strategy.Decide(context, playerId);
            if (strategyAction != null) return strategyAction;

            // 2. Fallback 到 BaseBehavior
            return _fallback.Decide(context, playerId);
        }

        /// <summary>
        /// 棺材优先检查：手中有棺材(8)且自己场地没有 → 必须打出。
    }
}
