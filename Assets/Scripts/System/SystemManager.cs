using UnityEngine;

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
            _scenarioManager.PlayIntroStory(() =>
            {
                TransitionTo(GamePhase.Match);
                _gameManager.StartMatch(OnMatchComplete);
            });
        }

        /// <summary>对局完成回调 → 进入结局剧情。</summary>
        private void OnMatchComplete(MatchResult result)
        {
            LastMatchResult = result;
            TransitionTo(GamePhase.EndingStory);
            _scenarioManager.PlayEndingStory(result, () =>
            {
                TransitionTo(GamePhase.GameComplete);
            });
        }

        /// <summary>返回菜单。</summary>
        public void ReturnToMenu()
        {
            TransitionTo(GamePhase.Menu);
        }

        /// <summary>开始新一局（从结局回到对局）。</summary>
        public void StartNewMatch()
        {
            TransitionTo(GamePhase.Match);
            _gameManager.StartMatch(OnMatchComplete);
        }

        // ── 内部方法 ──────────────────────────────────────────

        private void TransitionTo(GamePhase phase)
        {
            CurrentPhase = phase;
            OnPhaseChanged?.Invoke(phase);
        }
    }
}
