using UnityEngine;
using CardGame.UI;

namespace CardGame
{
    /// <summary>
    /// 系统管理器 — 管理整个游戏大流程。
    /// <para>流程：菜单 → 初始剧情 → 对局 → 结局剧情 →（循环或返回菜单）</para>
    /// <para>为单例 MonoBehaviour，持有 GameManager 和 ScenarioManager 引用。</para>
    /// </summary>
    public class SystemManager : MonoBehaviour
    {
        public static SystemManager Instance { get; private set; }

        [Header("组件引用")]
        [SerializeField] private GameManager _gameManager;
        [SerializeField] private ScenarioManager _scenarioManager;
        [SerializeField] private ScenarioUI _scenarioUI;

        /// <summary>当前游戏阶段。</summary>
        public GamePhase CurrentPhase { get; private set; } = GamePhase.Menu;

        /// <summary>最近一次对局结果。</summary>
        public MatchResult LastMatchResult { get; private set; }

        /// <summary>游戏阶段变更事件。</summary>
        public event System.Action<GamePhase> OnPhaseChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        // ── 流程入口 ──────────────────────────────────────────

        /// <summary>
        /// 开始游戏流程：菜单 → 初始剧情 → 对局。
        /// </summary>
        public void StartGameFlow()
        {
            TransitionTo(GamePhase.IntroStory);

            // 使用 ScenarioUI 播放开场剧情
            if (_scenarioUI != null)
            {
                _scenarioUI.Play(new[]
                {
                    new DialogueLine("旁白", "嘿，水手，你终于醒了。", 4f),
                    new DialogueLine("旁白", "那瘸子真不地道，输了牌就拿船桨砸你脑袋——别激动，他已经付出代价了。", 5f),
                    new DialogueLine("旁白", "依我看，你们都该学我躲进铁罐头里，免得被仓底老鼠咬了脚趾头。", 5f),
                    new DialogueLine("旁白", "（铃声响起）", 2f),
                    new DialogueLine("旁白", "哦，休息时间结束了，水手。去牌桌上收割祭品吧，呵呵呵……", 5f),
                    new DialogueLine("瘦子", "嘻嘻嘻……看看谁来了？这不是脑袋开花的小拖把吗？", 5f),
                    new DialogueLine("瘦子", "待会儿你可得小心点，别把脑浆抹在牌上了，嘻嘻嘻……", 5f),
                    new DialogueLine("胖子", "嘿，小子，听着，敲你脑袋的瘸子已经被我碾成鱼饵了。我可是你的恩人，明白吗？", 6f),
                    new DialogueLine("胖子", "老子最讨厌恩将仇报。你要是敢赢，老子就拿你沾满脑浆的脑壳通便！相信我，我会这么做的。", 6f),
                    new DialogueLine("胖子", "去吧，让我们坐上牌桌，把他们打得落花流水。", 4f),
                    new DialogueLine("旁白", "不记得规则？怎么回事，水手，记忆和脑浆一起溅出来了？", 4f),
                    new DialogueLine("旁白", "听着，规则非常简单——率先在自己的出牌区打出七张不同的塔罗牌就赢了。", 5f),
                    new DialogueLine("旁白", "至于其他细节，你最好去牌桌上亲自试试。", 4f),
                    new DialogueLine("旁白", "等干掉面前这两个蠢货，你就能去挑战船长了。呵呵呵……", 5f),
                }, () =>
                {
                    _gameManager.StartMatch(OnMatchComplete);
                    TransitionTo(GamePhase.Match);
                });
            }
            else
            {
                // 无 ScenarioUI → 直接开始对局
                _gameManager.StartMatch(OnMatchComplete);
                TransitionTo(GamePhase.Match);
            }
        }

        /// <summary>对局完成回调 → 进入结局剧情。</summary>
        private void OnMatchComplete(MatchResult result)
        {
            LastMatchResult = result;
            TransitionTo(GamePhase.EndingStory);

            if (_scenarioUI != null)
            {
                var lines = result.HumanWon
                    ? new[]
                    {
                        new DialogueLine("旁白", "哼，还算不赖。即使脑浆少了一半，也不影响你干掉那两个无赖。", 6f),
                        new DialogueLine("旁白", "准备好面对船长了吗？这次，我们要用真正的命运牌了。", 5f),
                        new DialogueLine("旁白", "我想你大概注意到了，从「幸运草」到「棺材」，分别是2到8号牌。", 5f),
                        new DialogueLine("旁白", "而从2加到8，结果为35。", 4f),
                        new DialogueLine("旁白", "集齐这7张牌，就能获得35号牌——传说中的「锚」牌。", 5f),
                        new DialogueLine("旁白", "将「锚」牌从船长手中夺走，整艘船，就是你的了……呵呵呵……", 6f),
                    }
                    : new[]
                    {
                        new DialogueLine("旁白", "你输了……命运终究无法改变。", 4f),
                        new DialogueLine("旁白", "船消失在黑暗中……", 3f),
                    };

                _scenarioUI.Play(lines, () => TransitionTo(GamePhase.GameComplete));
            }
            else
            {
                TransitionTo(GamePhase.GameComplete);
            }
        }

        /// <summary>返回菜单。</summary>
        public void ReturnToMenu()
        {
            TransitionTo(GamePhase.Menu);
        }

        /// <summary>开始新一局（从结局回到对局）。</summary>
        public void StartNewMatch()
        {
            _gameManager.StartMatch(OnMatchComplete);
            TransitionTo(GamePhase.Match);
        }

        // ── 内部方法 ──────────────────────────────────────────

        private void TransitionTo(GamePhase phase)
        {
            CurrentPhase = phase;
            OnPhaseChanged?.Invoke(phase);
        }
    }
}
