namespace CardGame
{
    /// <summary>
    /// CPU 出牌策略接口 — 所有策略实现此接口。
    /// <para>CPUBehavior 先尝试分配的策略，失败时 fallback 到 BaseBehavior。</para>
    /// </summary>
    public interface IBehaviorStrategy
    {
        /// <summary>
        /// 根据当前游戏状态产出出牌决策。
        /// </summary>
        /// <returns>合法的 PlayAction；无法决策返回 null。</returns>
        PlayAction Decide(IGameContext context, int playerId);
    }
}
