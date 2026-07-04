using System.Collections.Generic;

namespace CardGame
{
    /// <summary>
    /// 局中事件类型 — 对应 event_scenario.csv 中的全部事件。
    /// </summary>
    public enum InGameEventType
    {
        // ── 教学/开场（打断操作）──────────────────────────────
        /// <summary>牌局开始 (CSV #1)。</summary>
        GameStart,
        /// <summary>玩家先手第一次抽牌 (CSV #2, 4 步)。</summary>
        FirstDraw,

        // ── UI 反馈 ──────────────────────────────────────────
        /// <summary>鼠标指向出牌区的牌 (CSV #3)。</summary>
        CursorOnCard,
        /// <summary>玩家打牌 (CSV #4)。</summary>
        PlayerPlayACard,
        /// <summary>任何人打出牌后 (CSV #5)。</summary>
        CardIntoBoard,

        // ── CPU 气泡 ──────────────────────────────────────────
        /// <summary>CPU 把牌打到自己出牌区 (CSV #6/#8)。</summary>
        CPUPlayCardSelf,
        /// <summary>CPU 把牌打到他人出牌区 (CSV #7/#9)。</summary>
        CPUPlayCardOther,

        // ── 胜负事件 ──────────────────────────────────────────
        /// <summary>CPU 胜利 (CSV #10/#12)。</summary>
        CPUWin,
        /// <summary>玩家胜利 (CSV #11/#13)。</summary>
        PlayerWin,

        // ── 卡牌子流程 ────────────────────────────────────────
        /// <summary>幸运草被打出后 (CSV #14)。</summary>
        CloverEffect,
        /// <summary>船被打出后 — 玩家选择目标 (CSV #15.1)。</summary>
        BoatEffectPlayer,
        /// <summary>船被打出后 — CPU 自动选择 (CSV #15.2)。</summary>
        BoatEffectCPU,
        /// <summary>房子被打出后 (CSV #16)。</summary>
        HouseEffect,
        /// <summary>房子效果触发时 — 免疫了影响 (CSV #17)。</summary>
        HouseImmunityTriggered,
        /// <summary>树被打出后 (CSV #18)。</summary>
        TreeEffect,
        /// <summary>云被打出后 — 玩家选择是否交换 (CSV #19.1)。</summary>
        CloudEffectChoose,
        /// <summary>云 — 交换了牌堆顶底 (CSV #19.2)。</summary>
        CloudEffectSwapped,
        /// <summary>云 — 没有交换 (CSV #19.3)。</summary>
        CloudEffectNotSwapped,
        /// <summary>蛇被打出后 — 玩家选择交换对手 (CSV #20.1)。</summary>
        SnakeEffectChoose,
        /// <summary>蛇 — 交换了手牌 (CSV #20.2)。</summary>
        SnakeEffectDone,
        /// <summary>棺材被置入出牌区 (CSV #21)。</summary>
        CoffinPlaced,
        /// <summary>棺材从出牌区移回牌堆 (CSV #22)。</summary>
        CoffinRemoved,

        // ── 监控 ──────────────────────────────────────────────
        /// <summary>玩家场地集齐 6 张不同牌（接近胜利）。</summary>
        NearVictory,
    }

    /// <summary>
    /// 局中事件数据 — 携带触发事件的上下文信息。
    /// </summary>
    public class InGameEvent
    {
        public InGameEventType Type { get; set; }
        public int PlayerId { get; set; }
        public int TargetPlayerId { get; set; } = -1;
        public int CardId { get; set; } = -1;
        public string CardName { get; set; }
        public int BoardUniqueCount { get; set; }
        public bool IsPlayerAction => PlayerId == 0;

        public InGameEvent(InGameEventType type) { Type = type; }
    }

    /// <summary>
    /// 剧情条目 — 一个事件类型对应的演出数据。
    /// </summary>
    public class ScenarioEntry
    {
        /// <summary>是否打断操作（需点击继续）。</summary>
        public bool Interrupt;

        /// <summary>显示时长（秒）。</summary>
        public float Duration = 5f;

        /// <summary>屏幕上方提示文案（按顺序播放，支持多步）。</summary>
        public DialogueLine[] TopPrompts;

        /// <summary>气泡文案（随机选一条，前后不重复）。null 表示无气泡。</summary>
        public DialogueLine[] RandomBubbles;

        /// <summary>气泡说话者名称（瘦子/胖子/系统）。</summary>
        public string BubbleSpeaker;
    }
}
