using System.Collections.Generic;

namespace CardGame
{
    /// <summary>
    /// CPU 性格类型。
    /// </summary>
    public enum CPUPersonality
    {
        Default,
        Greedy,
        Hateful,
    }

    /// <summary>
    /// CPU 性格配置 — 描述一个 AI 玩家的性格特征。
    /// <para>通过 <see cref="CreateStrategy"/> 将性格映射到出牌策略。</para>
    /// </summary>
    public class CPUCharacteristics
    {
        public CPUPersonality Personality { get; }
        public string DisplayName { get; }
        public string Description { get; }

        /// <summary>AI 思考延迟（秒），供系统层模拟思考时间。</summary>
        public float ThinkDelaySeconds { get; }

        public CPUCharacteristics(
            CPUPersonality personality,
            string displayName = null,
            string description = "",
            float thinkDelaySeconds = 1.5f)
        {
            Personality = personality;
            DisplayName = displayName ?? personality.ToString();
            Description = description;
            ThinkDelaySeconds = thinkDelaySeconds;
        }

        /// <summary>
        /// 默认配置：2 名 CPU 玩家的性格（按 PlayerId 顺序）。
        /// <para>[0] → PlayerId=1 下家｜瘦子：Hateful，优先拆玩家场地</para>
        /// <para>[1] → PlayerId=2 上家｜胖子：Greedy，优先填满自己场地并拆最满对手</para>
        /// </summary>
        public static readonly CPUCharacteristics[] DefaultConfigs =
        {
            new CPUCharacteristics(
                CPUPersonality.Hateful,
                "瘦子",
                "下家，优先拆玩家的场地"),
            new CPUCharacteristics(
                CPUPersonality.Greedy,
                "胖子",
                "上家，优先填满自己场地，并拆场地最满的对手"),
        };

        /// <summary>
        /// 性格 → 策略工厂。
        /// <para>Default 性格直接使用 BaseBehavior。</para>
        /// </summary>
        public static IBehaviorStrategy CreateStrategy(CPUPersonality personality)
        {
            return personality switch
            {
                CPUPersonality.Greedy => new GreedyBehavior(),
                CPUPersonality.Hateful => new HatefulBehavior(),
                _ => new BaseBehavior(),
            };
        }
    }
}
