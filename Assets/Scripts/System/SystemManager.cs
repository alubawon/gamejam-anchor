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
                    new DialogueLine("旁白", "深夜的海面上，一艘孤船漂泊……", 3f),
                    new DialogueLine("旁白", "船上的三人，各自怀揣着不同的命运。", 3f),
                    new DialogueLine("旁白", "在这张牌桌上，只有一人能活下去。", 3f),
                    new DialogueLine("旁白", "牌局，开始了。", 3f),
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
                        new DialogueLine("旁白", "你赢了……但代价是什么？", 3f),
                        new DialogueLine("旁白", "船继续在夜海上漂泊……", 3f),
                    }
                    : new[]
                    {
                        new DialogueLine("旁白", "你输了……命运终究无法改变。", 3f),
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
