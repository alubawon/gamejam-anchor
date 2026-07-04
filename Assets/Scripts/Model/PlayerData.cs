using System.Collections.Generic;

namespace CardGame
{
    /// <summary>
    /// 玩家数据 — 手牌、积分、状态标记。
    /// </summary>
    public class PlayerData
    {
        public int Id { get; }
        public string Name { get; }

        /// <summary>当前手牌列表。</summary>
        public List<CardBase> Hand { get; } = new List<CardBase>();

        /// <summary>累计积分。</summary>
        public int Score { get; set; }

        /// <summary>是否免疫其他玩家效果（House 卡打出后置 true，下轮自动重置）。</summary>
        public bool IsImmune { get; set; }

        /// <summary>是否可以出牌（Coffin 卡在场时由 OnFieldCheck 置 false）。</summary>
        public bool CanPlay { get; set; } = true;

        /// <summary>是否为 CPU 玩家。</summary>
        public bool IsCPU { get; set; }

        public PlayerData(int id, string name, bool isCPU = false)
        {
            Id = id;
            Name = name;
            IsCPU = isCPU;
        }
    }
}
