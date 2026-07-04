using System;

namespace CardGame
{
    /// <summary>
    /// 出牌时附带的目标信息。不需要目标的卡牌忽略此参数。
    /// </summary>
    public class PlayTarget
    {
        /// <summary>目标玩家 ID（如 Boat 的对手、Snake 的交换对象）。</summary>
        public int? TargetPlayerId { get; set; }

        /// <summary>目标卡牌在对手场地上的索引（如 Boat 选择移除哪张牌）。</summary>
        public int TargetCardIndex { get; set; } = -1;

        /// <summary>玩家的选择结果（如 Cloud 是否选择交换牌堆顶底）。</summary>
        public bool? Choice { get; set; }
    }

    /// <summary>
    /// 卡牌基类 — 所有卡牌的抽象基础。
    /// <para>打出效果 (OnPlay): 卡牌被打出时触发的即时效果，由 TurnManager 在出牌阶段调用。</para>
    /// <para>在场效果 (OnFieldCheck): 卡牌在场上时持续/检查的效果，由 BoardManager.CheckAllBoards 调用。</para>
    /// </summary>
    public abstract class CardBase
    {
        public int Id { get; }
        public string Name { get; }
        public string Description { get; }

        /// <summary>
        /// 打出后是否进入打牌者的场地。默认 true。
        /// Clover 等卡牌重写为 false（回收至牌堆而非留在场上）。
        /// </summary>
        public virtual bool GoesToBoardAfterPlay => true;

        /// <summary>
        /// 效果是否被抑制（如被 Tree 打出但不触发效果时设为 true）。
        /// TurnManager / BoardManager 在调用 OnPlay / OnFieldCheck 前检查此标记。
        /// </summary>
        public bool EffectsSuppressed { get; set; }

        protected CardBase(int id, string name, string description)
        {
            Id = id;
            Name = name;
            Description = description;
        }

        /// <summary>打出效果 — 卡牌被打出时由 TurnManager 调用。</summary>
        public abstract void OnPlay(IGameContext context, int playerId, PlayTarget target = null);

        /// <summary>在场效果 — BoardManager.CheckAllBoards 时对场上每张牌调用。</summary>
        public virtual void OnFieldCheck(IGameContext context, int playerId) { }
    }
}
